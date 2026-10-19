using Amendment.Client.Repository.GraphQl;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Amendment.Client.AuthProviders;

/// <summary>
/// Cognito sign-in and the typed GraphQL client behind the <c>Features:CognitoGraphQlPreview</c> flag (decision D17).
/// Settings come from <c>amendment-config.json</c>, which <c>infra/scripts/publish-web.ps1</c> writes per stage.
/// </summary>
public sealed record CognitoGraphQlPreview(bool Enabled, string Region, string UserPoolId, string ClientId, string SignInDomain, string GraphQlUrl)
{
    public const string FlagKey = "Features:CognitoGraphQlPreview";

    public static CognitoGraphQlPreview FromConfiguration(IConfiguration config)
    {
        var settings = new CognitoGraphQlPreview(
            config.GetValue<bool>(FlagKey),
            config["region"] ?? "",
            config["userPoolId"] ?? "",
            config["userPoolClientId"] ?? "",
            config["signInDomain"] ?? "",
            config["graphqlUrl"] ?? "");
        var complete = new[] { settings.Region, settings.UserPoolId, settings.ClientId, settings.SignInDomain, settings.GraphQlUrl }.All(v => v.Length > 0);
        return settings with { Enabled = settings.Enabled && complete };
    }

    public string Authority => $"https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}";

    /// <summary>Cognito hosted sign-out; returns to <paramref name="siteRoot"/>, which must be a registered logout URL.</summary>
    public string SignOutUrl(string siteRoot) =>
        $"https://{SignInDomain}/logout?client_id={Uri.EscapeDataString(ClientId)}&logout_uri={Uri.EscapeDataString(siteRoot)}";

    public void Register(IServiceCollection services)
    {
        services.AddSingleton(this);
        if (!Enabled)
            return;

        services.AddOidcAuthentication(options =>
        {
            options.ProviderOptions.Authority = Authority;
            options.ProviderOptions.ClientId = ClientId;
            options.ProviderOptions.ResponseType = "code";
            options.ProviderOptions.DefaultScopes.Add("email");
            options.UserOptions.RoleClaim = "cognito:groups";
        });
        services.AddScoped<IAmendmentApi>(sp => new AmendmentApiClient(
            new HttpClient(new CognitoAccessTokenHandler(sp.GetRequiredService<IAccessTokenProvider>()) { InnerHandler = new HttpClientHandler() })
            {
                BaseAddress = new Uri(GraphQlUrl)
            }));
    }
}

/// <summary>Adds the Cognito access token as the raw <c>Authorization</c> header that AppSync expects.</summary>
public sealed class CognitoAccessTokenHandler(IAccessTokenProvider tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var result = await tokens.RequestAccessToken();
        if (!result.TryGetToken(out var token))
            throw new AmendmentApiException(AmendmentApiException.Unauthorized, "Sign in with Cognito first.");
        request.Headers.TryAddWithoutValidation("Authorization", token.Value);
        return await base.SendAsync(request, cancellationToken);
    }
}
