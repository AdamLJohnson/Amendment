using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.Infrastructure;
using Amendment.Repository.Infrastructure;

namespace Amendment.Service.Infrastructure
{
    public sealed class GenericReadOnlyDataService<T> : BaseReadOnlyDataService<T> where T : class, ITableBase
    {
        public GenericReadOnlyDataService(IReadOnlyRepository<T> repository) : base(repository)
        {
        }
    }
}
