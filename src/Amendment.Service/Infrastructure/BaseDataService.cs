using System;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;
using Amendment.Repository.Infrastructure;

namespace Amendment.Service.Infrastructure
{
    public abstract class BaseDataService<T> : BaseReadOnlyDataService<T>, IDataService<T> where T : class, ITableBase
    {
        private readonly IRepository<T> _repository;
        private readonly IUnitOfWork _unitOfWork;

        protected BaseDataService(IRepository<T> repository, IUnitOfWork unitOfWork) : base(repository)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public virtual Task CreateAsync(T item)
        {
            _repository.Add(item);
            return _unitOfWork.SaveChangesAsync();
        }

        public virtual Task DeleteAsync(T item)
        {
            _repository.Delete(item);
            return _unitOfWork.SaveChangesAsync();
        }

        public virtual Task UpdateAsync(T item)
        {
            _repository.Update(item);
            return _unitOfWork.SaveChangesAsync();
        }
    }
}
