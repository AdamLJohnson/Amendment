using Amendment.Client.AuthProviders;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Blazored.LocalStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Amendment.Client.Repository
{
    public class AccountRepository : IAccountRepository
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly JsonSerializerOptions _options;

        public AccountRepository(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<bool> ChangePassword(ChangePasswordRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Account/ChangePassword", request);

            if (response.IsSuccessStatusCode)
            {
                // Update the RequirePasswordChange flag in local storage
                var authResponse = await _localStorage.GetItemAsync<AccountLoginResponse>("authResponse");
                if (authResponse != null)
                {
                    authResponse.RequirePasswordChange = false;
                    await _localStorage.SetItemAsync("authResponse", authResponse);
                }
                return true;
            }

            return false;
        }

        public async Task<bool> GetRequirePasswordChange()
        {
            // Get the RequirePasswordChange flag from local storage
            var authToken = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrEmpty(authToken))
                return false;

            try
            {
                // Get the current auth response from local storage
                var authResponse = await _localStorage.GetItemAsync<AccountLoginResponse>("authResponse");
                if (authResponse != null)
                {
                    return authResponse.RequirePasswordChange;
                }

                // If not found in local storage, make a refresh token request to get the current state
                var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshRequest = new RefreshTokenRequest
                    {
                        Token = authToken,
                        RefreshToken = refreshToken
                    };

                    var response = await _httpClient.PostAsJsonAsync("api/Account/RefreshToken", refreshRequest);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadFromJsonAsync<ApiResult<AccountLoginResponse>>(_options);
                        if (content?.Result != null)
                        {
                            await _localStorage.SetItemAsync("authResponse", content.Result);
                            return content.Result.RequirePasswordChange;
                        }
                    }
                }
            }
            catch
            {
                // If any error occurs, default to false
                return false;
            }

            return false;
        }
    }
}
