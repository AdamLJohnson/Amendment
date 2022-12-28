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

        var result = JsonSerializer.Deserialize<ApiResult<AccountLoginResponse>>(authContent, _options);



        await _localStorage.SetItemAsync("authToken", result.Result.Token);
        ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(userForAuthentication.Username);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", result.Result.Token);

        return new AuthResponseDto { IsAuthSuccessful = true };
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        ((AuthStateProvider)_authStateProvider).NotifyUserLogout();
        _client.DefaultRequestHeaders.Authorization = null;
    }
}

public class AuthResponseDto
{
    public bool IsAuthSuccessful { get; set; }
    public string ErrorMessage { get; set; }
}