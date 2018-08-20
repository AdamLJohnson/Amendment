using System.Threading.Tasks;
using Amendment.Model.Infrastructure;

namespace Amendment.Service.Infrastructure
{
    public interface IDataService<T> : IReadOnlyDataService<T> where T : ITableBase
    {
        Task<IOperationResult> CreateAsync(T item, int userId);
        Task<IOperationResult> UpdateAsync(T item, int userId);
        Task<IOperationResult> DeleteAsync(T item, int userId);
    }
}