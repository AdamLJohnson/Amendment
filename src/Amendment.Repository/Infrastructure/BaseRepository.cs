using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;
using Dapper;

namespace Amendment.Repository.Infrastructure
{
    public class BaseRepository<TModel, TSelector> : BaseReadOnlyRepository<TModel, TSelector>, IRepository<TModel, TSelector> where TModel : BaseModel
    {
        protected BaseRepository(IDbConnection dbConnection) : base(dbConnection)
        {
        }

        public Task<int> InsertAsync(TModel model)
        {
            // Returns ID of inserted record (Use OUTPUT Inserted.<ObjectID> in stored procedure)
            return _dbConnection.QuerySingleAsync<int>(
                $"{DatabaseObjectNames.Schema}.{_modelName}_{DatabaseObjectNames.Insert}", model,
                commandType: CommandType.StoredProcedure);
        }

        public Task<int> UpdateAsync(TModel model)
        {
            return _dbConnection.ExecuteAsync(
                $"{DatabaseObjectNames.Schema}.{_modelName}_{DatabaseObjectNames.Update}", model,
                commandType: CommandType.StoredProcedure);
        }

        public Task<int> DeleteAsync(TSelector selector)
        {
            return _dbConnection.ExecuteAsync(
                $"{DatabaseObjectNames.Schema}.{_modelName}_{DatabaseObjectNames.Delete}", selector,
                commandType: CommandType.StoredProcedure);
        }
    }
}
