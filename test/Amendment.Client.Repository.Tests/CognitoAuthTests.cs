using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Amendment.Client.AuthProviders;
using Amendment.Client.Repository.GraphQl;
using Amendment.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.JSInterop;
using NSubstitute;

namespace Amendment.Client.Repository.Tests;

/// <summary>Cognito group-to-role mapping and the GraphQL token handler.</summary>
public sealed class CognitoAuthTests
{
    private const string StoreKey = "oidc.user:authority:client";
    private const string PublicToken = "public-token";

    private static async Task<ClaimsPrincipal> CreateUser(string accountJson)
    {
        var account = JsonSerializer.Deserialize<RemoteUserAccount>(accountJson)!;
        var options = new RemoteAuthenticationUserOptions { AuthenticationType = "client", RoleClaim = CognitoAuth.GroupClaim, NameClaim = "email" };
        return await new CognitoClaimsPrincipalFactory(Substitute.For<IAccessTokenProviderAccessor>()).CreateUserAsync(account, options);
    }

    [Fact]
    public async Task Splits_group_list_into_roles_and_legacy_role_names()
    {
        var user = await CreateUser("""{"email":"a@example.test","cognito:groups":["SystemAdministrator","ScreenController"]}""");

        Assert.True(user.IsInRole("SystemAdministrator"));
        Assert.True(user.IsInRole("ScreenController"));
        Assert.True(user.IsInRole(RoleGroups.Admin));
        Assert.True(user.IsInRole(RoleGroups.ScreenController));
        Assert.False(user.IsInRole(RoleGroups.AmendmentEditor));
        Assert.DoesNotContain(user.FindAll(CognitoAuth.GroupClaim), c => c.Value.StartsWith('['));
    }

    [Fact]
    public async Task Keeps_unmapped_group_without_adding_a_legacy_role()
    {
        var user = await CreateUser("""{"email":"a@example.test","cognito:groups":["Other"]}""");

        var claim = Assert.Single(user.FindAll(CognitoAuth.GroupClaim));
        Assert.Equal("Other", claim.Value);
    }

    [Fact]
    public async Task User_without_groups_has_no_roles()
    {
        var user = await CreateUser("""{"email":"a@example.test"}""");

        Assert.True(user.Identity!.IsAuthenticated);
        Assert.Empty(user.FindAll(CognitoAuth.GroupClaim));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Authorization = request.Headers.TryGetValues("Authorization", out var values) ? values.Single() : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static IJSRuntime StoredUser(string? value)
    {
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<string?>("sessionStorage.getItem", Arg.Any<CancellationToken>(), Arg.Any<object?[]?>())
            .Returns(new ValueTask<string?>(value));
        return js;
    }

    private static async Task<string?> Send(AuthenticationStateProvider authState, IAccessTokenProvider tokens, IJSRuntime js, string publicToken = PublicToken)
    {
        var inner = new RecordingHandler();
        var handler = new CognitoAccessTokenHandler(authState, tokens, js, StoreKey, publicToken) { InnerHandler = inner };
        using var client = new HttpClient(handler);
        await client.PostAsync("https://api.example.test/graphql", new StringContent("{}"));
        return inner.Authorization;
    }

    [Fact]
    public async Task Signed_out_visitor_gets_public_token_without_asking_sign_in_library()
    {
        var authState = Substitute.For<AuthenticationStateProvider>();
        var tokens = Substitute.For<IAccessTokenProvider>();

        var authorization = await Send(authState, tokens, StoredUser(null));

        Assert.Equal(PublicToken, authorization);
        await authState.DidNotReceive().GetAuthenticationStateAsync();
        await tokens.DidNotReceive().RequestAccessToken();
    }

    [Fact]
    public async Task Signed_in_user_gets_access_token()
    {
        var authState = Substitute.For<AuthenticationStateProvider>();
        authState.GetAuthenticationStateAsync()
            .Returns(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim("email", "a@example.test")], "client"))));
        var tokens = Substitute.For<IAccessTokenProvider>();
        tokens.RequestAccessToken().Returns(new ValueTask<AccessTokenResult>(new AccessTokenResult(
            AccessTokenResultStatus.Success, new AccessToken { Value = "user-token", Expires = DateTimeOffset.UtcNow.AddHours(1) }, null, null)));

        var authorization = await Send(authState, tokens, StoredUser("{}"));

        Assert.Equal("user-token", authorization);
    }

    [Fact]
    public async Task Signed_out_visitor_without_public_token_is_rejected()
    {
        var error = await Assert.ThrowsAsync<AmendmentApiException>(() =>
            Send(Substitute.For<AuthenticationStateProvider>(), Substitute.For<IAccessTokenProvider>(), StoredUser(null), publicToken: ""));

        Assert.Equal(AmendmentApiException.Unauthorized, error.ErrorType);
    }
}
