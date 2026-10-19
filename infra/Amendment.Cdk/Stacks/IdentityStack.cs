using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using Amendment.Cdk.Config;
using Constructs;

namespace Amendment.Cdk.Stacks;

/// <summary>
/// Cognito user pool, legacy role groups, a public SPA client (authorization code + PKCE) and the sign-in pages on a
/// custom domain. Cognito requires the parent (site) domain to resolve before it creates the custom domain, so this
/// stack deploys after <see cref="WebStack"/>.
/// </summary>
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
    public UserPoolDomain SignInDomain { get; }

    /// <summary>Path the Blazor OIDC library returns to after Cognito sign-in.</summary>
    public const string LoginCallbackPath = "authentication/login-callback";

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

        var siteUrls = new[] { config.SiteUrl + "/" }.Concat(config.AdditionalCallbackUrls).ToArray();
        var callbackUrls = siteUrls.Select(url => url + LoginCallbackPath).ToArray();
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
                LogoutUrls = siteUrls
            },
            AccessTokenValidity = Duration.Minutes(60),
            IdTokenValidity = Duration.Minutes(60),
            RefreshTokenValidity = Duration.Days(30)
        });

        var zone = HostedZone.FromHostedZoneAttributes(this, "SharedZone", new HostedZoneAttributes
        {
            HostedZoneId = config.Domain.HostedZoneId,
            ZoneName = config.Domain.HostedZoneName
        });
        var signInCertificate = new Certificate(this, "SignInCertificate", new CertificateProps
        {
            DomainName = config.SignInDomainName,
            Validation = CertificateValidation.FromDns(zone)
        });
        SignInDomain = UserPool.AddDomain("SignInDomain", new UserPoolDomainOptions
        {
            CustomDomain = new CustomDomainOptions { DomainName = config.SignInDomainName, Certificate = signInCertificate }
        });
        // L1 record: UserPoolDomainTarget adds a Lambda-backed custom resource unless a feature flag is set (D14).
        _ = new CfnRecordSet(this, "SignInAliasA", new CfnRecordSetProps
        {
            HostedZoneId = config.Domain.HostedZoneId,
            Name = config.SignInDomainName + ".",
            Type = "A",
            AliasTarget = new CfnRecordSet.AliasTargetProperty
            {
                DnsName = SignInDomain.CloudFrontEndpoint,
                HostedZoneId = CloudFrontTarget.CLOUDFRONT_ZONE_ID
            }
        });

        _ = new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = UserPool.UserPoolId });
        _ = new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = WebClient.UserPoolClientId });
        _ = new CfnOutput(this, "SignInDomainName", new CfnOutputProps { Value = config.SignInDomainName });

        if (!config.IsProduction)
        {
            // Non-production only (decision D18): API tests sign in throwaway users with ADMIN_USER_PASSWORD_AUTH,
            // which requires IAM credentials (cognito-idp:AdminInitiateAuth), so this client is not usable from a browser.
            TestClient = UserPool.AddClient("TestClient", new UserPoolClientOptions
            {
                UserPoolClientName = $"{config.Prefix}-api-tests",
                GenerateSecret = false,
                AuthFlows = new AuthFlow { AdminUserPassword = true },
                DisableOAuth = true,
                PreventUserExistenceErrors = true,
                AccessTokenValidity = Duration.Minutes(60),
                IdTokenValidity = Duration.Minutes(60),
                RefreshTokenValidity = Duration.Hours(1)
            });
            _ = new CfnOutput(this, "TestClientId", new CfnOutputProps { Value = TestClient.UserPoolClientId });
        }
    }

    /// <summary>Non-production API test client; null in production.</summary>
    public UserPoolClient? TestClient { get; }
}
