using System;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;

namespace Amendment.Repository.Infrastructure
{
    public interface IRepository<TModel, in TSelector> : IReadOnlyRepository<TModel, TSelector> where TModel: BaseModel
    {
        /// <summary>
        /// Inserts a record into the data store
        /// </summary>
        /// <param name="model">The record to insert</param>
        /// <returns>The newly inserted row</returns>
        Task<int> InsertAsync(TModel model);

        /// <summary>
        /// Update a record in the data store
        /// </summary>
        /// <param name="model"></param>
        /// <returns>The number of rows updated</returns>
        Task<int> UpdateAsync(TModel model);

        /// <summary>
        /// Deletes a record in the data store
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        Task<int> DeleteAsync(TSelector selector);
    }
}
