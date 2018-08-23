using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Repository;
using Amendment.Repository.Infrastructure;
using Amendment.Service.Infrastructure;

namespace Amendment.Service
{
    public interface IScreenControlService
    {
        Task GoLiveAsync(int userId, int amendmentId);
        Task GoLiveAsync(int userId, int amendmentId, int amendmentBodyId);
        Task TakeDownAsync(int userId, int amendmentId);
        Task TakeDownAsync(int userId, int amendmentId, int amendmentBodyId);
        Task ClearScreensAsync(int userId);
        Task UpdateBodyAsync(AmendmentBody item);
        Task UpdateAmendmentAsync(Model.DataModel.Amendment item);
    }

    public class ScreenControlService : IScreenControlService
    {
        private readonly IClientNotifier _clientNotifier;
        private readonly IAmendmentRepository _amendmentRepository;
        private readonly IAmendmentBodyRepository _amendmentBodyRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ScreenControlService(IClientNotifier clientNotifier, IAmendmentRepository amendmentRepository, IAmendmentBodyRepository amendmentBodyRepository, IUnitOfWork unitOfWork)
        {
            _clientNotifier = clientNotifier;
            _amendmentRepository = amendmentRepository;
            _amendmentBodyRepository = amendmentBodyRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task GoLiveAsync(int userId, int amendmentId)
        {
            var amendment = await _amendmentRepository.GetByIdAsync(amendmentId);
            if (amendment != null)
            {
                _amendmentRepository.SetIsLive(true, amendment);
                await _unitOfWork.SaveChangesAsync(userId);
                await _clientNotifier.SendAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentChange, new { id = amendmentId, results = new OperationResult(OperationType.Update), data = amendment });
                await _clientNotifier.SendAsync(DestinationHub.Screen, ClientNotifierMethods.GoLive, amendment);
            }
        }

        public async Task GoLiveAsync(int userId, int amendmentId, int amendmentBodyId)
        {
            var amendment = await _amendmentRepository.GetByIdAsync(amendmentId);
            if (amendment == null || !amendment.IsLive) return;

            var amendmentBody = amendment.AmendmentBodies?.Where(b => b.Id == amendmentBodyId).FirstOrDefault();
            if (amendmentBody != null)
            {
                _amendmentBodyRepository.SetIsLive(true, amendmentBody);
                await _unitOfWork.SaveChangesAsync(userId);
                await _clientNotifier.SendAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = amendmentId, results = new OperationResult(OperationType.Update), data = amendmentBody });
                await _clientNotifier.SendAsync(DestinationHub.Screen, ClientNotifierMethods.GoLiveBody, amendmentBody);
            }
        }

        public async Task TakeDownAsync(int userId, int amendmentId)
        {
            var amendment = await _amendmentRepository.GetByIdAsync(amendmentId);
            if (amendment != null)
            {
                _amendmentRepository.SetIsLive(false, amendment);
                await _unitOfWork.SaveChangesAsync(userId);
                await _clientNotifier.SendAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentChange, new { id = amendmentId, results = new OperationResult(OperationType.Update), data = amendment });
                await _clientNotifier.SendAsync(DestinationHub.Screen, ClientNotifierMethods.GoLive, amendment);
            }
        }

        public async Task TakeDownAsync(int userId, int amendmentId, int amendmentBodyId)
        {
            var amendment = await _amendmentRepository.GetByIdAsync(amendmentId);

            var amendmentBody = amendment?.AmendmentBodies?.Where(b => b.Id == amendmentBodyId).FirstOrDefault();
            if (amendmentBody != null)
            {
                _amendmentBodyRepository.SetIsLive(false, amendmentBody);
                await _unitOfWork.SaveChangesAsync(userId);
                await _clientNotifier.SendAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = amendmentId, results = new OperationResult(OperationType.Update), data = amendmentBody });
                await _clientNotifier.SendAsync(DestinationHub.Screen, ClientNotifierMethods.GoLiveBody, amendmentBody);
            }
        }

        public async Task ClearScreensAsync(int userId)
        {
            await _clientNotifier.SendAsync(DestinationHub.Screen, ClientNotifierMethods.ClearScreens, null);
            await ClearAll(userId);
        }

        public Task UpdateBodyAsync(AmendmentBody item)
        {
            return _clientNotifier.SendAsync(DestinationHub.Screen, ClientNotifierMethods.AmendmentBodyChange, item);
        }

        public Task UpdateAmendmentAsync(Model.DataModel.Amendment item)
        {
            return _clientNotifier.SendAsync(DestinationHub.Screen, ClientNotifierMethods.AmendmentChange, item);
        }

        private async Task ClearAll(int userId)
        {
            var activeAmendments = await _amendmentRepository.GetManyAsync(where: a => a.IsLive);
            foreach (var amendment in activeAmendments.Results)
            {
                _amendmentRepository.SetIsLive(false, amendment);
                await _unitOfWork.SaveChangesAsync(userId);
                await _clientNotifier.SendAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentChange, new { id = amendment.Id, results = new OperationResult(OperationType.Update), data = amendment });
            }

            var activeAmendmentBodies = await _amendmentBodyRepository.GetManyAsync(where: a => a.IsLive);
            foreach (var amendmentBody in activeAmendmentBodies.Results)
            {
                _amendmentBodyRepository.SetIsLive(false, amendmentBody);
                await _unitOfWork.SaveChangesAsync(userId);
                await _clientNotifier.SendAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, new { id = amendmentBody.Id, results = new OperationResult(OperationType.Update), data = amendmentBody });
            }
        }
    }
}
