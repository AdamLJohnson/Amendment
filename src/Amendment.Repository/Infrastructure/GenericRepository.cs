using System.Data;
using Amendment.Model.Infrastructure;

namespace Amendment.Repository.Infrastructure
{
    public class GenericRepository<T> : BaseRepository<T> where T : class, ITableBase
    {
        public GenericRepository(IDbFactory dbFactory) : base(dbFactory)
        {
        }
    }
}
