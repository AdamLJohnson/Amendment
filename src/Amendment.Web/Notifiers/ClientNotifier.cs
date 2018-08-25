using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Service.Infrastructure;
using Amendment.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Web.Notifiers
{
    public class ClientNotifier : IClientNotifier
    {
        private readonly IHubContext<AmendmentHub> _amendmentHub;
        private readonly IHubContext<ScreenHub> _screenhHubContext;

        public ClientNotifier(IHubContext<AmendmentHub> amendmentHub, IHubContext<ScreenHub> screenhHubContext)
        {
            _amendmentHub = amendmentHub;
            _screenhHubContext = screenhHubContext;
        }

        public Task SendToAllAsync(DestinationHub destinationHub, string method, object obj)
        {
            switch (destinationHub)
            {
                case DestinationHub.Amendment:
                    return _amendmentHub.Clients.All.SendAsync(method, obj);
                case DestinationHub.Screen:
                    return _screenhHubContext.Clients.All.SendAsync(method, obj);
                default:
                    throw new ArgumentOutOfRangeException(nameof(destinationHub), destinationHub, null);
            }
        }

        public Task SendToLanguageScreenAsync(int languageId, string method, object obj)
        {
            return _screenhHubContext.Clients.Group($"language_{languageId}").SendAsync(method, obj);
        }
    }
}
