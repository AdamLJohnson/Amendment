using System.Data;
using Amendment.Model.Infrastructure;

namespace Amendment.Repository.Infrastructure
{
    public class GenericRepository<TModel, TSelector> : BaseRepository<TModel, TSelector> where TModel : BaseModel
    {
        public GenericRepository(IDbConnection dbConnection) : base(dbConnection)
        {
        }
    }
}
