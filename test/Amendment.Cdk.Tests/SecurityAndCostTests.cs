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
        "AWS::Cognito::UserPoolClient", "AWS::Cognito::UserPoolGroup", "AWS::Cognito::UserPoolDomain", "AWS::CloudFront::OriginAccessControl",
        "AWS::Route53::RecordSet", "AWS::Budgets::Budget", "AWS::Lambda::Permission"
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
        var logGroups = s.Resources().Where(r => r.Type == "AWS::Logs::LogGroup").Select(r => r.Resource["Properties"]!).ToList();
        Assert.Equal(3, logGroups.Count); // AppSync API + API Lambda + public authorizer
        Assert.All(logGroups, g => Assert.Equal(s.Config.LogRetentionDays, (int)g["RetentionInDays"]!));
        var api = s.Resources().Single(r => r.Type == "AWS::AppSync::GraphQLApi").Resource["Properties"]!;
        Assert.Equal("ERROR", (string)api["LogConfig"]!["FieldLogLevel"]!);
        Assert.Equal("AMAZON_COGNITO_USER_POOLS", (string)api["AuthenticationType"]!);
        Assert.DoesNotContain(s.Resources(), r => r.Type == "AWS::AppSync::ApiKey");
        var alarm = s.Resources().Single(r => r.Type == "AWS::CloudWatch::Alarm").Resource["Properties"]!;
        Assert.Equal("5XXError", (string)alarm["MetricName"]!);
        Assert.NotNull(alarm["AlarmActions"]);
    }

    [Theory, MemberData(nameof(Stages))]
    public void Api_lambda_is_dotnet10_arm64_zip_with_least_privilege_table_access(string stage)
    {
        var s = For(stage);
        var fn = s.Resources().Single(r => r.Type == "AWS::Lambda::Function" && r.LogicalId.StartsWith("ApiFunction", StringComparison.Ordinal)).Resource["Properties"]!;
        Assert.Equal("dotnet10", (string)fn["Runtime"]!);
        Assert.Equal("arm64", (string)fn["Architectures"]![0]!);
        Assert.Equal("Amendment.Api.Lambda::Amendment.Api.Lambda.Function::HandleAsync", (string)fn["Handler"]!);
        Assert.NotNull(fn["Code"]!["S3Key"]);
        Assert.NotNull(fn["Environment"]!["Variables"]!["AMENDMENT_TABLE_NAME"]);

        var lambdaPolicy = s.Resources().Single(r => r.Type == "AWS::IAM::Policy" && r.LogicalId.StartsWith("ApiFunctionRole", StringComparison.Ordinal)).Resource.ToJsonString();
        foreach (var action in new[] { "dynamodb:GetItem", "dynamodb:Query", "dynamodb:PutItem", "dynamodb:DeleteItem", "dynamodb:ConditionCheckItem" })
            Assert.Contains(action, lambdaPolicy);
        Assert.DoesNotContain("dynamodb:Scan", lambdaPolicy);
        Assert.DoesNotContain("dynamodb:DeleteTable", lambdaPolicy);
    }

    [Theory, MemberData(nameof(Stages))]
    public void Reads_use_a_read_only_table_source_and_every_mutation_goes_through_the_lambda(string stage)
    {
        var s = For(stage);
        var resolvers = s.Resources().Where(r => r.Type == "AWS::AppSync::Resolver").Select(r => r.Resource["Properties"]!).ToList();
        string[] Fields(string type) => resolvers.Where(r => (string)r["TypeName"]! == type).Select(r => (string)r["FieldName"]!).Order().ToArray();

        Assert.Equal(Amendment.Cdk.Stacks.ApiStack.MutationFields.Order().ToArray(), Fields("Mutation"));
        Assert.Equal(["amendment", "amendments", "apiVersion", "languages", "presentation", "publicPresentation"], Fields("Query"));
        Assert.Equal(["bodies"], Fields("Amendment"));
        Assert.All(resolvers, r => Assert.Equal("APPSYNC_JS", (string)r["Runtime"]!["Name"]!));

        var readPolicy = s.Resources().Single(r => r.Type == "AWS::IAM::Policy" && r.LogicalId.StartsWith("TableReadSource", StringComparison.Ordinal)).Resource.ToJsonString();
        Assert.Contains("dynamodb:Query", readPolicy);
        Assert.DoesNotContain("dynamodb:PutItem", readPolicy);
        Assert.DoesNotContain("dynamodb:DeleteItem", readPolicy);
        Assert.DoesNotContain("dynamodb:UpdateItem", readPolicy);
    }

    [Theory, MemberData(nameof(Stages))]
    public void Public_projectors_use_a_cached_logs_only_node_authorizer_and_no_api_key(string stage)
    {
        var s = For(stage);
        var token = Amendment.Cdk.Stacks.ApiStack.PublicToken(s.Config);
        var api = s.Resources().Single(r => r.Type == "AWS::AppSync::GraphQLApi").Resource["Properties"]!;
        var lambdaMode = api["AdditionalAuthenticationProviders"]!.AsArray().Single(p => (string)p!["AuthenticationType"]! == "AWS_LAMBDA")!;
        Assert.Equal(3600, (int)lambdaMode["LambdaAuthorizerConfig"]!["AuthorizerResultTtlInSeconds"]!);
        Assert.Equal($"^{token}$", (string)lambdaMode["LambdaAuthorizerConfig"]!["IdentityValidationExpression"]!);
        Assert.DoesNotContain(s.Resources(), r => r.Type == "AWS::AppSync::ApiKey");

        var fn = s.Resources().Single(r => r.Type == "AWS::Lambda::Function" && r.LogicalId.StartsWith("PublicAuthorizerFunction", StringComparison.Ordinal)).Resource["Properties"]!;
        Assert.Equal("nodejs24.x", (string)fn["Runtime"]!);
        Assert.Equal("arm64", (string)fn["Architectures"]![0]!);
        Assert.Equal(token, (string)fn["Environment"]!["Variables"]!["PUBLIC_TOKEN"]!);
        Assert.Contains("event.authorizationToken === TOKEN", (string)fn["Code"]!["ZipFile"]!);

        var policy = s.Resources().Single(r => r.Type == "AWS::IAM::Policy" && r.LogicalId.StartsWith("PublicAuthorizerRole", StringComparison.Ordinal)).Resource.ToJsonString();
        Assert.DoesNotContain("dynamodb:", policy);
        var permission = s.Resources().Single(r => r.Type == "AWS::Lambda::Permission").Resource["Properties"]!;
        Assert.Equal("appsync.amazonaws.com", (string)permission["Principal"]!);
    }

    [Fact]
    public void Anonymous_schema_access_is_limited_to_public_fields()
    {
        var schema = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Schema", "amendment.graphql"));
        var lambdaLines = schema.Split('\n').Where(l => l.Contains("@aws_lambda", StringComparison.Ordinal)).Select(l => l.Trim().Split(' ', '(', ':')[0]).ToArray();
        Assert.Equal(["type", "type", "type", "languages", "publicPresentation"], lambdaLines.Where(l => !l.StartsWith('#')).ToArray());
        Assert.Contains("type Language @aws_lambda @aws_cognito_user_pools", schema);
        Assert.Contains("type PublicLanguageView @aws_lambda @aws_cognito_user_pools", schema);
        Assert.Contains("type PublicPresentation @aws_lambda @aws_cognito_user_pools", schema);
    }

    [Fact]
    public void Deployed_schema_is_the_real_schema_unless_a_replacement_file_is_given()
    {
        static string DeployedSchema(SynthesizedStage s) =>
            (string)s.Resources().Single(r => r.Type == "AWS::AppSync::GraphQLSchema").Resource["Properties"]!["Definition"]!;

        var schema = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Schema", "amendment.graphql"));
        Assert.Equal(schema, DeployedSchema(Dev));

        var replacement = Path.Combine(Path.GetTempPath(), "amendment-cdk-tests", "replacement.graphql");
        Directory.CreateDirectory(Path.GetDirectoryName(replacement)!);
        File.WriteAllText(replacement, "type Query { ping: String }");
        var replaced = SynthesizedStage.Create("dev", new Dictionary<string, object> { [Amendment.Cdk.Stacks.ApiStack.SchemaFileContextKey] = replacement });
        Assert.Equal("type Query { ping: String }", DeployedSchema(replaced));
    }
}
