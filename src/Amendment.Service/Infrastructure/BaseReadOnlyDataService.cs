using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;
using Amendment.Repository.Infrastructure;

namespace Amendment.Service.Infrastructure
{
    public abstract class BaseReadOnlyDataService<T> : IReadOnlyDataService<T> where T : class, IReadOnlyTable
    {
        private readonly IReadOnlyRepository<T> _repository;

        protected BaseReadOnlyDataService(IReadOnlyRepository<T> repository)
        {
            _repository = repository;
        }

        public virtual async Task<List<T>> GetAllAsync()
        {
            return (await _repository.GetAllAsync()).ToList();
        }

        public virtual Task<T> GetAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }
    }
}