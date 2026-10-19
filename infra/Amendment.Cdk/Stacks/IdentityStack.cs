using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amendment.Cdk.Config;
using Constructs;

namespace Amendment.Cdk.Stacks;

/// <summary>Cognito user pool, legacy role groups and a public SPA client (authorization code + PKCE).</summary>
public sealed class IdentityStack : Stack
{
    /// <summary>
    /// Cognito group names cannot contain spaces, so each legacy role (<c>Amendment.Domain.Authorization.Roles</c>)
    /// maps to a space-free group name. The legacy role name is kept as the group description.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> RoleGroups = new Dictionary<string, string>
    {
        ["SystemAdministrator"] = "System Administrator",
        ["ScreenController"] = "Screen Controller",
        ["AmendmentEditor"] = "Amendment Editor",
        ["Translator"] = "Translator"
    };

    public UserPool UserPool { get; }
    public UserPoolClient WebClient { get; }

    public IdentityStack(Construct scope, string id, StageConfig config, IStackProps props) : base(scope, id, props)
    {
        UserPool = new UserPool(this, "UserPool", new UserPoolProps
        {
            UserPoolName = $"{config.Prefix}-users",
            SelfSignUpEnabled = false,
            SignInAliases = new SignInAliases { Username = true, Email = true },
            SignInCaseSensitive = false,
            AutoVerify = new AutoVerifiedAttrs { Email = true },
            PasswordPolicy = new PasswordPolicy
            {
                MinLength = 12,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigits = true,
                RequireSymbols = false,
                TempPasswordValidity = Duration.Days(7)
            },
            AccountRecovery = AccountRecovery.EMAIL_ONLY,
            DeletionProtection = config.IsProduction,
            RemovalPolicy = config.IsProduction ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
        });

        foreach (var (groupName, legacyRole) in RoleGroups)
        {
            _ = new CfnUserPoolGroup(this, $"Group{groupName}", new CfnUserPoolGroupProps
            {
                UserPoolId = UserPool.UserPoolId,
                GroupName = groupName,
                Description = legacyRole
            });
        }

        var callbackUrls = new[] { config.SiteUrl + "/" }.Concat(config.AdditionalCallbackUrls).ToArray();
        WebClient = UserPool.AddClient("WebClient", new UserPoolClientOptions
        {
            UserPoolClientName = $"{config.Prefix}-web",
            GenerateSecret = false,
            AuthFlows = new AuthFlow { UserSrp = true },
            PreventUserExistenceErrors = true,
            OAuth = new OAuthSettings
            {
                Flows = new OAuthFlows { AuthorizationCodeGrant = true },
                Scopes = [OAuthScope.OPENID, OAuthScope.EMAIL, OAuthScope.PROFILE],
                CallbackUrls = callbackUrls,
                LogoutUrls = callbackUrls
            },
            AccessTokenValidity = Duration.Minutes(60),
            IdTokenValidity = Duration.Minutes(60),
            RefreshTokenValidity = Duration.Days(30)
        });

        _ = new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = UserPool.UserPoolId });
        _ = new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = WebClient.UserPoolClientId });
    }
}
