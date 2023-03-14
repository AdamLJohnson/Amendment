using System.Net.Http.Headers;
using Toolbelt.Blazor;

namespace Amendment.Client.AuthProviders
{
    public class HttpInterceptorService
    {
        private readonly HttpClientInterceptor _interceptor;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly List<string> _anonymousUrls = new ()
        {
            "api/account/login",
            "api/account/refreshtoken",
            "api/language",
            "api/Amendment/Live"
        };

        public HttpInterceptorService(HttpClientInterceptor interceptor, IRefreshTokenService refreshTokenService)
        {
            _interceptor = interceptor;
            _refreshTokenService = refreshTokenService;
        }

        public void RegisterEvent() => _interceptor.BeforeSendAsync += InterceptBeforeHttpAsync;
        public async Task InterceptBeforeHttpAsync(object sender, HttpClientInterceptorEventArgs e)
        {
            var absPath = e.Request.RequestUri.AbsolutePath;
            if (_anonymousUrls.Any(z => absPath.Contains(z, StringComparison.CurrentCultureIgnoreCase)))
            {
                e.Request.Headers.Authorization = null;
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
