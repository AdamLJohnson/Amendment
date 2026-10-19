using Amendment.Client.AuthProviders;
using Amendment.Client.Helpers;
using Amendment.Client.Repository;
using Amendment.Shared.Responses;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Blazorise.FluentValidation;
using FluentValidation;
using Amendment.Client.Repository.Infrastructure;
using Amendment.Client.Services;

namespace Amendment.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            await AddStageConfigAsync(builder);
            var cognitoAuth = CognitoAuth.FromConfiguration(builder.Configuration);

            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }.EnableIntercept(sp));
            builder.Services.AddHttpClientInterceptor();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
            builder.Services.AddAuthorizationCore();
            cognitoAuth.Register(builder.Services);
            builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            builder.Services.AddScoped<IAmendmentRepository, AmendmentRepository>();
            builder.Services.AddScoped<IAmendmentBodyRepository, AmendmentBodyRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
            builder.Services.AddScoped<ILanguageRepository, LanguageRepository>();
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IHubEventService, HubEventService>();
            builder.Services.AddScoped<IAmendmentHubCommandService, AmendmentHubCommandService>();
            builder.Services.AddScoped<IScreenHubCommandService, ScreenHubCommandService>();
            builder.Services.AddScoped<INotificationServiceWrapper, NotificationServiceWrapper>();
            builder.Services.AddScoped<ITimerEventService,  TimerEventService>();
            builder.Services.AddScoped<ITimerHubCommandService, TimerHubCommandService>();
            builder.Services.AddScoped<ITimerControlHubCommandService, TimerControlHubCommandService>();
            builder.Services.AddScoped<IClientErrorService, ClientErrorService>();
            builder.Services.AddScoped<IVersionCheckService, VersionCheckService>(); 

            builder.Services
                .AddBlazorise(options =>
                {
                    options.Immediate = true;
                })
                .AddBootstrap5Providers()
                .AddFontAwesomeIcons()
                .AddBlazoriseFluentValidation();

            builder.Services.AddValidatorsFromAssembly(typeof(UserResponse).Assembly);

            await builder.Build().RunAsync();
        }

        /// <summary>
        /// Adds the per-stage <c>amendment-config.json</c> written by <c>publish-web.ps1</c>, if present. Hosts that
        /// fall back to <c>index.html</c> for unknown paths return HTML instead, which is ignored.
        /// </summary>
        private static async Task AddStageConfigAsync(WebAssemblyHostBuilder builder)
        {
            try
            {
                using var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
                using var response = await http.GetAsync("amendment-config.json");
                if (response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/json")
                    builder.Configuration.AddJsonStream(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            }
            catch (HttpRequestException)
            {
            }
        }
    }
}