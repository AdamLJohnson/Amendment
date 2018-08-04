using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Model.Selectors;
using Amendment.Repository.Infrastructure;
using Dapper;

namespace Amendment.Repository
{
    public interface IRoleRepository : IReadOnlyRepository<Role, RoleSelector>
    {
        Task AddUserToRoleAsync(int userId, string roleName);
        Task RemoveUserFromRoleAsync(int userId, string roleName);
        Task<IEnumerable<Role>> GetRolesForUserAsync(int userId);
    }

    public class RoleRepository : BaseReadOnlyRepository<Role, RoleSelector>, IRoleRepository
    {
        public RoleRepository(IDbConnection dbConnection) : base(dbConnection)
        {
        }

        public Task AddUserToRoleAsync(int userId, string roleName)
        {
            return _dbConnection.ExecuteAsync(
                $"{DatabaseObjectNames.Schema}.{_modelName}_AddUserToRole", new { userId, roleName },
                commandType: CommandType.StoredProcedure);
        }

        public Task RemoveUserFromRoleAsync(int userId, string roleName)
        {
            return _dbConnection.ExecuteAsync(
                $"{DatabaseObjectNames.Schema}.{_modelName}_RemoveUserToRole", new { userId, roleName },
                commandType: CommandType.StoredProcedure);
        }

        public Task<IEnumerable<Role>> GetRolesForUserAsync(int userId)
        {
            return _dbConnection.QueryAsync<Role>(
                $"{DatabaseObjectNames.Schema}.{_modelName}_GetRolesForUser", new { userId },
                commandType: CommandType.StoredProcedure);
        }
    }
}
