using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Web.Hubs
{
    [Authorize(Roles = "System Administrator, Amendment Editor, Translator")]
    public class DiffHub : Hub
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DiffHub(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task OnConnectedAsync()
        {
            //Context.Items
            var httpContext = _httpContextAccessor.HttpContext;
            var pageUrlHash = httpContext.Request.Query["pageUrlHash"];
            Groups.AddToGroupAsync(Context.ConnectionId, $"pageUrlHash_{pageUrlHash}");
            return base.OnConnectedAsync();
        }

        [Authorize(Roles = "System Administrator, Amendment Editor, Translator")]
        public async Task XmitPatch(string pageUrlHash, string patch)
        {
            await Clients.OthersInGroup($"pageUrlHash_{pageUrlHash}").SendAsync("receivePatch", patch);
        }
    }
}
