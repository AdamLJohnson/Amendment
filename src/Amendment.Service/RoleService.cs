using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;

namespace Amendment.Service
{
    public interface IRoleService
    {
        Task AddUserToRoleAsync(int userId, string roleName);
        Task RemoveUserFromRoleAsync(int userId, string roleName);
        Task<List<Role>> GetRolesForUserAsync(int userId);
        bool IsInRoleAsync(int userId, string roleName);
        Task<List<Role>> GetAllAsync();
    }

    public class RoleService : IRoleService
    {
        public Task AddUserToRoleAsync(int userId, string roleName)
        {
            throw new NotImplementedException();
        }

        public Task RemoveUserFromRoleAsync(int userId, string roleName)
        {
            throw new NotImplementedException();
        }

        public Task<List<Role>> GetRolesForUserAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public bool IsInRoleAsync(int userId, string roleName)
        {
            throw new NotImplementedException();
        }

        public Task<List<Role>> GetAllAsync()
        {
            throw new NotImplementedException();
        }
    }
}
