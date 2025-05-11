using Amendment.Client;
using Amendment.Client.AuthProviders;
using Amendment.Client.Helpers;
using Amendment.Client.Repository;
using Amendment.Shared.Responses;
using Blazored.LocalStorage;
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

            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }.EnableIntercept(sp));
            builder.Services.AddHttpClientInterceptor();
            builder.Services.AddScoped<HttpInterceptorService>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
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

            builder.Services
                .AddBlazorise(options =>
                {
                    options.Immediate = true;
                })
                .AddBootstrap5Providers()
                .AddFontAwesomeIcons()
                .AddBlazoriseFluentValidation();

            builder.Services.AddValidatorsFromAssembly(typeof(UserResponse).Assembly);

            var app = builder.Build();

            var interceptorService = app.Services.GetRequiredService<HttpInterceptorService>();
            interceptorService.RegisterEvent();

            await app.RunAsync();
        }
    }
}