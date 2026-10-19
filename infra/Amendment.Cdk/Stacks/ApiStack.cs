using Amazon.CDK;
using Amazon.CDK.AWS.AppSync;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amendment.Cdk.Config;
using Constructs;
using LambdaCode = Amazon.CDK.AWS.Lambda.Code;
using LambdaFunction = Amazon.CDK.AWS.Lambda.Function;

namespace Amendment.Cdk.Stacks;

/// <summary>
/// AppSync GraphQL API: Cognito user pool (default), IAM, and the public projector Lambda authorizer (decision D21);
/// error-level logs. Amendment reads resolve directly against DynamoDB (JS resolvers, read-only role); mutations and
/// the presentation reads go through the .NET 10 API Lambda (decisions D15, D22).
/// </summary>
public sealed class ApiStack : Stack
{
    /// <summary>CDK context key for the published API Lambda folder (default <c>artifacts/lambda/api</c> under the repository root).</summary>
    public const string ApiLambdaPackageContextKey = "apiLambdaPackage";

    /// <summary>CDK context key for a replacement schema file (used only by <c>floci-validate.ps1</c>; default <c>Schema/amendment.graphql</c>).</summary>
    public const string SchemaFileContextKey = "schemaFile";

    public static readonly string[] MutationFields =
    [
        "createAmendment", "updateAmendment", "archiveAmendment", "cloneAmendment", "approveAmendment", "deleteAmendment",
        "createAmendmentBody", "updateAmendmentBody", "deleteAmendmentBody",
        "presentAmendment", "clearPresentation", "setLanguagePresentation"
    ];

    /// <summary>Query fields resolved by the API Lambda (decision D22).</summary>
    public static readonly string[] LambdaQueryFields = ["presentation", "publicPresentation"];

    public GraphqlApi Api { get; }
    public LambdaFunction ApiFunction { get; }
    public LambdaFunction PublicAuthorizerFunction { get; }

    /// <summary>The fixed, non-secret token anonymous projectors send in the <c>Authorization</c> header (decision D21).</summary>
    public static string PublicToken(StageConfig config) => $"{config.Prefix}-public-projector";

    public ApiStack(Construct scope, string id, StageConfig config, IUserPool userPool, ITable table, IStackProps props) : base(scope, id, props)
    {
        PublicAuthorizerFunction = CreatePublicAuthorizerFunction(config);
        Api = new GraphqlApi(this, "GraphqlApi", new GraphqlApiProps
        {
            Name = $"{config.Prefix}-api",
            Definition = Definition.FromFile(Node.TryGetContext(SchemaFileContextKey) as string is { Length: > 0 } schemaFile
                ? schemaFile
                : Path.Combine(AppContext.BaseDirectory, "Schema", "amendment.graphql")),
            AuthorizationConfig = new AuthorizationConfig
            {
                DefaultAuthorization = new AuthorizationMode
                {
                    AuthorizationType = AuthorizationType.USER_POOL,
                    UserPoolConfig = new UserPoolConfig { UserPool = userPool }
                },
                AdditionalAuthorizationModes =
                [
                    new AuthorizationMode { AuthorizationType = AuthorizationType.IAM },
                    new AuthorizationMode
                    {
                        AuthorizationType = AuthorizationType.LAMBDA,
                        LambdaAuthorizerConfig = new LambdaAuthorizerConfig
                        {
                            Handler = PublicAuthorizerFunction,
                            ResultsCacheTtl = Duration.Hours(1),
                            ValidationRegex = "^" + System.Text.RegularExpressions.Regex.Escape(PublicToken(config)) + "$"
                        }
                    }
                ]
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
            Code = Amazon.CDK.AWS.AppSync.Code.FromInline(
                "export function request(ctx) { return { payload: 'phase-5' }; }\n" +
                "export function response(ctx) { return ctx.result; }\n")
        });

        var tableSource = new DynamoDbDataSource(this, "TableReadSource", new DynamoDbDataSourceProps
        {
            Api = Api,
            Table = table,
            ReadOnlyAccess = true,
            Name = "AmendmentTableRead"
        });
        foreach (var (type, field) in new[] { ("Query", "amendments"), ("Query", "amendment"), ("Query", "languages"), ("Amendment", "bodies") })
            tableSource.CreateResolver($"{type}{char.ToUpperInvariant(field[0])}{field[1..]}Resolver", JsResolver(type, field, $"{type}.{field}.js"));

        ApiFunction = CreateApiFunction(config, table);
        var lambdaSource = Api.AddLambdaDataSource("ApiLambdaSource", ApiFunction, new DataSourceOptions { Name = "ApiLambda" });
        foreach (var field in MutationFields)
            lambdaSource.CreateResolver($"Mutation{char.ToUpperInvariant(field[0])}{field[1..]}Resolver", JsResolver("Mutation", field, "invoke-api.js"));
        foreach (var field in LambdaQueryFields)
            lambdaSource.CreateResolver($"Query{char.ToUpperInvariant(field[0])}{field[1..]}Resolver", JsResolver("Query", field, "invoke-api.js"));

        _ = new CfnOutput(this, "GraphqlUrl", new CfnOutputProps { Value = Api.GraphqlUrl });
        _ = new CfnOutput(this, "PublicToken", new CfnOutputProps { Value = PublicToken(config) });
        _ = new CfnOutput(this, "GraphqlApiId", new CfnOutputProps { Value = Api.ApiId });
        _ = new CfnOutput(this, "ApiFunctionName", new CfnOutputProps { Value = ApiFunction.FunctionName });
    }

    private LambdaFunction CreateApiFunction(StageConfig config, ITable table)
    {
        var functionName = $"{config.Prefix}-api";
        var logGroup = new LogGroup(this, "ApiFunctionLogGroup", new LogGroupProps
        {
            LogGroupName = $"/aws/lambda/{functionName}",
            Retention = Retention.FromDays(config.LogRetentionDays),
            RemovalPolicy = config.IsProduction ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
        });

        // Inline least-privilege policy instead of the AWSLambdaBasicExecutionRole managed policy.
        var role = new Role(this, "ApiFunctionRole", new RoleProps { AssumedBy = new ServicePrincipal("lambda.amazonaws.com") });
        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = ["logs:CreateLogStream", "logs:PutLogEvents"],
            Resources = [logGroup.LogGroupArn, $"{logGroup.LogGroupArn}:log-stream:*"]
        }));
        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = ["dynamodb:GetItem", "dynamodb:Query", "dynamodb:PutItem", "dynamodb:DeleteItem", "dynamodb:ConditionCheckItem"],
            Resources = [table.TableArn]
        }));

        return new LambdaFunction(this, "ApiFunction", new FunctionProps
        {
            FunctionName = functionName,
            Runtime = Runtime.DOTNET_10,
            Architecture = Architecture.ARM_64,
            Handler = "Amendment.Api.Lambda::Amendment.Api.Lambda.Function::HandleAsync",
            Code = LambdaCode.FromAsset(ApiLambdaPackagePath()),
            MemorySize = 1024,
            Timeout = Duration.Seconds(15),
            Role = role,
            LogGroup = logGroup,
            Environment = new Dictionary<string, string> { ["AMENDMENT_TABLE_NAME"] = table.TableName }
        });
    }

    private LambdaFunction CreatePublicAuthorizerFunction(StageConfig config)
    {
        var functionName = $"{config.Prefix}-public-authorizer";
        var logGroup = new LogGroup(this, "PublicAuthorizerLogGroup", new LogGroupProps
        {
            LogGroupName = $"/aws/lambda/{functionName}",
            Retention = Retention.FromDays(config.LogRetentionDays),
            RemovalPolicy = config.IsProduction ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
        });

        // Logs only: the authorizer reads no data.
        var role = new Role(this, "PublicAuthorizerRole", new RoleProps { AssumedBy = new ServicePrincipal("lambda.amazonaws.com") });
        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = ["logs:CreateLogStream", "logs:PutLogEvents"],
            Resources = [logGroup.LogGroupArn, $"{logGroup.LogGroupArn}:log-stream:*"]
        }));

        return new LambdaFunction(this, "PublicAuthorizerFunction", new FunctionProps
        {
            FunctionName = functionName,
            Runtime = Runtime.NODEJS_24_X,
            Architecture = Architecture.ARM_64,
            Handler = "index.handler",
            Code = LambdaCode.FromInline(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Authorizer", "public-authorizer.js"))),
            MemorySize = 128,
            Timeout = Duration.Seconds(3),
            Role = role,
            LogGroup = logGroup,
            Environment = new Dictionary<string, string> { ["PUBLIC_TOKEN"] = PublicToken(config) }
        });
    }

    private static BaseResolverProps JsResolver(string typeName, string fieldName, string file)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Resolvers");
        return new BaseResolverProps
        {
            TypeName = typeName,
            FieldName = fieldName,
            Runtime = FunctionRuntime.JS_1_0_0,
            Code = Amazon.CDK.AWS.AppSync.Code.FromInline(File.ReadAllText(Path.Combine(dir, "_shared.js")) + "\n" + File.ReadAllText(Path.Combine(dir, file)))
        };
    }

    private string ApiLambdaPackagePath()
    {
        var path = Node.TryGetContext(ApiLambdaPackageContextKey) as string;
        if (string.IsNullOrEmpty(path))
        {
            var root = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (root is not null && !File.Exists(Path.Combine(root.FullName, "global.json")))
                root = root.Parent;
            path = Path.Combine(root?.FullName ?? Directory.GetCurrentDirectory(), "artifacts", "lambda", "api");
        }
        if (!File.Exists(Path.Combine(path, "Amendment.Api.Lambda.dll")))
            throw new InvalidOperationException(
                $"The API Lambda package was not found in '{path}'. Run: dotnet publish src/Amendment.Api.Lambda -c Release -o artifacts/lambda/api");
        return path;
    }
}
