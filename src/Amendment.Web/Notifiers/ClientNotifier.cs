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

        public ClientNotifier(IHubContext<AmendmentHub> amendmentHub)
        {
            _amendmentHub = amendmentHub;
        }

        public Task SendAsync(DestinationHub destinationHub, string method, object obj)
        {
            switch (destinationHub)
            {
                case DestinationHub.Amendment:
                    return _amendmentHub.Clients.All.SendAsync(method, obj);
                case DestinationHub.Screen:
                    return _amendmentHub.Clients.All.SendAsync(method, obj);
                default:
                    throw new ArgumentOutOfRangeException(nameof(destinationHub), destinationHub, null);
            }
        }
    }
}
