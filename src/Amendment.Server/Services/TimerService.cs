using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Server.Hubs;
using Amendment.Shared;
using Amendment.Shared.Interfaces;
using Amendment.Shared.Responses;
using Microsoft.AspNetCore.SignalR;

namespace Amendment.Server.Services
{
    public class TimerService : ITimerService
    {
        private readonly IHubContext<TimerHub> _timerHub;

        public TimerService(IHubContext<TimerHub> timerHub)
        {
            _timerHub = timerHub;
        }
        private int _seconds;
        private bool _isRunning;
        private bool _show;

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                StateUpdated?.Invoke(this, new StateUpdated(value));
                _timerHub.Clients.All.SendAsync(TimerHubMethods.StateChangedEvent, value);
            }
        }

        public bool Shown
        {
            get => _show;
            set
            {
                _show = value;
                _timerHub.Clients.All.SendAsync(TimerHubMethods.ShowChangedEvent, value);
            }
        }

        public void Show(bool show)
        { 
            Shown = show;
        }

        public int Seconds
        {
            get { return _seconds; }
            set
            {
                _seconds = value; 
                SecondsUpdated?.Invoke(this, new SecondsUpdated(value));
                _timerHub.Clients.All.SendAsync(TimerHubMethods.SecondsChangedEvent, value);
            }
        }

        public void Reset()
        {
            Seconds = 0;
            IsRunning = false;
        }

        public void Reset(int seconds)
        {
            Seconds = seconds;
            IsRunning = false;
        }

        public void Set(int seconds) => Seconds = seconds;
        public void Pause() => IsRunning = false;
        public void Start()
        {
            if (Seconds <= 0 || IsRunning)
                return;

            IsRunning = true;
#pragma warning disable CS4014
            doTimer();
#pragma warning restore CS4014
        }

        private async Task doTimer()
        {
            while (IsRunning && Seconds > 0)
            {
                await Task.Delay(1000);
                if (!IsRunning)
                    return;
                Seconds--;

                if (Seconds == 0)
                {
                    IsRunning = false;
                }
            }
        }

        public event EventHandler<SecondsUpdated> SecondsUpdated = null!;
        public event EventHandler<StateUpdated> StateUpdated = null!;
    }
}
