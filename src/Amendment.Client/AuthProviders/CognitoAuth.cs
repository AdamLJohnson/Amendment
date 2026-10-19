using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amendment.Client.Repository.GraphQl;
using Amendment.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Amendment.Client.AuthProviders;

/// <summary>
/// Cognito sign-in and the typed GraphQL client (decision D23). Settings come from <c>amendment-config.json</c>, which
/// <c>infra/scripts/publish-web.ps1</c> writes per stage. Signed-out callers use the public projector token (D21).
/// </summary>
public sealed record CognitoAuth(string Region, string UserPoolId, string ClientId, string SignInDomain, string GraphQlUrl, string PublicToken)
{
    public const string GroupClaim = "cognito:groups";

    /// <summary>Cognito group name (no spaces) to the legacy role name used by <see cref="RoleGroups"/>.</summary>
    public static readonly IReadOnlyDictionary<string, string> GroupRoles = new Dictionary<string, string>
    {
        ["SystemAdministrator"] = RoleGroups.Admin,
        ["ScreenController"] = RoleGroups.ScreenController,
        ["AmendmentEditor"] = RoleGroups.AmendmentEditor,
        ["Translator"] = RoleGroups.Translator
    };

    public static CognitoAuth FromConfiguration(IConfiguration config) => new(
        config["region"] ?? "",
        config["userPoolId"] ?? "",
        config["userPoolClientId"] ?? "",
        config["signInDomain"] ?? "",
        config["graphqlUrl"] ?? "",
        config["publicToken"] ?? "");

    /// <summary>
    /// Cognito answers the library's hidden-frame silent sign-in (<c>prompt=none</c>) with its login page, which it
    /// refuses to show in a frame, so that attempt always waits out this timeout. Stored refresh tokens do not use it.
    /// </summary>
    public const int SilentRequestTimeoutMs = 1000;

    public string Authority => $"https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}";

    /// <summary>The <c>sessionStorage</c> key where the browser sign-in library keeps the signed-in user.</summary>
    public string UserStoreKey => $"oidc.user:{Authority}:{ClientId}";

    /// <summary>Cognito hosted sign-out; returns to <paramref name="siteRoot"/>, which must be a registered logout URL.</summary>
    public string SignOutUrl(string siteRoot) =>
        $"https://{SignInDomain}/logout?client_id={Uri.EscapeDataString(ClientId)}&logout_uri={Uri.EscapeDataString(siteRoot)}";

    public void Register(IServiceCollection services)
    {
        services.AddSingleton(this);
        services.AddScoped<IPostConfigureOptions<RemoteAuthenticationOptions<CognitoProviderOptions>>, CognitoProviderOptionsConfiguration>();
        services.AddRemoteAuthentication<RemoteAuthenticationState, RemoteUserAccount, CognitoProviderOptions>(options =>
            {
                options.ProviderOptions.Authority = Authority;
                options.ProviderOptions.ClientId = ClientId;
                options.ProviderOptions.ResponseType = "code";
                options.ProviderOptions.DefaultScopes.Add("email");
                options.ProviderOptions.SilentRequestTimeout = SilentRequestTimeoutMs;
                options.UserOptions.RoleClaim = GroupClaim;
                options.UserOptions.NameClaim = "email";
            })
            .AddAccountClaimsPrincipalFactory<CognitoClaimsPrincipalFactory>();
        services.AddScoped<IAmendmentApi>(sp => new AmendmentApiClient(
            new HttpClient(new CognitoAccessTokenHandler(
                sp.GetRequiredService<AuthenticationStateProvider>(), sp.GetRequiredService<IAccessTokenProvider>(),
                sp.GetRequiredService<IJSRuntime>(), UserStoreKey, PublicToken) { InnerHandler = new HttpClientHandler() })
            {
                BaseAddress = new Uri(GraphQlUrl.Length > 0 ? GraphQlUrl : "https://localhost/graphql")
            }));
    }
}

/// <summary>OIDC settings plus <c>silentRequestTimeout</c>, which the browser sign-in library reads but <see cref="OidcProviderOptions"/> omits.</summary>
public sealed class CognitoProviderOptions : OidcProviderOptions
{
    [JsonPropertyName("silentRequestTimeout")]
    public int SilentRequestTimeout { get; set; }
}

/// <summary>The defaults that <c>AddOidcAuthentication</c> applies to <see cref="OidcProviderOptions"/> only.</summary>
public sealed class CognitoProviderOptionsConfiguration(NavigationManager navigation) : IPostConfigureOptions<RemoteAuthenticationOptions<CognitoProviderOptions>>
{
    public void PostConfigure(string? name, RemoteAuthenticationOptions<CognitoProviderOptions> options)
    {
        if (name != Options.DefaultName)
            return;
        options.UserOptions.AuthenticationType ??= options.ProviderOptions.ClientId;
        options.ProviderOptions.RedirectUri = Absolute(options.ProviderOptions.RedirectUri, "authentication/login-callback");
        options.ProviderOptions.PostLogoutRedirectUri = Absolute(options.ProviderOptions.PostLogoutRedirectUri, "authentication/logout-callback");
    }

    private string Absolute(string? uri, string fallback) =>
        uri != null && Uri.TryCreate(uri, UriKind.Absolute, out _) ? uri : navigation.ToAbsoluteUri(uri ?? fallback).AbsoluteUri;
}

/// <summary>Adds the legacy role name for each Cognito group, so <see cref="RoleGroups"/> checks keep working.</summary>
public sealed class CognitoClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor) : AccountClaimsPrincipalFactory<RemoteUserAccount>(accessor)
{
    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
    {
        var user = await base.CreateUserAsync(account, options);
        if (user.Identity is ClaimsIdentity { IsAuthenticated: true } identity)
        {
            foreach (var claim in identity.FindAll(CognitoAuth.GroupClaim).ToList())
            {
                identity.RemoveClaim(claim);
                var groups = claim.Value.StartsWith('[')
                    ? JsonSerializer.Deserialize<string[]>(claim.Value) ?? []
                    : [claim.Value];
                foreach (var group in groups)
                {
                    identity.AddClaim(new Claim(identity.RoleClaimType, group));
                    if (CognitoAuth.GroupRoles.TryGetValue(group, out var role))
                        identity.AddClaim(new Claim(identity.RoleClaimType, role));
                }
            }
        }
        return user;
    }
}

/// <summary>
/// Adds the Cognito access token as the raw <c>Authorization</c> header that AppSync expects. Without a signed-in user it
/// sends the public token, which reaches only the anonymous fields. Without a stored user it skips the sign-in library
/// entirely, whose first check waits for a silent sign-in that Cognito never completes.
/// </summary>
public sealed class CognitoAccessTokenHandler(
    AuthenticationStateProvider authState, IAccessTokenProvider tokens, IJSRuntime js, string userStoreKey, string publicToken) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var signedIn = !string.IsNullOrEmpty(await js.InvokeAsync<string?>("sessionStorage.getItem", cancellationToken, userStoreKey))
            && (await authState.GetAuthenticationStateAsync()).User.Identity?.IsAuthenticated == true;
        if (signedIn && (await tokens.RequestAccessToken()).TryGetToken(out var token))
            request.Headers.TryAddWithoutValidation("Authorization", token.Value);
        else if (publicToken.Length > 0)
            request.Headers.TryAddWithoutValidation("Authorization", publicToken);
        else
            throw new AmendmentApiException(AmendmentApiException.Unauthorized, "Sign in first.");
        return await base.SendAsync(request, cancellationToken);
    }
}
