using System.Collections.Generic;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;

namespace Amendment.Repository.Infrastructure
{
    public interface IReadOnlyRepository<TModel, in TSelector> where TModel : BaseModel
    {
        /// <summary>
        /// Selects a single record using the selector model
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        Task<TModel> SelectSingleAsync(TSelector selector);

        /// <summary>
        /// Selects multiple records using the selector model
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        Task<List<TModel>> SelectManyAsync(TSelector selector);

        /// <summary>
        /// Selects a single record using the primary key
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TModel> SelectSingleAsync(int id);

        /// <summary>
        /// Selects all records
        /// </summary>
        /// <returns></returns>
        Task<List<TModel>> SelectAllAsync();
    }
}