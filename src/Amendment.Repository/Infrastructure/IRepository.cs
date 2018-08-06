using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;

namespace Amendment.Repository.Infrastructure
{
    public interface IRepository<T> where T: class, ITableBase
    {
        // Marks an entity as new
        void Add(T entity);
        // Marks an entity as modified
        void Update(T entity);
        // Marks an entity to be removed
        void Delete(T entity);
        void Delete(params Expression<Func<T, bool>>[] where);
        // Get an entity by int id
        Task<T> GetByIdAsync(int id);
        // Get an entity using delegate
        Task<T> GetAsync(params Expression<Func<T, bool>>[] where);
        // Gets all entities of type T
        Task<IEnumerable<T>> GetAllAsync();
        // Gets entities using delegate
        Task<ListResults<T>> GetManyAsync(string orderBy, int pageNumber, int pageSize, params Expression<Func<T, bool>>[] where);
        Task<int> CountAsync(params Expression<Func<T, bool>>[] where);
        Task<int> CountAsync();
    }
}
