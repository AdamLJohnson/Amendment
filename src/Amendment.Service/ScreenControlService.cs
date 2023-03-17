using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository;
using Amendment.Repository.Infrastructure;
using Amendment.Service.Infrastructure;
using AutoMapper;

namespace Amendment.Service
{
    public interface IScreenControlService
    {
        Task GoLiveAsync(int userId, int amendmentId, bool isLive);
        Task GoLiveAsync(int userId, int amendmentId, int amendmentBodyId, bool isLive);
        Task ClearScreensAsync(int userId);
        Task UpdateBodyAsync(AmendmentBody item, bool forceSend);
        Task UpdateAmendmentAsync(Model.DataModel.Amendment item);
        Task AmendmentBodyChangePage(int userId, int amendmentBodyId, int dir);
        Task AmendmentBodyAllPages(int userId, int amendmentId, int dir);
        Task AmendmentBodyResetAllPages(int userId, int amendmentId);
    }

    public class ScreenControlService : IScreenControlService
    {
        private readonly IClientNotifier _clientNotifier;
        private readonly IAmendmentRepository _amendmentRepository;
        private readonly IAmendmentBodyRepository _amendmentBodyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;

        public ScreenControlService(IClientNotifier clientNotifier, IAmendmentRepository amendmentRepository, IAmendmentBodyRepository amendmentBodyRepository, IUnitOfWork unitOfWork, IUserService userService)
        {
            _clientNotifier = clientNotifier;
            _amendmentRepository = amendmentRepository;
            _amendmentBodyRepository = amendmentBodyRepository;
            _unitOfWork = unitOfWork;
            _userService = userService;
        }

        public async Task GoLiveAsync(int userId, int amendmentId, bool isLive)
        {
            var amendment = await _amendmentRepository.GetByIdAsync(amendmentId);
            if (amendment != null)
            {
                var user = await _userService.GetForToastAsync(userId);
                _amendmentRepository.SetIsLive(isLive, amendment);
                await _unitOfWork.SaveChangesAsync(userId);
                await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentChange, new { id = amendmentId, results = new OperationResult(OperationType.GoLive), data = amendment, isLive, user });
                await UpdateAmendmentAsync(amendment);
            }
        }

        public async Task GoLiveAsync(int userId, int amendmentId, int amendmentBodyId, bool isLive)
        {
            var amendment = await _amendmentRepository.GetByIdAsync(amendmentId);
            if (amendment == null || !amendment.IsLive) return;

            var amendmentBody = amendment.AmendmentBodies?.Where(b => b.Id == amendmentBodyId).FirstOrDefault();
            if (amendmentBody != null)
            {
                var user = await _userService.GetForToastAsync(userId);
                var changed = isLive != amendmentBody.IsLive;
                _amendmentBodyRepository.SetIsLive(isLive, amendmentBody);
                await _unitOfWork.SaveChangesAsync(userId);
                await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = amendmentBodyId, results = new OperationResult(OperationType.GoLive), data = amendmentBody, amendment, isLive, user });
                if (changed)
                    await UpdateBodyAsync(amendmentBody, changed);
            }
        }

        public async Task ClearScreensAsync(int userId)
        {
            await _clientNotifier.SendToAllAsync(DestinationHub.Screen, ClientNotifierMethods.ClearScreens, null);
            await ClearAll(userId);
        }

        public Task UpdateBodyAsync(AmendmentBody item, bool forceSend)
        {
            if (item.IsLive || forceSend)
                return _clientNotifier.SendToLanguageScreenAsync(item.LanguageId, ClientNotifierMethods.AmendmentBodyChange, item);
            return Task.FromResult(0);
        }

        public Task UpdateAmendmentAsync(Model.DataModel.Amendment item)
        {
            if (item.IsLive)
                return _clientNotifier.SendToAllAsync(DestinationHub.Screen, ClientNotifierMethods.AmendmentChange, item);
            return Task.FromResult(0);
        }

        public async Task AmendmentBodyChangePage(int userId, int amendmentBodyId, int dir)
        {
            var amendmentBody = await _amendmentBodyRepository.GetByIdAsync(amendmentBodyId);
            if (amendmentBody == null)
                return;

            var page = amendmentBody.Page + dir;
            await ChangeBodyPage(userId, amendmentBody, page);
        }

        public async Task AmendmentBodyAllPages(int userId, int amendmentId, int dir)
        {
            var amendment = await _amendmentRepository.GetByIdAsync(amendmentId);
            if (amendment == null) return;

            var list = new List<AmendmentBody>(amendment.AmendmentBodies);

            foreach (var body in list)
            {
                var page = body.Page + dir;
                await ChangeBodyPage(userId, body, page);
            }
        }

        public async Task AmendmentBodyResetAllPages(int userId, int amendmentId)
        {
            var amendment = await _amendmentRepository.GetByIdAsync(amendmentId);
            if (amendment == null) return;

            var list = new List<AmendmentBody>(amendment.AmendmentBodies);

            foreach (var body in list)
            {
                await ChangeBodyPage(userId, body, 0);
            }
        }

        private async Task ChangeBodyPage(int userId, AmendmentBody amendmentBody, int page)
        {
            if (page < 0 || page > amendmentBody.Pages - 1)
                page = 0;

            var user = await _userService.GetForToastAsync(userId);
            var changed = page != amendmentBody.Page;
            _amendmentBodyRepository.ChangePage(page, amendmentBody);
            await _unitOfWork.SaveChangesAsync(userId);
            var amendment = await _amendmentRepository.GetByIdAsync(amendmentBody.AmendId);
            await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = amendmentBody.Id, results = new OperationResult(OperationType.ChangePage), data = amendmentBody, amendment, page, user });
            if (changed)
                await UpdateBodyAsync(amendmentBody, changed);
        }

        private async Task ClearAll(int userId)
        {
            var activeAmendments = await _amendmentRepository.GetManyAsync(where: a => a.IsLive);
            var user = await _userService.GetForToastAsync(userId);
            foreach (var amendment in activeAmendments.Results)
            {
                _amendmentRepository.SetIsLive(false, amendment);
                await _unitOfWork.SaveChangesAsync(userId);
                await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentChange, new { id = amendment.Id, results = new OperationResult(OperationType.GoLive), data = amendment, isLive = false, user });
            }

            var activeAmendmentBodies = await _amendmentBodyRepository.GetManyAsync(where: a => a.IsLive);
            foreach (var amendmentBody in activeAmendmentBodies.Results)
            {
                _amendmentBodyRepository.SetIsLive(false, amendmentBody);
                var amendment = await _amendmentRepository.GetByIdAsync(amendmentBody.AmendId);
                await _unitOfWork.SaveChangesAsync(userId);
                await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = amendmentBody.Id, results = new OperationResult(OperationType.GoLive), data = amendmentBody, amendment, isLive = false, user });
            }
        }
    }
}
