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
        Task<Model.DataModel.Amendment> GetLiveAsync();
    }

    public class AmendmentService : BaseDataService<Model.DataModel.Amendment>, IAmendmentService
    {
        private readonly IRepository<Model.DataModel.Amendment> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClientNotifier _clientNotifier;
        private readonly IScreenControlService _screenControlService;
        private readonly IUserService _userService;

        public AmendmentService(IAmendmentRepository repository, IUnitOfWork unitOfWork, IClientNotifier clientNotifier, IScreenControlService screenControlService
            , IUserService userService) : base(repository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _clientNotifier = clientNotifier;
            _screenControlService = screenControlService;
            _userService = userService;
        }

        public override async Task<IOperationResult> CreateAsync(Model.DataModel.Amendment item, int userId)
        {
            var results = await base.CreateAsync(item, userId);
            var user = await _userService.GetForToastAsync(userId);
            item = await GetAsync(item.Id);
            await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentChange, new { id = item.Id, results, data = item, user });
            return results;
        }

        public override async Task<IOperationResult> UpdateAsync(Model.DataModel.Amendment item, int userId)
        {
            var results = await base.UpdateAsync(item, userId);
            var user = await _userService.GetForToastAsync(userId);
            item = await GetAsync(item.Id);
            await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentChange, new { id = item.Id, results, data = item, user });
            await _screenControlService.UpdateAmendmentAsync(item);
            return results;
        }

        public override async Task<IOperationResult> DeleteAsync(Model.DataModel.Amendment item, int userId)
        {
            var results = await base.DeleteAsync(item, userId);
            var user = await _userService.GetForToastAsync(userId);
            await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentChange, new { id = item.Id, results, data = item, user });
            return results;
        }

        public Task<Model.DataModel.Amendment> GetLiveAsync()
        {
            return _repository.GetAsync(a => a.IsLive);
        }
    }
}
