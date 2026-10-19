using System.Text.Json.Nodes;

namespace Amendment.Cdk.Tests;

public class SecurityAndCostTests
{
    private static readonly SynthesizedStage Dev = SynthesizedStage.Create("dev");
    private static readonly SynthesizedStage Prod = SynthesizedStage.Create("prod");

    public static TheoryData<string> Stages => new() { "dev", "prod" };
    private static SynthesizedStage For(string stage) => stage == "dev" ? Dev : Prod;

    /// <summary>CloudFormation types that cannot carry resource tags; stack-level tags still apply where supported.</summary>
    private static readonly HashSet<string> UntaggableTypes =
    [
        "AWS::CDK::Metadata", "AWS::S3::BucketPolicy", "AWS::SNS::TopicPolicy", "AWS::IAM::Policy",
        "AWS::AppSync::GraphQLSchema", "AWS::AppSync::DataSource", "AWS::AppSync::Resolver",
        "AWS::Cognito::UserPoolClient", "AWS::Cognito::UserPoolGroup", "AWS::CloudFront::OriginAccessControl",
        "AWS::Route53::RecordSet", "AWS::Budgets::Budget"
    ];

    [Theory, MemberData(nameof(Stages))]
    public void Every_stack_and_taggable_resource_has_application_and_environment_tags(string stage)
    {
        var s = For(stage);
        foreach (var stack in s.Stacks.All)
        {
            var stackTags = stack.Tags.TagValues();
            Assert.Equal("Amendment", stackTags["Application"]);
            Assert.Equal(stage, stackTags["Environment"]);
        }

        foreach (var (stack, id, type, resource) in s.Resources().Where(r => !UntaggableTypes.Contains(r.Type)))
        {
            var tags = SynthesizedStage.TagsOf(resource);
            Assert.True(tags.TryGetValue("Application", out var app) && app == "Amendment", $"{stack.StackName}/{id} ({type}) missing Application tag");
            Assert.True(tags.TryGetValue("Environment", out var env) && env == stage, $"{stack.StackName}/{id} ({type}) missing Environment tag");
        }
    }

    [Theory, MemberData(nameof(Stages))]
    public void No_vpc_nat_load_balancer_database_server_or_custom_resource(string stage)
    {
        string[] forbiddenPrefixes =
        [
            "AWS::EC2::", "AWS::ElasticLoadBalancing", "AWS::RDS::", "AWS::ElastiCache::", "AWS::ECS::",
            "AWS::EKS::", "AWS::OpenSearchService::", "AWS::Redshift", "AWS::CloudFormation::CustomResource", "Custom::"
        ];
        var offenders = For(stage).Resources().Where(r => forbiddenPrefixes.Any(p => r.Type.StartsWith(p, StringComparison.Ordinal))).ToList();
        Assert.True(offenders.Count == 0, "Unexpected resources: " + string.Join(", ", offenders.Select(o => $"{o.LogicalId} ({o.Type})")));
    }

    [Theory, MemberData(nameof(Stages))]
    public void Every_dotnet_lambda_uses_dotnet10(string stage)
    {
        var lambdas = For(stage).Resources().Where(r => r.Type == "AWS::Lambda::Function");
        foreach (var (_, id, _, resource) in lambdas)
        {
            var runtime = (string?)resource["Properties"]?["Runtime"] ?? "";
            if (runtime.StartsWith("dotnet", StringComparison.Ordinal))
                Assert.True(runtime == "dotnet10", $"{id} uses {runtime}");
        }
    }

    [Theory, MemberData(nameof(Stages))]
    public void Iam_policies_have_no_wildcard_actions_or_resources(string stage)
    {
        foreach (var (stack, id, _, resource) in For(stage).Resources().Where(r => r.Type is "AWS::IAM::Policy" or "AWS::IAM::Role"))
        {
            var statements = resource.ToJsonString();
            Assert.DoesNotContain("\"Action\":\"*\"", statements);
            Assert.DoesNotContain("\"Resource\":\"*\"", statements);
            Assert.DoesNotContain("\"Action\":[\"*\"]", statements);
            Assert.True(!statements.Contains("ManagedPolicyArns"), $"{stack.StackName}/{id} attaches managed policies");
        }
    }

    [Theory, MemberData(nameof(Stages))]
    public void Site_bucket_is_private_encrypted_and_tls_only(string stage)
    {
        var s = For(stage);
        var t = s.Template(s.Stacks.Web);
        t.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
        {
            ["PublicAccessBlockConfiguration"] = new Dictionary<string, object>
            {
                ["BlockPublicAcls"] = true, ["BlockPublicPolicy"] = true, ["IgnorePublicAcls"] = true, ["RestrictPublicBuckets"] = true
            },
            ["BucketEncryption"] = new Dictionary<string, object>
            {
                ["ServerSideEncryptionConfiguration"] = new object[]
                {
                    new Dictionary<string, object> { ["ServerSideEncryptionByDefault"] = new Dictionary<string, object> { ["SSEAlgorithm"] = "AES256" } }
                }
            }
        });
        var policy = s.Resources().Single(r => r.Type == "AWS::S3::BucketPolicy").Resource.ToJsonString();
        Assert.Contains("\"aws:SecureTransport\":\"false\"", policy);
        Assert.Contains("cloudfront.amazonaws.com", policy);
    }

    [Theory, MemberData(nameof(Stages))]
    public void Distribution_enforces_https_with_oac_and_modern_tls(string stage)
    {
        var s = For(stage);
        var dist = s.Resources().Single(r => r.Type == "AWS::CloudFront::Distribution").Resource["Properties"]!["DistributionConfig"]!;
        Assert.Equal("redirect-to-https", (string)dist["DefaultCacheBehavior"]!["ViewerProtocolPolicy"]!);
        Assert.Equal("TLSv1.2_2021", (string)dist["ViewerCertificate"]!["MinimumProtocolVersion"]!);
        Assert.Equal("sni-only", (string)dist["ViewerCertificate"]!["SslSupportMethod"]!);
        Assert.NotNull(dist["Origins"]![0]!["OriginAccessControlId"]);
        Assert.Single(s.Resources(), r => r.Type == "AWS::CloudFront::OriginAccessControl");
    }

    [Fact]
    public void Budget_filters_on_application_tag_and_only_notifies_when_email_configured()
    {
        var budget = Dev.Resources().Single(r => r.Type == "AWS::Budgets::Budget").Resource["Properties"]!;
        Assert.Equal("user:Application$Amendment", (string)budget["Budget"]!["CostFilters"]!["TagKeyValue"]![0]!);
        Assert.Null(budget["NotificationsWithSubscribers"]);

        var withEmail = SynthesizedStage.Create("dev", new Dictionary<string, object> { ["alertEmail"] = "alerts@example.com" });
        var notified = withEmail.Resources().Single(r => r.Type == "AWS::Budgets::Budget").Resource["Properties"]!;
        Assert.Equal(2, notified["NotificationsWithSubscribers"]!.AsArray().Count);
        Assert.Single(withEmail.Resources(), r => r.Type == "AWS::SNS::Subscription");
    }

    [Theory, MemberData(nameof(Stages))]
    public void Api_logs_have_retention_and_api_errors_alarm(string stage)
    {
        var s = For(stage);
        var logGroup = s.Resources().Single(r => r.Type == "AWS::Logs::LogGroup").Resource["Properties"]!;
        Assert.Equal(s.Config.LogRetentionDays, (int)logGroup["RetentionInDays"]!);
        var api = s.Resources().Single(r => r.Type == "AWS::AppSync::GraphQLApi").Resource["Properties"]!;
        Assert.Equal("ERROR", (string)api["LogConfig"]!["FieldLogLevel"]!);
        Assert.Equal("AMAZON_COGNITO_USER_POOLS", (string)api["AuthenticationType"]!);
        Assert.DoesNotContain(s.Resources(), r => r.Type == "AWS::AppSync::ApiKey");
        var alarm = s.Resources().Single(r => r.Type == "AWS::CloudWatch::Alarm").Resource["Properties"]!;
        Assert.Equal("5XXError", (string)alarm["MetricName"]!);
        Assert.NotNull(alarm["AlarmActions"]);
    }
}
