using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository;
using Amendment.Repository.Infrastructure;
using Amendment.Service.Infrastructure;

namespace Amendment.Service
{
    public interface IAmendmentBodyService : IDataService<AmendmentBody>
    {
        Task<List<AmendmentBody>> GetByAmentmentId(int amendmentId);
    }

    public class AmendmentBodyService : BaseDataService<AmendmentBody>, IAmendmentBodyService
    {
        private readonly IAmendmentBodyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public AmendmentBodyService(IAmendmentBodyRepository repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<AmendmentBody>> GetByAmentmentId(int amendmentId)
        {
            var list = (await _repository.GetManyAsync(where: q => q.AmendId == amendmentId)).Results;
            return list;
        }
    }
}
