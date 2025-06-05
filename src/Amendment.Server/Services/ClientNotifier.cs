using Amendment.Service.Infrastructure;
using Amendment.Server.Hubs;
using Amendment.Shared.Responses;
using Microsoft.AspNetCore.SignalR;
using MediatR;
using Amendment.Server.Mediator.Queries.AmendmentQueries;
using Mapster;

namespace Amendment.Server.Services
{
    /// <summary>
    /// Real implementation of IClientNotifier that sends SignalR messages to connected clients
    /// </summary>
    public class ClientNotifier : IClientNotifier
    {
        private readonly IHubContext<AmendmentHub> _amendmentHub;
        private readonly IHubContext<ScreenHub> _screenHub;
        private readonly IMediator _mediator;

        public ClientNotifier(
            IHubContext<AmendmentHub> amendmentHub,
            IHubContext<ScreenHub> screenHub,
            IMediator mediator)
        {
            _amendmentHub = amendmentHub;
            _screenHub = screenHub;
            _mediator = mediator;
        }

        public async Task SendToAllAsync(DestinationHub destinationHub, string method, object obj)
        {
            switch (destinationHub)
            {
                case DestinationHub.Amendment:
                    await SendToAmendmentHub(method, obj);
                    break;
                case DestinationHub.Screen:
                    await SendToScreenHub(method, obj);
                    break;
            }
        }

        public async Task SendToLanguageScreenAsync(int languageId, string method, object obj)
        {
            // For now, just send to all screen clients
            // TODO: Implement language-specific groups if needed
            if (method == ClientNotifierMethods.AmendmentBodyChange)
            {
                await _screenHub.Clients.All.SendAsync(method, obj);
            }
        }

        private async Task SendToAmendmentHub(string method, object obj)
        {
            switch (method)
            {
                case ClientNotifierMethods.AmendmentChange:
                    await HandleAmendmentChange(obj);
                    break;
                case ClientNotifierMethods.AmendmentBodyChange:
                    await HandleAmendmentBodyChange(obj);
                    break;
                case ClientNotifierMethods.ClearScreens:
                    await _amendmentHub.Clients.All.SendAsync("ClearScreens");
                    break;
                case ClientNotifierMethods.RefreshSetting:
                    // Extract the actual setting data and send it
                    var settingData = ExtractDataFromServiceObject(obj);
                    if (settingData != null)
                    {
                        await _amendmentHub.Clients.All.SendAsync("SystemSettingUpdate", new SignalRResponse<object>(Amendment.Shared.Enums.OperationType.Update, settingData));
                    }
                    break;
                case ClientNotifierMethods.RefreshLanguage:
                    // Extract the actual language data and send it
                    var languageData = ExtractDataFromServiceObject(obj);
                    if (languageData != null)
                    {
                        await _amendmentHub.Clients.All.SendAsync("LanguageUpdate", new SignalRResponse<object>(Amendment.Shared.Enums.OperationType.Update, languageData));
                    }
                    break;
            }
        }

        private async Task SendToScreenHub(string method, object obj)
        {
            switch (method)
            {
                case ClientNotifierMethods.AmendmentChange:
                    await HandleScreenAmendmentChange(obj);
                    break;
                case ClientNotifierMethods.AmendmentBodyChange:
                    await HandleScreenAmendmentBodyChange(obj);
                    break;
                case ClientNotifierMethods.ClearScreens:
                    await _screenHub.Clients.All.SendAsync("ClearScreens");
                    break;
                case ClientNotifierMethods.RefreshSetting:
                    // Extract the actual setting data and send it
                    var settingData = ExtractDataFromServiceObject(obj);
                    if (settingData != null)
                    {
                        await _screenHub.Clients.All.SendAsync("SystemSettingUpdate", new SignalRResponse<object>(Amendment.Shared.Enums.OperationType.Update, settingData));
                    }
                    break;
            }
        }

        private async Task HandleAmendmentChange(object obj)
        {
            // Extract amendment data from the service object and convert to response
            var amendmentData = ExtractDataFromServiceObject(obj);
            if (amendmentData != null)
            {
                var amendmentResponse = amendmentData.Adapt<AmendmentResponse>();
                await _amendmentHub.Clients.All.SendAsync("AmendmentUpdate", new SignalRResponse<AmendmentResponse>(Amendment.Shared.Enums.OperationType.Update, amendmentResponse));
            }
        }

        private async Task HandleScreenAmendmentChange(object obj)
        {
            // Extract amendment data and check if it's live
            var amendmentData = ExtractDataFromServiceObject(obj);
            if (amendmentData != null)
            {
                var amendmentResponse = amendmentData.Adapt<AmendmentResponse>();
                if (amendmentResponse.IsLive)
                {
                    await _screenHub.Clients.All.SendAsync("AmendmentUpdate", new SignalRResponse<AmendmentResponse>(Amendment.Shared.Enums.OperationType.Update, amendmentResponse));
                }
            }
        }

        private async Task HandleAmendmentBodyChange(object obj)
        {
            // Extract amendment body data from the service object and convert to response
            var amendmentBodyData = ExtractDataFromServiceObject(obj);
            if (amendmentBodyData != null)
            {
                var amendmentBodyResponse = amendmentBodyData.Adapt<AmendmentBodyResponse>();
                await _amendmentHub.Clients.All.SendAsync("AmendmentBodyUpdate", new SignalRResponse<AmendmentBodyResponse>(Amendment.Shared.Enums.OperationType.Update, amendmentBodyResponse));
            }
        }

        private async Task HandleScreenAmendmentBodyChange(object obj)
        {
            // Extract amendment body data and check if the amendment is live
            var amendmentBodyData = ExtractDataFromServiceObject(obj);
            if (amendmentBodyData != null)
            {
                var amendmentBodyResponse = amendmentBodyData.Adapt<AmendmentBodyResponse>();

                // Check if this amendment body belongs to the currently live amendment
                var liveAmendment = await _mediator.Send(new GetLiveAmendmentQuery());
                if (liveAmendment?.Result != null && liveAmendment.Result.Id == amendmentBodyResponse.AmendId)
                {
                    await _screenHub.Clients.All.SendAsync("AmendmentBodyUpdate", new SignalRResponse<AmendmentBodyResponse>(Amendment.Shared.Enums.OperationType.Update, amendmentBodyResponse));
                }
            }
        }

        private object ExtractDataFromServiceObject(object obj)
        {
            // Extract data from the complex object structure used by services
            // The object typically has structure: { id, results, data, amendment, user }
            var objType = obj.GetType();
            var dataProperty = objType.GetProperty("data");
            return dataProperty?.GetValue(obj);
        }
    }
}
