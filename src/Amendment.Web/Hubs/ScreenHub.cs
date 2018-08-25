using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Web.Hubs
{
    public class ScreenHub : Hub
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ScreenHub(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task OnConnectedAsync()
        {
            //Context.Items
            var httpContext = _httpContextAccessor.HttpContext;
            var languageId = httpContext.Request.Query["languageId"];
            Groups.AddToGroupAsync(Context.ConnectionId, $"language_{languageId}");
            return base.OnConnectedAsync();
        }
    }
}
