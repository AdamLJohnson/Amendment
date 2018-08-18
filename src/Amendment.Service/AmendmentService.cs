using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Repository.Infrastructure;
using Amendment.Service.Infrastructure;

namespace Amendment.Service
{
    public interface IAmendmentService : IDataService<Model.DataModel.Amendment>
    {
    }

    public class AmendmentService : BaseDataService<Model.DataModel.Amendment>, IAmendmentService
    {
        private readonly IRepository<Model.DataModel.Amendment> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public AmendmentService(IRepository<Model.DataModel.Amendment> repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
    }
}
