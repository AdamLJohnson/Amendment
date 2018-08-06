using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Amendment.Repository.Infrastructure
{
    public abstract class BaseRepository<T> : BaseReadOnlyRepository<T>, IRepository<T> where T : class, ITableBase
    {
        protected BaseRepository(IDbFactory dbFactory) : base(dbFactory)
        {
        }

        public virtual void Add(T entity)
        {
            DbSet.AddAsync(entity);
        }

        public virtual void Update(T entity)
        {
            DbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public virtual void Delete(T entity)
        {
            _context.Remove(entity);
        }

        public virtual void Delete(params Expression<Func<T, bool>>[] where)
        {
            IEnumerable<T> objects = DbSet.WhereMany<T>(where).AsEnumerable();
            foreach (T obj in objects)
                Delete(obj);
        }
    }
}
