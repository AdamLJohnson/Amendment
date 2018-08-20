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

        public virtual Task CreateAsync(T item, int userId)
        {
            _repository.Add(item);
            return _unitOfWork.SaveChangesAsync(userId);
        }

        public virtual Task DeleteAsync(T item, int userId)
        {
            _repository.Delete(item);
            return _unitOfWork.SaveChangesAsync(userId);
        }

        public virtual Task UpdateAsync(T item, int userId)
        {
            _repository.Update(item);
            return _unitOfWork.SaveChangesAsync(userId);
        }
    }
}
