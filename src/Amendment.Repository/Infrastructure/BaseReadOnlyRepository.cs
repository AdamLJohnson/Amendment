using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;
using Dapper;

namespace Amendment.Repository.Infrastructure
{
    public abstract class BaseReadOnlyRepository<TModel, TSelector> : IReadOnlyRepository<TModel, TSelector> where TModel : BaseModel
    {
        protected readonly IDbConnection _dbConnection;
        protected readonly string _modelName;

        protected BaseReadOnlyRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _modelName = nameof(TModel);

        }

        public Task<TModel> SelectSingleAsync(int id)
        {
            return _dbConnection.QuerySingleAsync<TModel>(
                $"{DatabaseObjectNames.Schema}.{_modelName}_{DatabaseObjectNames.SelectSingleById}", new { id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<TModel>> SelectAllAsync()
        {
            return (await _dbConnection.QueryAsync<TModel>(
                $"{DatabaseObjectNames.Schema}.{_modelName}_{DatabaseObjectNames.SelectAll}",
                commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task<List<TModel>> SelectManyAsync(TSelector selector)
        {
            return (await _dbConnection.QueryAsync<TModel>(
                $"{DatabaseObjectNames.Schema}.{_modelName}_{DatabaseObjectNames.SelectMany}", selector,
                commandType: CommandType.StoredProcedure)).ToList();
        }

        public Task<TModel> SelectSingleAsync(TSelector selector)
        {
            return _dbConnection.QuerySingleAsync<TModel>(
                $"{DatabaseObjectNames.Schema}.{_modelName}_{DatabaseObjectNames.SelectSingle}", selector,
                commandType: CommandType.StoredProcedure);
        }
    }
}
