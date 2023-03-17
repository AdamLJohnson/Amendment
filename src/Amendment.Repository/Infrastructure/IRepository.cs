using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

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

        Task<int> ExecuteUpdate(Expression<Func<T, bool>> where,
            Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls);

        Task<int> ExecuteDelete(Expression<Func<T, bool>> where);
    }
}
