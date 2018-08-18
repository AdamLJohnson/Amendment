using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;


namespace Amendment.Repository.Infrastructure
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AmendmentContext>
    {
        public AmendmentContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                //.AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<AmendmentContext>();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            builder.UseMySql(connectionString);

            return new AmendmentContext(builder.Options);
        }
    }
}
