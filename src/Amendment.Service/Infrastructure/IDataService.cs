using System.Threading.Tasks;
using Amendment.Model.Infrastructure;

namespace Amendment.Service.Infrastructure
{
    public interface IDataService<T> : IReadOnlyDataService<T> where T : ITableBase
    {
        Task CreateAsync(T item);
        Task UpdateAsync(T item);
        Task DeleteAsync(T item);
    }
}