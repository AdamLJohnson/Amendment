using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository;

namespace Amendment.Service
{
    public interface IRoleService
    {
        Task AddUserToRoleAsync(int userId, string roleName);
        Task RemoveUserFromRoleAsync(int userId, string roleName);
        Task<List<Role>> GetRolesForUserAsync(int userId);
        Task<bool> IsInRoleAsync(int userId, string roleName);
        Task<List<Role>> GetAllAsync();
    }

    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _repository;

        public RoleService(IRoleRepository repository)
        {
            _repository = repository;
        }

        public Task AddUserToRoleAsync(int userId, string roleName)
        {
            return _repository.AddUserToRoleAsync(userId, roleName);
        }

        public Task RemoveUserFromRoleAsync(int userId, string roleName)
        {
            return _repository.RemoveUserFromRoleAsync(userId, roleName);
        }

        public async Task<List<Role>> GetRolesForUserAsync(int userId)
        {
            return (await _repository.GetRolesForUserAsync(userId))?.ToList();
        }

        public async Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            var roles = await _repository.GetRolesForUserAsync(userId);
            return roles.Any(r => r.Name == roleName);
        }

        public Task<List<Role>> GetAllAsync()
        {
            return _repository.SelectAllAsync();
        }
    }
}
