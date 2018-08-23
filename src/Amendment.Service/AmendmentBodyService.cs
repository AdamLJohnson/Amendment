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
        private readonly IClientNotifier _clientNotifier;
        private readonly IScreenControlService _screenControlService;

        public AmendmentBodyService(IAmendmentBodyRepository repository, IUnitOfWork unitOfWork, IClientNotifier clientNotifier, IScreenControlService screenControlService) : base(repository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _clientNotifier = clientNotifier;
            _screenControlService = screenControlService;
        }

        public override async Task<IOperationResult> CreateAsync(Model.DataModel.AmendmentBody item, int userId)
        {
            var results = await base.CreateAsync(item, userId);
            await _clientNotifier.SendAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = item.Id, results, data = item });
            return results;
        }

        public override async Task<IOperationResult> UpdateAsync(Model.DataModel.AmendmentBody item, int userId)
        {
            var results = await base.UpdateAsync(item, userId);
            await _clientNotifier.SendAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = item.Id, results, data = item });
            await _screenControlService.UpdateBodyAsync(item);
            return results;
        }

        public override async Task<IOperationResult> DeleteAsync(Model.DataModel.AmendmentBody item, int userId)
        {
            var results = await base.DeleteAsync(item, userId);
            await _clientNotifier.SendAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = item.Id, results, data = item });
            return results;
        }

        public async Task<List<AmendmentBody>> GetByAmentmentId(int amendmentId)
        {
            var list = (await _repository.GetManyAsync(where: q => q.AmendId == amendmentId)).Results;
            return list;
        }
    }
}
