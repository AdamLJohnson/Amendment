using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Model.Infrastructure;
using Amendment.Repository;
using Amendment.Repository.Infrastructure;
using Amendment.Service.Infrastructure;

namespace Amendment.Service
{
    public interface IAmendmentBodyService : IDataService<AmendmentBody>
    {
        Task<List<AmendmentBody>> GetByAmentmentId(int amendmentId);
        Task<bool> ValidateLanguageId(AmendmentBody body);
        Task<IEnumerable<AmendmentBody>> SetIsLiveForManyAsync(SetIsLive[] data);
        Task<IEnumerable<AmendmentBody>> SetPageForManyAsync(SetPage[] data);
    }

    public class AmendmentBodyService : BaseDataService<AmendmentBody>, IAmendmentBodyService
    {
        private readonly IAmendmentBodyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClientNotifier _clientNotifier;
        private readonly IScreenControlService _screenControlService;
        private readonly IAmendmentRepository _amendmentRepository;
        private readonly IUserService _userService;

        public AmendmentBodyService(IAmendmentBodyRepository repository, IUnitOfWork unitOfWork, IClientNotifier clientNotifier, IScreenControlService screenControlService
            , IAmendmentRepository amendmentRepository, IUserService userService) : base(repository, unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _clientNotifier = clientNotifier;
            _screenControlService = screenControlService;
            _amendmentRepository = amendmentRepository;
            _userService = userService;
        }

        public override async Task<IOperationResult> CreateAsync(Model.DataModel.AmendmentBody item, int userId)
        {
            var results = await base.CreateAsync(item, userId);
            var amendment = await _amendmentRepository.GetByIdAsync(item.AmendId);
            var user = await _userService.GetForToastAsync(userId);

            item = await GetAsync(item.Id);
            await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = item.Id, results, data = item, amendment, user });
            return results;
        }

        public override async Task<IOperationResult> UpdateAsync(Model.DataModel.AmendmentBody item, int userId)
        {
            var results = await base.UpdateAsync(item, userId);
            var amendment = await _amendmentRepository.GetByIdAsync(item.AmendId);
            var user = await _userService.GetForToastAsync(userId);

            item = await GetAsync(item.Id);
            await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = item.Id, results, data = item, amendment, user });
            await _screenControlService.UpdateBodyAsync(item, false);
            return results;
        }

        public override async Task<IOperationResult> DeleteAsync(Model.DataModel.AmendmentBody item, int userId)
        {
            var results = await base.DeleteAsync(item, userId);
            var amendment = await _amendmentRepository.GetByIdAsync(item.AmendId);
            var user = await _userService.GetForToastAsync(userId);

            await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = item.Id, results, data = item, amendment, user });
            return results;
        }

        public async Task<List<AmendmentBody>> GetByAmentmentId(int amendmentId)
        {
            var list = (await _repository.GetManyAsync(where: q => q.AmendId == amendmentId)).Results;
            return list;
        }

        public async Task<bool> ValidateLanguageId(AmendmentBody body)
        {
            var result = (await _repository.GetManyAsync(where: q =>
                q.AmendId == body.AmendId && q.LanguageId == body.LanguageId && q.Id != body.Id)).FilteredCount == 0;

            return result;
        }

        public async Task<IEnumerable<AmendmentBody>> SetIsLiveForManyAsync(SetIsLive[] data)
        {
            var ids = data.Select(x => x.Id).ToArray();
            if (!_unitOfWork.InMemory)
            {
                foreach (var item in data)
                {
                    await _repository.ExecuteUpdate(
                        x => x.Id == item.Id,
                        x => x.SetProperty(p => p.IsLive, p => item.IsLive));
                }
            }
            else
            {
                foreach (var item in data)
                {
                    var body = await _repository.GetByIdAsync(item.Id);
                    body.IsLive = item.IsLive;
                    _repository.Update(body);
                }
                await _unitOfWork.SaveChangesAsync(0);
            }
            var output = await _repository.GetManyAsync(where: x => ids.Contains(x.Id));
            return output.Results;
        }

        public async Task<IEnumerable<AmendmentBody>> SetPageForManyAsync(SetPage[] data)
        {
            var ids = data.Select(x => x.Id).ToArray();
            if (!_unitOfWork.InMemory)
            {
                foreach (var item in data)
                {
                    await _repository.ExecuteUpdate(
                        x => x.Id == item.Id,
                        x => x.SetProperty(p => p.Page, p => item.Page));
                }
            }
            else
            {
                foreach (var item in data)
                {
                    var body = await _repository.GetByIdAsync(item.Id);
                    body.Page = item.Page;
                    _repository.Update(body);
                }
                await _unitOfWork.SaveChangesAsync(0);
            }
            var output = await _repository.GetManyAsync(where: x => ids.Contains(x.Id));
            return output.Results;
        }
    }
}
