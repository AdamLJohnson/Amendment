using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository;
using Amendment.Repository.Infrastructure;
using Amendment.Service.Infrastructure;

namespace Amendment.Service
{
    public interface IRoleService : IReadOnlyDataService<Role>
    {
        Task<Role> GetByNameAsync(string roleName);
    }

    public class RoleService : BaseReadOnlyDataService<Role>, IRoleService
    {
        private readonly IRepository<Role> _repository;

        public RoleService(IRepository<Role> repository) : base(repository)
        {
            _repository = repository;
        }

        public Task<Role> GetByNameAsync(string roleName)
        {
            return _repository.GetAsync(r => r.Name == roleName);
        }
    }
}
