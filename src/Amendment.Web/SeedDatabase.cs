using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository.Infrastructure;
using Amendment.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Amendment.Web
{
    public class SeedDatabase
    {
        private readonly IServiceProvider _serviceProvider;

        public SeedDatabase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Seed()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
                var passwordHashService = _serviceProvider.GetRequiredService<IPasswordHashService>();
                var userRepository = _serviceProvider.GetRequiredService<IRepository<User>>();
                await SeedDefaultUsers(passwordHashService, userRepository);

                await unitOfWork.SaveChangesAsync();
            }
        }

        private async Task SeedDefaultUsers(IPasswordHashService passwordHashService, IRepository<User> userRepository)
        {
            if (await userRepository.CountAsync(u => u.Username == "adamlj") == 0)
            {
                userRepository.Add(new User
                {
                    Username = "adamlj",
                    Email = "columbus@adamlj.com",
                    Password = passwordHashService.HashPassword("adamlj"),
                    EnteredBy = -1,
                    EnteredDate = DateTime.UtcNow,
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.UtcNow
                });
            }
        }
    }
}
