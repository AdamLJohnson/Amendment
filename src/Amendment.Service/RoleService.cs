using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository;
using Amendment.Repository.Infrastructure;

namespace Amendment.Service
{
    public interface IRoleService
    {
        Task<Role> GetByNameAsync(string roleName);
    }

    public class RoleService : IRoleService
    {
        private readonly IRepository<Role> _repository;

        public RoleService(IRepository<Role> repository)
        {
            _repository = repository;
        }

        public Task<Role> GetByNameAsync(string roleName)
        {
            return _repository.GetAsync(r => r.Name == roleName);
        }
    }
}
