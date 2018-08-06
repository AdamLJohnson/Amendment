using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Amendment.Repository.Infrastructure
{
    public abstract class BaseReadOnlyRepository<T> where T : class, IReadOnlyTable
    {
        protected AmendmentContext _context;
        protected DbSet<T> DbSet;
        protected IQueryable<T> Query;

        protected IDbFactory DbFactory
        {
            get;
            private set;
        }

        private AmendmentContext DbContext => _context ?? (_context = DbFactory.Init());

        protected BaseReadOnlyRepository(IDbFactory dbFactory)
        {
            DbFactory = dbFactory;
            DbSet = DbContext.Set<T>();
            Query = DbSet;
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            var results = await Query.SingleOrDefaultAsync(e => e.Id == id);
            if (results == null)
                return null;

            return ChildRecordSelector(results);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await DbSet.ToListAsync();
        }

        public virtual Task<ListResults<T>> GetManyAsync(string orderBy = "", int pageNumber = 1, int pageSize = int.MaxValue, params Expression<Func<T, bool>>[] @where)
        {
            return GetManWithIncludeAsync(Query, orderBy, pageNumber, pageSize, @where);
        }

        protected async Task<ListResults<T>> GetManWithIncludeAsync(IQueryable<T> query, string orderBy = "", int pageNumber = 1, int pageSize = int.MaxValue, params Expression<Func<T, bool>>[] @where)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var output = new ListResults<T>
            {
                TotalCount = await CountAsync()
            };

            if (where != null)
                query = query.WhereMany(where);

            output.FilteredCount = await CountAsync(query);

            if (!string.IsNullOrEmpty(orderBy))
                query = query.OrderByJsonApi(orderBy);

            if (pageNumber < 1)
                pageNumber = 1;

            output.PageNumber = pageNumber;
            pageNumber = pageNumber - 1;

            query = query.Skip(pageNumber * pageSize);
            query = query.Take(pageSize);

            output.Results = await query.ToListAsync();

            if (output.FilteredCount == 0)
                return output;

            output.Results = output.Results.Select(ChildRecordSelector).ToList();

            return output;
        }

        public virtual Task<T> GetAsync(params Expression<Func<T, bool>>[] where)
        {
            return GetAsync(Query, where);
        }

        protected virtual async Task<T> GetAsync(IQueryable<T> query, params Expression<Func<T, bool>>[] where)
        {
            var result = await query.WhereMany(where).FirstOrDefaultAsync<T>();
            if (result == null)
                return null;
            return ChildRecordSelector(result);
        }

        public virtual async Task<int> CountAsync()
        {
            return await DbSet.CountAsync();
        }

        public virtual async Task<int> CountAsync(params Expression<Func<T, bool>>[] where)
        {
            return await DbSet.WhereMany(where).CountAsync();
        }

        private async Task<int> CountAsync(IQueryable<T> query)
        {
            return await query.CountAsync();
        }

        protected virtual T ChildRecordSelector(T s)
        {
            return s;
        }
    }
}
