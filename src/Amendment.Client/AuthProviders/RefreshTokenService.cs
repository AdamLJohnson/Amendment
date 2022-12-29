using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Amendment.Client.AuthProviders
{
    public interface IRefreshTokenService
    {
        Task<string> TryRefreshToken();
    }

    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AuthenticationStateProvider _authProvider;
        private readonly IAuthenticationService _authService;
        private readonly ILocalStorageService _localStorage;

        public RefreshTokenService(AuthenticationStateProvider authProvider, IAuthenticationService authService, ILocalStorageService localStorage)
        {
            _authProvider = authProvider;
            _authService = authService;
            _localStorage = localStorage;
        }

        public async Task<string> TryRefreshToken()
        {
            var authState = await _authProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var exp = user.FindFirst(c => c.Type.Equals("exp")).Value;
            var expTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp));
            var timeUTC = DateTime.UtcNow;
            var diff = expTime - timeUTC;
            if (diff.TotalMinutes <= 2)
                return await _authService.RefreshToken();

            var token = await _localStorage.GetItemAsync<string>("authToken");
            return token;
        }
    }
}
