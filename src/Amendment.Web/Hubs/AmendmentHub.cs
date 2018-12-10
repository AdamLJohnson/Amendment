using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Service;
using Amendment.Service.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Web.Hubs
{
    [Authorize]
    public class AmendmentHub : Hub
    {
        private readonly IScreenControlService _screenControlService;
        private readonly IAmendmentService _amendmentService;

        public AmendmentHub(IScreenControlService screenControlService, IAmendmentService amendmentService)
        {
            _screenControlService = screenControlService;
            _amendmentService = amendmentService;
        }

        [Authorize(Roles = "Screen Controller, System Administrator")]
        public Task ClearScreens()
        {
            return _screenControlService.ClearScreensAsync(Context.User.UserId());
        }

        [Authorize(Roles = "Screen Controller, System Administrator")]
        public Task AmendmentGoLive(int amendmentId, bool isLive)
        {
            return _screenControlService.GoLiveAsync(Context.User.UserId(), amendmentId, isLive);
        }

        [Authorize(Roles = "Screen Controller, System Administrator")]
        public Task AmendmentBodyChangePage(int amendmentBodyId, int dir)
        {
            return _screenControlService.AmendmentBodyChangePage(Context.User.UserId(), amendmentBodyId, dir);
        }

        [Authorize(Roles = "Screen Controller, System Administrator")]
        public Task AmendmentBodyChangeAllPages(int amendmentId, int dir)
        {
            return _screenControlService.AmendmentBodyAllPages(Context.User.UserId(), amendmentId, dir);
        }

        [Authorize(Roles = "Screen Controller, System Administrator")]
        public Task AmendmentBodyResetAllPages(int amendmentId)
        {
            return _screenControlService.AmendmentBodyResetAllPages(Context.User.UserId(), amendmentId);
        }

        [Authorize(Roles = "Screen Controller, System Administrator")]
        public Task AmendmentBodyGoLive(int amendmentId, int amendmentBodyId, bool isLive)
        {
            return _screenControlService.GoLiveAsync(Context.User.UserId(), amendmentId, amendmentBodyId, isLive);
        }

        public async Task GetAmendment(int amendmentId)
        {
            var amendment = await _amendmentService.GetAsync(amendmentId);
            await Clients.Caller.SendAsync("amendment.amendmentReturn", amendment);
        }
    }
}
