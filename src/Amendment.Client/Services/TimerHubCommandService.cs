using Amendment.Client.AuthProviders;
using Amendment.Client.Helpers;
using Amendment.Client.Repository;
using Amendment.Shared.Responses;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Amendment.Shared;
using Amendment.Shared.SignalRCommands;

namespace Amendment.Client.Services
{
    public interface ITimerHubCommandService : INotifyPropertyChanged//, IDisposable
    {
        Task Connect();
        Task Disconnect();
        Task<CurrentState> GetCurrentStateAsync();
        bool IsConnected { get; }
    }
    public interface ITimerControlHubCommandService : INotifyPropertyChanged//, IDisposable
    {
        Task Connect();
        Task Disconnect();
        Task Reset();
        Task Reset(int seconds);
        Task Set(int seconds);
        Task Pause();
        Task Start();
        Task<CurrentState> GetCurrentStateAsync();
        bool IsConnected { get; }
        Task Show(bool show);
    }

    public class TimerControlHubCommandService : BaseTimerHubCommandService, ITimerControlHubCommandService
    {
        public TimerControlHubCommandService(ITimerEventService timerEventService, IRefreshTokenService refreshTokenService, NavigationManager navigationManager) : base("/timerHub", true, timerEventService, refreshTokenService, navigationManager)
        {
        }
    }
    public class TimerHubCommandService : BaseTimerHubCommandService, ITimerHubCommandService
    {
        public TimerHubCommandService(ITimerEventService timerEventService, IRefreshTokenService refreshTokenService, NavigationManager navigationManager) : base("/timerHub", false, timerEventService, refreshTokenService, navigationManager)
        {
        }
    }

    public abstract class BaseTimerHubCommandService
    {
        private readonly string _url;
        private readonly bool _needAuth;
        private readonly ITimerEventService _timerEventService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly NavigationManager _navigationManager;

        private HubConnection? _hubConnection;
        private bool _isConnected;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (value == _isConnected) return;
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        protected BaseTimerHubCommandService(string url, bool needAuth, ITimerEventService timerEventService, IRefreshTokenService refreshTokenService, NavigationManager navigationManager)
        {
            _url = url;
            _needAuth = needAuth;
            _timerEventService = timerEventService;
            _refreshTokenService = refreshTokenService;
            _navigationManager = navigationManager;
        }

        public async Task Connect()
        {
            if (_hubConnection == null)
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithAutomaticReconnect(new SignalRRetryPolicy())
                    .WithUrl(_navigationManager.ToAbsoluteUri(_url), options =>
                    {
                        if (_needAuth)
                        {
                            options.AccessTokenProvider = () => _refreshTokenService.TryRefreshToken()!;
                        }
                    })
                    .Build();

                _hubConnection.Closed += HubConnectionOnClosed;
                _hubConnection.Reconnecting += HubConnectionOnClosed;
                _hubConnection.Reconnected += HubConnectionOnReconnected;

                _hubConnection.On<int>(TimerHubMethods.SecondsChangedEvent, response =>
                {
                    _timerEventService.OnSecondsUpdated(new SecondsUpdated(response));
                });

                _hubConnection.On<bool>(TimerHubMethods.StateChangedEvent, response =>
                {
                    _timerEventService.OnStateUpdated(new StateUpdated(response));
                });

                _hubConnection.On<bool>(TimerHubMethods.ShowChangedEvent, response =>
                {
                    _timerEventService.OnShowUpdated(new ShowUpdated(response));
                });

                await StartAsync();
            }
        }

        private Task HubConnectionOnReconnected(string? arg)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        private Task HubConnectionOnClosed(Exception? arg)
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public async Task StartAsync()
        {
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.StartAsync();
                    IsConnected = true;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Closed, starting delay");
                await Task.Delay(5000);
                Console.WriteLine("Restarting");
                await StartAsync();
            }
        }

        public Task Disconnect()
        {
            return Task.CompletedTask;
            if (!IsConnected)
                return Task.CompletedTask;

            if (_hubConnection != null)
            {
                _hubConnection.Closed -= HubConnectionOnClosed;
                _hubConnection.Reconnecting -= HubConnectionOnClosed;
                _hubConnection.Reconnected -= HubConnectionOnReconnected;
            }

            IsConnected = false;
            return _hubConnection?.StopAsync() ?? Task.CompletedTask;
        }

        //public void Dispose()
        //{
        //    if (_hubConnection != null)
        //    {
        //        _hubConnection.Closed -= HubConnectionOnClosed;
        //        _hubConnection.Reconnecting -= HubConnectionOnClosed;
        //        _hubConnection.Reconnected -= HubConnectionOnReconnected;
        //    }
        //    _hubConnection?.DisposeAsync();
        //}

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }


        public async Task<CurrentState> GetCurrentStateAsync()
        {
            var output = await _hubConnection?.InvokeAsync<CurrentState?>("CurrentState")!;
            if (output != null) return output;
            return new CurrentState(false, 0, false);
        }

        public Task Reset()
        {
            return _hubConnection?.InvokeAsync("Reset")!;
        }

        public Task Reset(int seconds)
        {
            return _hubConnection?.InvokeAsync("ResetWithTime", seconds)!;
        }

        public Task Set(int seconds)
        {
            return _hubConnection?.InvokeAsync("Set", seconds)!;
        }

        public Task Pause()
        {
            return _hubConnection?.InvokeAsync("Pause")!;
        }

        public Task Start()
        {
            return _hubConnection?.InvokeAsync("Start")!;
        }

        public Task Show(bool show)
        {
            return _hubConnection?.InvokeAsync("Show", show)!;
        }
    }
}
