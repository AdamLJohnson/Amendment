using Amendment.Cdk.Config;
using Amendment.Cdk.Stacks;
using Amendment.Domain.Authorization;

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

        var records = s.Resources().Where(r => r.Type == "AWS::Route53::RecordSet").Select(r => r.Resource["Properties"]!).ToList();
        Assert.Equal(["A", "AAAA"], records.Select(r => (string)r["Type"]!).Order());
        foreach (var record in records)
        {
            Assert.Equal(host + ".", (string)record["Name"]!);
            Assert.Equal("Z3A6EHPCI5BABT", (string)record["HostedZoneId"]!);
            Assert.Contains("Distribution", record["AliasTarget"]!["DNSName"]!.ToJsonString());
        }

        var cert = s.Resources().Single(r => r.Type == "AWS::CertificateManager::Certificate").Resource["Properties"]!;
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
        var record = s.Resources().First(r => r.Type == "AWS::Route53::RecordSet").Resource["Properties"]!;
        Assert.Equal("app.example.org.", (string)record["Name"]!);
        Assert.Equal("ZOTHER", (string)record["HostedZoneId"]!);
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

    [Fact]
    public void Cognito_groups_cover_every_legacy_role_and_client_is_public_pkce()
    {
        var groups = Dev.Resources().Where(r => r.Type == "AWS::Cognito::UserPoolGroup").Select(r => r.Resource["Properties"]!).ToList();
        Assert.Equal(Roles.All.Order(), groups.Select(g => (string)g["Description"]!).Order());
        Assert.All(groups, g => Assert.DoesNotContain(" ", (string)g["GroupName"]!));
        Assert.Equal(IdentityStack.RoleGroups.Keys.Order(), groups.Select(g => (string)g["GroupName"]!).Order());

        var pool = Dev.Resources().Single(r => r.Type == "AWS::Cognito::UserPool").Resource["Properties"]!;
        Assert.True((bool)pool["AdminCreateUserConfig"]!["AllowAdminCreateUserOnly"]!);

        var client = Dev.Resources().Single(r => r.Type == "AWS::Cognito::UserPoolClient").Resource["Properties"]!;
        Assert.False((bool?)client["GenerateSecret"] ?? false);
        Assert.Equal("""["code"]""", client["AllowedOAuthFlows"]!.ToJsonString());
        Assert.Contains("https://dev-amendments.adamlj.com/", client["CallbackURLs"]!.ToJsonString());
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
