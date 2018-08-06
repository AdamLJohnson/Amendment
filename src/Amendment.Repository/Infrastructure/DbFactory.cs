using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Amendment.Repository.Infrastructure
{
    public class DbFactory : Disposable, IDbFactory
    {
        private readonly DbContextOptions<AmendmentContext> _options;
        AmendmentContext dbContext;

        public DbFactory(DbContextOptions<AmendmentContext> options)
        {
            _options = options;
        }

        public AmendmentContext Init()
        {
            return dbContext ?? (dbContext = new AmendmentContext(_options));
        }

        //protected override void DisposeCore()
        //{
        //    if (dbContext != null)
        //        dbContext.Dispose();
        //}
    }
}
