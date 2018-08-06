using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;

namespace Amendment.Repository.Infrastructure
{
    public interface IRepository<T> : IReadOnlyRepository<T> where T: class, ITableBase
    {
        // Marks an entity as new
        void Add(T entity);
        // Marks an entity as modified
        void Update(T entity);
        // Marks an entity to be removed
        void Delete(T entity);
        void Delete(params Expression<Func<T, bool>>[] where);
    }
}
