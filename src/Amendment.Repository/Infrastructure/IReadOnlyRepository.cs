using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;

namespace Amendment.Repository.Infrastructure
{
    public interface IReadOnlyRepository<T> where T : class, IReadOnlyTable
    {
        // Get an entity by int id
        Task<T> GetByIdAsync(int id);
        // Get an entity using delegate
        Task<T> GetAsync(params Expression<Func<T, bool>>[] where);
        // Gets all entities of type T
        Task<IEnumerable<T>> GetAllAsync();
        // Gets entities using delegate
        Task<ListResults<T>> GetManyAsync(string orderBy = "", int pageNumber = 1, int pageSize = int.MaxValue, params Expression<Func<T, bool>>[] @where);
        Task<int> CountAsync(params Expression<Func<T, bool>>[] where);
        Task<int> CountAsync();
    }
}
