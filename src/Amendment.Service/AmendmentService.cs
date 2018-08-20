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
    public interface IAmendmentService : IDataService<Model.DataModel.Amendment>
    {
    }

    public class AmendmentService : BaseDataService<Model.DataModel.Amendment>, IAmendmentService
    {
        private readonly IRepository<Model.DataModel.Amendment> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClientNotifier _clientNotifier;

        public AmendmentService(IAmendmentRepository repository, IUnitOfWork unitOfWork, IClientNotifier clientNotifier) : base(repository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _clientNotifier = clientNotifier;
        }

        public override async Task<IOperationResult> CreateAsync(Model.DataModel.Amendment item, int userId)
        {
            var results = await base.CreateAsync(item, userId);
            await _clientNotifier.SendAsync(DestinationHub.Amendment, "AmendmentChange", new { id = item.Id, results, data = item });
            return results;
        }

        public override async Task<IOperationResult> UpdateAsync(Model.DataModel.Amendment item, int userId)
        {
            var results = await base.UpdateAsync(item, userId);
            await _clientNotifier.SendAsync(DestinationHub.Amendment, "AmendmentChange", new { id = item.Id, results, data = item });
            return results;
        }

        public override async Task<IOperationResult> DeleteAsync(Model.DataModel.Amendment item, int userId)
        {
            var results = await base.DeleteAsync(item, userId);
            await _clientNotifier.SendAsync(DestinationHub.Amendment, "AmendmentChange", new { id = item.Id, results, data = item });
            return results;
        }
    }
}
