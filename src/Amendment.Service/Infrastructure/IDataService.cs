using System.Threading.Tasks;
using Amendment.Model.Infrastructure;

namespace Amendment.Service.Infrastructure
{
    public interface IDataService<T> : IReadOnlyDataService<T> where T : ITableBase
    {
        Task CreateAsync(T item, int userId);
        Task UpdateAsync(T item, int userId);
        Task DeleteAsync(T item, int userId);
    }
}