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

                services.Remove(descriptor);

                services.AddDbContext<AmendmentContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
            });

            builder.UseEnvironment("Development");
        }
    }
}
