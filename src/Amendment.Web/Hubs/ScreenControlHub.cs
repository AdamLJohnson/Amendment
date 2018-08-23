using System;
using System.Threading.Tasks;
using Amendment.Service;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Web.Hubs
{
    public class ScreenControlHub : Hub
    {
        private readonly IScreenControlService _screenControlService;

        public ScreenControlHub(IScreenControlService screenControlService)
        {
            _screenControlService = screenControlService;
        }
    }
}