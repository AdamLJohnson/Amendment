using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Amendment.Web.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Amendment.Web
{
    public class Startup : StartupBase
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }

        protected override DbContextOptions<AmendmentContext> ConfigureContextOptions(IServiceProvider serviceProvider)
        {
            return new DbContextOptionsBuilder<AmendmentContext>()
                .UseMySQL(Configuration.GetConnectionString("DefaultConnection"))
                .Options;
        }

        protected override void SetupEnvSpecificServices(IServiceCollection services)
        {
        }
    }
}
