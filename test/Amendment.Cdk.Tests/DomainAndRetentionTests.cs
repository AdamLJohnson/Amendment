using Amendment.Cdk.Config;
using Amendment.Cdk.Stacks;
using Amendment.Domain.Authorization;
using Amendment.Infrastructure.DynamoDb;

namespace Amendment.Cdk.Tests;

public class DomainAndRetentionTests
{
    private static readonly SynthesizedStage Dev = SynthesizedStage.Create("dev");
    private static readonly SynthesizedStage Prod = SynthesizedStage.Create("prod");

    [Theory]
    [InlineData("dev", "dev-amendments.adamlj.com")]
    [InlineData("prod", "amendments.adamlj.com")]
    public void Site_aliases_target_the_stage_distribution_in_the_reused_zone(string stage, string host)
    {
        var s = stage == "dev" ? Dev : Prod;
        Assert.DoesNotContain(s.Resources(), r => r.Type == "AWS::Route53::HostedZone");

        Assert.Equal(["A", "A", "AAAA"], s.Resources().Where(r => r.Type == "AWS::Route53::RecordSet").Select(r => (string)r.Resource["Properties"]!["Type"]!).Order());
        var records = s.Json(s.Stacks.Web)["Resources"]!.AsObject().Select(r => r.Value!)
            .Where(r => (string)r["Type"]! == "AWS::Route53::RecordSet").Select(r => r["Properties"]!).ToList();
        Assert.Equal(["A", "AAAA"], records.Select(r => (string)r["Type"]!).Order());
        foreach (var record in records)
        {
            Assert.Equal(host + ".", (string)record["Name"]!);
            Assert.Equal("Z3A6EHPCI5BABT", (string)record["HostedZoneId"]!);
            Assert.Contains("Distribution", record["AliasTarget"]!["DNSName"]!.ToJsonString());
        }

        var cert = s.Resources().Single(r => r.Type == "AWS::CertificateManager::Certificate" && r.Stack == s.Stacks.Web).Resource["Properties"]!;
        Assert.Equal(host, (string)cert["DomainName"]!);
        Assert.Equal("DNS", (string)cert["ValidationMethod"]!);
        var aliases = s.Resources().Single(r => r.Type == "AWS::CloudFront::Distribution").Resource["Properties"]!["DistributionConfig"]!["Aliases"]!;
        Assert.Equal(host, (string)aliases[0]!);
    }

    [Fact]
    public void Web_stack_with_cloudfront_certificate_deploys_to_us_east_1()
    {
        Assert.Equal("us-east-1", Dev.Stacks.Web.Region);
        Assert.Equal("us-east-1", Prod.Stacks.Web.Region);
    }

    [Fact]
    public void Dev_and_prod_use_distinct_names()
    {
        var devNames = Dev.Stacks.All.Select(s => s.StackName).ToHashSet();
        Assert.DoesNotContain(Prod.Stacks.All, s => devNames.Contains(s.StackName));
        Assert.All(Dev.Stacks.All, s => Assert.StartsWith("Amendment-dev-", s.StackName));
        Assert.All(Prod.Stacks.All, s => Assert.StartsWith("Amendment-prod-", s.StackName));
    }

    [Fact]
    public void Domain_and_zone_can_be_overridden_through_context()
    {
        var s = SynthesizedStage.Create("dev", new Dictionary<string, object>
        {
            ["hostedZoneId"] = "ZOTHER", ["hostedZoneName"] = "example.org", ["siteDomainName"] = "app.example.org"
        });
        var records = s.Resources().Where(r => r.Type == "AWS::Route53::RecordSet").Select(r => r.Resource["Properties"]!).ToList();
        Assert.Equal(["app.example.org.", "app.example.org.", "auth.app.example.org."], records.Select(r => (string)r["Name"]!).Order());
        Assert.All(records, r => Assert.Equal("ZOTHER", (string)r["HostedZoneId"]!));
    }

    [Fact]
    public void Dev_resources_are_disposable()
    {
        var table = Dev.Resources().Single(r => r.Type == "AWS::DynamoDB::Table").Resource;
        Assert.Equal("Delete", (string)table["DeletionPolicy"]!);
        Assert.NotEqual(true, (bool?)table["Properties"]!["DeletionProtectionEnabled"]);
        Assert.Equal("Delete", (string)Dev.Resources().Single(r => r.Type == "AWS::Cognito::UserPool").Resource["DeletionPolicy"]!);
        Assert.Equal("Delete", (string)Dev.Resources().Single(r => r.Type == "AWS::S3::Bucket").Resource["DeletionPolicy"]!);
        Assert.All(Dev.Stacks.All, s => Assert.False(s.TerminationProtection));
    }

    [Fact]
    public void Prod_data_and_identity_are_retained_and_protected()
    {
        var table = Prod.Resources().Single(r => r.Type == "AWS::DynamoDB::Table").Resource;
        Assert.Equal("Retain", (string)table["DeletionPolicy"]!);
        Assert.True((bool)table["Properties"]!["DeletionProtectionEnabled"]!);
        Assert.True((bool)table["Properties"]!["PointInTimeRecoverySpecification"]!["PointInTimeRecoveryEnabled"]!);
        Assert.Equal("PAY_PER_REQUEST", (string)table["Properties"]!["BillingMode"]!);

        var pool = Prod.Resources().Single(r => r.Type == "AWS::Cognito::UserPool").Resource;
        Assert.Equal("Retain", (string)pool["DeletionPolicy"]!);
        Assert.Equal("ACTIVE", (string)pool["Properties"]!["DeletionProtection"]!);
        Assert.Equal("Retain", (string)Prod.Resources().Single(r => r.Type == "AWS::S3::Bucket").Resource["DeletionPolicy"]!);
        Assert.All(Prod.Stacks.All, s => Assert.True(s.TerminationProtection));
    }

    [Fact]
    public void Table_uses_pk_sk_string_keys_and_stage_name()
    {
        var props = Dev.Resources().Single(r => r.Type == "AWS::DynamoDB::Table").Resource["Properties"]!;
        Assert.Equal("AmendmentData-dev", (string)props["TableName"]!);
        Assert.Equal("""[{"AttributeName":"PK","KeyType":"HASH"},{"AttributeName":"SK","KeyType":"RANGE"}]""", props["KeySchema"]!.ToJsonString());
    }

    [Theory]
    [InlineData("dev")]
    [InlineData("prod")]
    public void Table_has_only_the_amendment_list_index_matching_the_persistence_layer(string stage)
    {
        var props = (stage == "dev" ? Dev : Prod).Resources().Single(r => r.Type == "AWS::DynamoDB::Table").Resource["Properties"]!;
        var index = Assert.Single(props["GlobalSecondaryIndexes"]!.AsArray())!;
        var request = AmendmentTable.CreateTableRequest("x");
        var expected = Assert.Single(request.GlobalSecondaryIndexes);

        Assert.Equal(AmendmentTable.Gsi1, DataStack.AmendmentListIndex);
        Assert.Equal(expected.IndexName, (string)index["IndexName"]!);
        Assert.Equal(
            expected.KeySchema.Select(k => $"{k.AttributeName}:{k.KeyType.Value}"),
            index["KeySchema"]!.AsArray().Select(k => $"{(string)k!["AttributeName"]!}:{(string)k["KeyType"]!}"));
        Assert.Equal("ALL", (string)index["Projection"]!["ProjectionType"]!);
        Assert.Equal(
            request.AttributeDefinitions.Select(a => a.AttributeName).Order(),
            props["AttributeDefinitions"]!.AsArray().Select(a => (string)a!["AttributeName"]!).Order());
    }

    [Fact]
    public void Cognito_groups_cover_every_legacy_role_and_client_is_public_pkce()
    {
        var groups = Dev.Resources().Where(r => r.Type == "AWS::Cognito::UserPoolGroup").Select(r => r.Resource["Properties"]!).ToList();
        Assert.Equal(Roles.All.Order(), groups.Select(g => (string)g["Description"]!).Order());
        Assert.All(groups, g => Assert.DoesNotContain(" ", (string)g["GroupName"]!));
        Assert.Equal(IdentityStack.RoleGroups.Keys.Order(), groups.Select(g => (string)g["GroupName"]!).Order());

        var pool = Dev.Resources().Single(r => r.Type == "AWS::Cognito::UserPool").Resource["Properties"]!;
        Assert.True((bool)pool["AdminCreateUserConfig"]!["AllowAdminCreateUserOnly"]!);

        var client = Dev.Resources().Single(r => r.Type == "AWS::Cognito::UserPoolClient" && r.LogicalId.Contains("WebClient")).Resource["Properties"]!;
        Assert.False((bool?)client["GenerateSecret"] ?? false);
        Assert.Equal("""["code"]""", client["AllowedOAuthFlows"]!.ToJsonString());
        Assert.Equal(
            ["https://dev-amendments.adamlj.com/authentication/login-callback", "https://localhost:5001/authentication/login-callback", "http://localhost:5165/authentication/login-callback"],
            client["CallbackURLs"]!.AsArray().Select(u => (string)u!));
        Assert.Equal(
            ["https://dev-amendments.adamlj.com/", "https://localhost:5001/", "http://localhost:5165/"],
            client["LogoutURLs"]!.AsArray().Select(u => (string)u!));
        Assert.DoesNotContain("ADMIN_USER_PASSWORD", client.ToJsonString());
    }

    [Theory]
    [InlineData("dev", "auth.dev-amendments.adamlj.com")]
    [InlineData("prod", "auth.amendments.adamlj.com")]
    public void Sign_in_pages_use_a_custom_domain_with_its_own_certificate_and_alias(string stage, string signInDomain)
    {
        var s = stage == "dev" ? Dev : Prod;
        var resources = s.Json(s.Stacks.Identity)["Resources"]!.AsObject().Select(r => r.Value!).ToList();

        var domain = Assert.Single(resources, r => (string)r["Type"]! == "AWS::Cognito::UserPoolDomain")["Properties"]!;
        Assert.Equal(signInDomain, (string)domain["Domain"]!);
        Assert.NotNull(domain["CustomDomainConfig"]!["CertificateArn"]);

        var certificate = Assert.Single(resources, r => (string)r["Type"]! == "AWS::CertificateManager::Certificate")["Properties"]!;
        Assert.Equal(signInDomain, (string)certificate["DomainName"]!);
        Assert.Equal("DNS", (string)certificate["ValidationMethod"]!);

        var alias = Assert.Single(resources, r => (string)r["Type"]! == "AWS::Route53::RecordSet")["Properties"]!;
        Assert.Equal("A", (string)alias["Type"]!);
        Assert.Equal(signInDomain + ".", (string)alias["Name"]!);
        Assert.Equal("Z3A6EHPCI5BABT", (string)alias["HostedZoneId"]!);
        Assert.Equal("Z2FDTNDATAQYW2", (string)alias["AliasTarget"]!["HostedZoneId"]!);
        Assert.Contains("CloudFrontDistribution", alias["AliasTarget"]!["DNSName"]!.ToJsonString());

        Assert.Contains(s.Stacks.Web, s.Stacks.Identity.Dependencies);
    }

    [Fact]
    public void Api_test_client_exists_only_outside_production_and_only_allows_admin_password_auth()
    {
        var testClient = Dev.Resources().Single(r => r.Type == "AWS::Cognito::UserPoolClient" && r.LogicalId.Contains("TestClient")).Resource["Properties"]!;
        Assert.Equal("""["ALLOW_ADMIN_USER_PASSWORD_AUTH","ALLOW_REFRESH_TOKEN_AUTH"]""", testClient["ExplicitAuthFlows"]!.ToJsonString());
        Assert.Null(testClient["AllowedOAuthFlows"]);
        Assert.False((bool?)testClient["GenerateSecret"] ?? false);

        Assert.Single(Prod.Resources(), r => r.Type == "AWS::Cognito::UserPoolClient");
        Assert.Null(Prod.Stacks.Identity.TestClient);
    }

    [Theory]
    [InlineData("adamlj.com")]
    [InlineData("amendments.example.com")]
    public void Config_rejects_site_domain_outside_the_zone(string siteDomain)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            SynthesizedStage.Create("dev", new Dictionary<string, object> { ["siteDomainName"] = siteDomain }));
        Assert.Contains("must be a subdomain", ex.Message);
    }

    [Fact]
    public void Config_rejects_unknown_stage() =>
        Assert.Throws<ArgumentException>(() => StageConfig.Load("qa"));
}
