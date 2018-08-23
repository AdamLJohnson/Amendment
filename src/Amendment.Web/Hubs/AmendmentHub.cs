using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Web.Hubs
{
    [Authorize]
    public class AmendmentHub : Hub
    {
        private readonly IScreenControlService _screenControlService;

        public AmendmentHub(IScreenControlService screenControlService)
        {
            _screenControlService = screenControlService;
        }

        public Task ClearScreens()
        {
            return _screenControlService.ClearScreensAsync(Context.User.UserId());
        }

        public Task AmendmentGoLive(int amendmentId)
        {
            return _screenControlService.GoLiveAsync(Context.User.UserId(), amendmentId);
        }
    }
}
