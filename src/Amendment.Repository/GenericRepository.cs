using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Amendment.Model.Infrastructure;
using Amendment.Repository.Infrastructure;

namespace Amendment.Repository
{
    public class GenericRepository<TModel, TSelector> : BaseRepository<TModel, TSelector> where TModel : BaseModel
    {
        public GenericRepository(IDbConnection dbConnection) : base(dbConnection)
        {
        }
    }
}
