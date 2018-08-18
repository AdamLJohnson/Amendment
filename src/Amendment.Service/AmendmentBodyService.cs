using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Repository.Infrastructure;
using Amendment.Service.Infrastructure;

namespace Amendment.Service
{
    public interface IAmendmentBodyService : IDataService<AmendmentBody>
    {
    }

    public class AmendmentBodyService : BaseDataService<AmendmentBody>, IAmendmentBodyService
    {
        private readonly IRepository<AmendmentBody> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public AmendmentBodyService(IRepository<AmendmentBody> repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }
    }
}
