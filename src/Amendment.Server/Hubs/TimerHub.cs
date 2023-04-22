using Amendment.Model.DataModel;
using Amendment.Server.Mediator.Commands.AmendmentBodyCommands;
using Amendment.Server.Services;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Interfaces;
using Amendment.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Server.Hubs
{
    public class TimerHub : Hub
    {
        private readonly ITimerService _timerService;

        public TimerHub(ITimerService timerService)
        { 
            _timerService = timerService;
        }

        public CurrentState CurrentState()
        {
            return new CurrentState(_timerService.IsRunning, _timerService.Seconds, _timerService.Shown);
        }

        [Authorize(Roles = RoleGroups.AdminScreenController)]
        public void Reset()
        {
            _timerService.Reset();
        }

        [Authorize(Roles = RoleGroups.AdminScreenController)]
        public void ResetWithTime(int seconds)
        {
            _timerService.Reset(seconds);
        }

        [Authorize(Roles = RoleGroups.AdminScreenController)]
        public void Set(int seconds)
        {
            _timerService.Set(seconds);
        }

        [Authorize(Roles = RoleGroups.AdminScreenController)]
        public void Pause()
        {
            _timerService.Pause();
        }

        [Authorize(Roles = RoleGroups.AdminScreenController)]
        public void Start()
        {
            _timerService.Start();
        }

        [Authorize(Roles = RoleGroups.AdminScreenController)]
        public void Show(bool show)
        {
            _timerService.Show(show);
        }
    }
}
