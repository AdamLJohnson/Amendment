using System;
using Amendment.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amendment.Web
{
    public class StartupDevelopment : StartupBase
    {
        public StartupDevelopment(IHostingEnvironment env) : base(env)
        {
        }

        protected override DbContextOptions<AmendmentContext> ConfigureContextOptions(IServiceProvider serviceProvider)
        {
            // Create a fresh service provider, and therefore a fresh 
            // InMemory database instance.
            var sp = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Create a new options instance telling the context to use an
            // InMemory database and the new service provider.
            var builder = new DbContextOptionsBuilder<AmendmentContext>();
            builder.UseInMemoryDatabase("Amendment")
                .UseInternalServiceProvider(sp)
                .ConfigureWarnings(b =>
                {
                    b.Ignore(InMemoryEventId.TransactionIgnoredWarning);
                });

            return builder.Options;
        }

        protected override void SetupEnvSpecificServices(IServiceCollection services)
        {
        }
    }
}