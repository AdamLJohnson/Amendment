using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Client.Repository.Infrastructure
{
    public interface IHttpRepository<TRequest, TResponse> where TRequest : class, new() where TResponse : class, new()
    {
        Task<IEnumerable<TResponse>> GetAsync();
        Task<TResponse> GetAsync(int id);
        Task<TResponse> PostAsync(TRequest request);
        Task<TResponse> PutAsync(int id, TRequest request);
        Task DeleteAsync(int id);
    }
}
