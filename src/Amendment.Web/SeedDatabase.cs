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
                var roleRepository = _serviceProvider.GetRequiredService<IRepository<Role>>();

                await seedRoles(roleRepository);
                await unitOfWork.SaveChangesAsync();

                await SeedDefaultUsers(passwordHashService, userRepository, roleRepository);
                await unitOfWork.SaveChangesAsync();
            }
        }

        private async Task seedRoles(IRepository<Role> roleRepository)
        {
            if (await roleRepository.CountAsync(r => r.Name == "Administrator") == 0)
            {
                roleRepository.Add(new Role()
                {
                    Name = "Administrator",
                    EnteredBy = -1,
                    EnteredDate = DateTime.UtcNow,
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.UtcNow,
                });
            }
        }

        private async Task SeedDefaultUsers(IPasswordHashService passwordHashService, IRepository<User> userRepository, IRepository<Role> roleRepository)
        {
            if (await userRepository.CountAsync(u => u.Username == "adamlj") == 0)
            {
                User adamljUser = new User
                {
                    Username = "adamlj",
                    Email = "columbus@adamlj.com",
                    Name = "Adam",
                    Password = passwordHashService.HashPassword("adamlj"),
                    EnteredBy = -1,
                    EnteredDate = DateTime.UtcNow,
                    LastUpdatedBy = -1,
                    LastUpdated = DateTime.UtcNow,
                    UserXRoles = new List<UserXRole>()
                };
                adamljUser.UserXRoles.Add(new UserXRole()
                {
                    User = adamljUser,
                    Role = await roleRepository.GetAsync(r => r.Name == "Administrator")
                });
                userRepository.Add(adamljUser);
            }
        }
    }
}
