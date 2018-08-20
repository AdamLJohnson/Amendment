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

        public virtual async Task<IOperationResult> CreateAsync(T item, int userId)
        {
            _repository.Add(item);
            await _unitOfWork.SaveChangesAsync(userId);
            return new OperationResult(true);
        }

        public virtual async Task<IOperationResult> DeleteAsync(T item, int userId)
        {
            _repository.Delete(item);
            await _unitOfWork.SaveChangesAsync(userId);
            return new OperationResult(true);
        }

        public virtual async Task<IOperationResult> UpdateAsync(T item, int userId)
        {
            _repository.Update(item);
            await _unitOfWork.SaveChangesAsync(userId);
            return new OperationResult(true);
        }
    }
}
