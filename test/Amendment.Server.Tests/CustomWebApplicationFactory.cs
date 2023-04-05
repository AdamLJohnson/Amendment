using Amendment.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Amendment.Server.Tests
{
    public class CustomWebApplicationFactory<TProgram>
        : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType ==
                        typeof(DbContextOptions<AmendmentContext>));

                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AmendmentContext>(options =>
                {
                    options.UseInMemoryDatabase($"dbName");
                });

                var basicAuth = services.SingleOrDefault(
                    s => s.ServiceType ==
                         typeof(JwtBearerHandler));

                if (basicAuth != null) services.Remove(basicAuth);

                services.AddTransient<IAuthenticationSchemeProvider, MockSchemeProvider>();
            });

            builder.UseEnvironment("Development");
        }
    }

    public class MockSchemeProvider : AuthenticationSchemeProvider
    {
        public MockSchemeProvider(IOptions<AuthenticationOptions> options)
            : base(options)
        {
        }

        protected MockSchemeProvider(
            IOptions<AuthenticationOptions> options,
            IDictionary<string, AuthenticationScheme> schemes
        )
            : base(options, schemes)
        {
        }

        public override Task<AuthenticationScheme?> GetSchemeAsync(string name)
        {
            if (name == "Bearer")
            {
                var scheme = new AuthenticationScheme(
                    "Bearer",
                    "Bearer",
                    typeof(MockAuthenticationHandler)
                );
                return Task.FromResult(scheme)!;
            }

            return base.GetSchemeAsync(name);
        }
    }

    public class MockAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public MockAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock
        )
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new (ClaimTypes.Name, "admin"),
                new ("id", 1.ToString()),
                new (ClaimTypes.Role, "System Administrator")
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Bearer");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
