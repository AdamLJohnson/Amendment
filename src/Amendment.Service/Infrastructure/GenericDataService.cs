using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.Infrastructure;
using Amendment.Repository.Infrastructure;

namespace Amendment.Service.Infrastructure
{
    public sealed class GenericDataService<T> : BaseDataService<T> where T : class, ITableBase
    {
        public GenericDataService(IRepository<T> repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
        }
    }
}
