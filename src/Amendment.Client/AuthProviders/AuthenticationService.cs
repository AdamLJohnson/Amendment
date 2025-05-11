using Amendment.Shared.Requests;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Amendment.Shared;
using Amendment.Shared.Responses;

namespace Amendment.Client.AuthProviders;

public interface IAuthenticationService
{
    //Task<RegistrationResponseDto> RegisterUser(UserForRegistrationDto userForRegistration);
    Task<AuthResponseDto> Login(AccountLoginRequest userForAuthentication);
    Task Logout();
    Task<string> RefreshToken();
}

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _options;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;

    public AuthenticationService(HttpClient client, AuthenticationStateProvider authStateProvider, ILocalStorageService localStorage)
    {
        _client = client;
        _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
    }

    //public async Task<RegistrationResponseDto> RegisterUser(UserForRegistrationDto userForRegistration)
    //{
    //    var content = JsonSerializer.Serialize(userForRegistration);
    //    var bodyContent = new StringContent(content, Encoding.UTF8, "application/json");

    //    var registrationResult = await _client.PostAsync("accounts/registration", bodyContent);
    //    var registrationContent = await registrationResult.Content.ReadAsStringAsync();

    //    if (!registrationResult.IsSuccessStatusCode)
    //    {
    //        var result = JsonSerializer.Deserialize<RegistrationResponseDto>(registrationContent, _options);
    //        return result;
    //    }

    //    return new RegistrationResponseDto { IsSuccessfulRegistration = true };
    //}

    public async Task<AuthResponseDto> Login(AccountLoginRequest userForAuthentication)
    {
        var content = JsonSerializer.Serialize(userForAuthentication);
        var bodyContent = new StringContent(content, Encoding.UTF8, "application/json");

        var authResult = await _client.PostAsync("api/Account/Login", bodyContent);
        var authContent = await authResult.Content.ReadAsStringAsync();
        if (!authResult.IsSuccessStatusCode)
            return new AuthResponseDto { IsAuthSuccessful = false, ErrorMessage = "Login Failed"};

        var result = JsonSerializer.Deserialize<ApiResult<AccountLoginResponse>>(authContent, _options) ?? throw new ArgumentNullException("JsonSerializer.Deserialize<ApiResult<AccountLoginResponse>>(authContent, _options)");


        if (result.Result != null)
        {
            await _localStorage.SetItemAsync("authToken", result.Result.Token);
            await _localStorage.SetItemAsync("refreshToken", result.Result.RefreshToken);
            await _localStorage.SetItemAsync("authResponse", result.Result);
            ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Result.Token!);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", result.Result.Token);
            return new AuthResponseDto {
                IsAuthSuccessful = true,
                RequirePasswordChange = result.Result.RequirePasswordChange
            };
        }
        return new AuthResponseDto { IsAuthSuccessful = false, ErrorMessage = "Login Failed" };
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        ((AuthStateProvider)_authStateProvider).NotifyUserLogout();
        _client.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<string> RefreshToken()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");

        var tokenDto = JsonSerializer.Serialize(new RefreshTokenRequest() { Token = token, RefreshToken = refreshToken });
        var bodyContent = new StringContent(tokenDto, Encoding.UTF8, "application/json");
        var refreshResult = await _client.PostAsync("api/Account/RefreshToken", bodyContent);
        var refreshContent = await refreshResult.Content.ReadAsStringAsync();
        if(!refreshResult.IsSuccessStatusCode)
        {
            await Logout();
            return "";
        }

        var result = JsonSerializer.Deserialize<ApiResult<AccountLoginResponse>>(refreshContent, _options) ?? throw new ArgumentNullException("JsonSerializer.Deserialize<ApiResult<AccountLoginResponse>>(refreshContent, _options)");

        if (result.Result == null)
            return "";

        await _localStorage.SetItemAsync("authToken", result.Result.Token);
        await _localStorage.SetItemAsync("refreshToken", result.Result.RefreshToken);
        await _localStorage.SetItemAsync("authResponse", result.Result);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", result.Result.Token);
        return result.Result.Token!;
    }
}

public class AuthResponseDto
{
    public bool IsAuthSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequirePasswordChange { get; set; }
}