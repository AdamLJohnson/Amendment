using Amazon.CDK;
using Amazon.CDK.AWS.AppSync;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amendment.Cdk.Config;
using Constructs;

namespace Amendment.Cdk.Stacks;

/// <summary>Basic AppSync GraphQL API: Cognito user pool (default) + IAM auth, placeholder schema, error-level logs.</summary>
public sealed class ApiStack : Stack
{
    public GraphqlApi Api { get; }

    public ApiStack(Construct scope, string id, StageConfig config, IUserPool userPool, IStackProps props) : base(scope, id, props)
    {
        Api = new GraphqlApi(this, "GraphqlApi", new GraphqlApiProps
        {
            Name = $"{config.Prefix}-api",
            Definition = Definition.FromFile(Path.Combine(AppContext.BaseDirectory, "Schema", "foundation.graphql")),
            AuthorizationConfig = new AuthorizationConfig
            {
                DefaultAuthorization = new AuthorizationMode
                {
                    AuthorizationType = AuthorizationType.USER_POOL,
                    UserPoolConfig = new UserPoolConfig { UserPool = userPool }
                },
                AdditionalAuthorizationModes = [new AuthorizationMode { AuthorizationType = AuthorizationType.IAM }]
            },
            XrayEnabled = false
        });

        var logGroup = new LogGroup(this, "ApiLogGroup", new LogGroupProps
        {
            LogGroupName = $"/aws/appsync/apis/{Api.ApiId}",
            Retention = Retention.FromDays(config.LogRetentionDays),
            RemovalPolicy = config.IsProduction ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
        });

        // Configured on the L1 resource so CDK does not add a Lambda-backed Custom::LogRetention.
        var logsRole = new Role(this, "ApiLogsRole", new RoleProps { AssumedBy = new ServicePrincipal("appsync.amazonaws.com") });
        logsRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = ["logs:CreateLogStream", "logs:PutLogEvents"],
            Resources = [logGroup.LogGroupArn, $"{logGroup.LogGroupArn}:log-stream:*"]
        }));
        var cfnApi = (CfnGraphQLApi)Api.Node.DefaultChild!;
        cfnApi.LogConfig = new CfnGraphQLApi.LogConfigProperty
        {
            CloudWatchLogsRoleArn = logsRole.RoleArn,
            FieldLogLevel = "ERROR",
            ExcludeVerboseContent = true
        };

        var none = Api.AddNoneDataSource("None");
        none.CreateResolver("QueryApiVersion", new BaseResolverProps
        {
            TypeName = "Query",
            FieldName = "apiVersion",
            Runtime = FunctionRuntime.JS_1_0_0,
            Code = Code.FromInline(
                "export function request(ctx) { return { payload: 'phase-2' }; }\n" +
                "export function response(ctx) { return ctx.result; }\n")
        });

        _ = new CfnOutput(this, "GraphqlUrl", new CfnOutputProps { Value = Api.GraphqlUrl });
        _ = new CfnOutput(this, "GraphqlApiId", new CfnOutputProps { Value = Api.ApiId });
    }
}
