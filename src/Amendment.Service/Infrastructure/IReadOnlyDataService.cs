using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.Infrastructure;

namespace Amendment.Service.Infrastructure
{
    public interface IReadOnlyDataService<T> where T : IReadOnlyTable
    {
        Task<T> GetAsync(int id);
        Task<List<T>> GetAllAsync();
    }
}
