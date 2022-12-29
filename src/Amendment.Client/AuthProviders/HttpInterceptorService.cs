using System.Net.Http.Headers;
using Toolbelt.Blazor;

namespace Amendment.Client.AuthProviders
{
    public class HttpInterceptorService
    {
        private readonly HttpClientInterceptor _interceptor;
        private readonly IRefreshTokenService _refreshTokenService;

        public HttpInterceptorService(HttpClientInterceptor interceptor, IRefreshTokenService refreshTokenService)
        {
            _interceptor = interceptor;
            _refreshTokenService = refreshTokenService;
        }

        public void RegisterEvent() => _interceptor.BeforeSendAsync += InterceptBeforeHttpAsync;
        public async Task InterceptBeforeHttpAsync(object sender, HttpClientInterceptorEventArgs e)
        {
            var absPath = e.Request.RequestUri.AbsolutePath;
            if (absPath.Contains("api/account/login", StringComparison.CurrentCultureIgnoreCase) ||
                absPath.Contains("api/account/refreshtoken", StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }

            var token = await _refreshTokenService.TryRefreshToken();
            if (string.IsNullOrEmpty(token))
            {
                e.Cancel = true;
                return;
            }

            e.Request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        }
        public void DisposeEvent() => _interceptor.BeforeSendAsync -= InterceptBeforeHttpAsync;
    }
}
