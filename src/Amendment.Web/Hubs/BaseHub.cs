using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Service;
using Amendment.Service.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Web.Hubs
{
    public abstract class BaseHub : Hub
    {
        private readonly ISystemSettingService _systemSettingService;

        protected BaseHub(ISystemSettingService systemSettingService)
        {
            _systemSettingService = systemSettingService;
        }

        public async Task GetSystemSetting(string key)
        {
            var setting = await _systemSettingService.GetSettingAsync(key);
            await Clients.Caller.SendAsync(ClientNotifierMethods.RefreshSetting, setting);
        }
    }
}
