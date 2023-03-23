﻿using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Client.Repository;
using Amendment.Client.AuthProviders;
using Amendment.Shared.Responses;
using Amendment.Shared.SignalRCommands;
using Microsoft.AspNetCore.Components;
using Amendment.Client.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Amendment.Client.Services
{
    public interface IScreenHubCommandService : IDisposable, INotifyPropertyChanged
    {
        Task Connect();
        Task Disconnect();
        bool IsConnected { get; }
    }
    public interface IAmendmentHubCommandService : IDisposable, INotifyPropertyChanged
    {
        Task Connect();
        Task Disconnect();
        Task SetAmendmentBodyLiveAsync(params SetAmendmentBodyLiveCommand[] bodies);
        Task SetAmendmentBodyPage(params SetAmendmentBodyPageCommand[] bodies);
        bool IsConnected { get; }
    }

    public class ScreenHubCommandService : BaseHubCommandService, IScreenHubCommandService
    {
        public ScreenHubCommandService(IHubEventService hubEventService, IRefreshTokenService refreshTokenService, NavigationManager navigationManager) : base("/screenHub", false, hubEventService, refreshTokenService, navigationManager)
        {
        }
    }

    public class AmendmentHubCommandService : BaseHubCommandService, IAmendmentHubCommandService
    {
        public AmendmentHubCommandService(IHubEventService hubEventService, IRefreshTokenService refreshTokenService, NavigationManager navigationManager) : base("/amendmentHub", true, hubEventService, refreshTokenService, navigationManager)
        {
        }
    }

    public class BaseHubCommandService : IDisposable, INotifyPropertyChanged
    {
        private readonly string _url;
        private readonly bool _needAuth;
        private readonly IHubEventService _hubEventService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly NavigationManager _navigationManager;

        private HubConnection? _hubConnection;
        private bool _isConnected;

        public BaseHubCommandService(string url, bool needAuth, IHubEventService hubEventService, IRefreshTokenService refreshTokenService, NavigationManager navigationManager)
        {
            _url = url;
            _needAuth = needAuth;
            _hubEventService = hubEventService;
            _refreshTokenService = refreshTokenService;
            _navigationManager = navigationManager;
        }

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

        #region IAmendmentHubCommandService
        public async Task Connect()
        {
            var token = "";
            if (_needAuth)
                token = await _refreshTokenService.TryRefreshToken();

            _hubConnection = new HubConnectionBuilder()
                .WithAutomaticReconnect(new SignalRRetryPolicy())
                .WithUrl(_navigationManager.ToAbsoluteUri(_url), options =>
                {
                    if (_needAuth)
                    {
                        options.AccessTokenProvider = () => Task.FromResult(token)!;
                    }
                })
                .Build();
            
            _hubConnection.Closed += HubConnectionOnClosed;
            _hubConnection.Reconnecting += HubConnectionOnClosed;
            _hubConnection.Reconnected += HubConnectionOnReconnected;

            _hubConnection.On<SignalRResponse<AmendmentResponse>>("AmendmentUpdate", response =>
            {
                _hubEventService.OnAmendmentUpdated(response);
            });

            _hubConnection.On<SignalRResponse<AmendmentBodyResponse>>("AmendmentBodyUpdate", response =>
            {
                _hubEventService.OnAmendmentBodyUpdated(response);
            });

            _hubConnection.On<SignalRResponse<List<AmendmentBodyResponse>>>("AmendmentBodyUpdateMany", response =>
            {
                _hubEventService.OnAmendmentBodyUpdatedMany(response);
            });

            _hubConnection.On<SignalRResponse<SystemSettingResponse>>("SystemSettingUpdate", response =>
            {
                _hubEventService.OnSystemSettingUpdated(response);
            });

            _hubConnection.On("ClearScreens", () =>
            {
                _hubEventService.OnClearScreens();
            });

            await StartAsync();
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
                await _hubConnection.StartAsync();
                IsConnected = true;
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
            if (_hubConnection != null)
            {
                _hubConnection.Closed -= HubConnectionOnClosed;
                _hubConnection.Reconnecting -= HubConnectionOnClosed;
                _hubConnection.Reconnected -= HubConnectionOnReconnected;
            }

            IsConnected = false;
            return _hubConnection?.StopAsync() ?? Task.CompletedTask;
        }
        #endregion

        #region IAmendmentHubClientService
        public Task SetAmendmentBodyLiveAsync(SetAmendmentBodyLiveCommand[] bodies)
        {
            var output = new SetAmendmentBodyLiveCommands(commands: bodies);
            return _hubConnection?.InvokeAsync("AmendmentBodyLive", output) ?? Task.CompletedTask;
        }

        public Task SetAmendmentBodyPage(SetAmendmentBodyPageCommand[] bodies)
        {
            var output = new SetAmendmentBodyPageCommands(commands: bodies);
            return _hubConnection?.InvokeAsync("AmendmentBodyPage", output) ?? Task.CompletedTask;
        }
        #endregion
        public void Dispose()
        {
            if (_hubConnection != null)
            {
                _hubConnection.Closed -= HubConnectionOnClosed;
                _hubConnection.Reconnecting -= HubConnectionOnClosed;
                _hubConnection.Reconnected -= HubConnectionOnReconnected;
            }
            _hubConnection?.DisposeAsync();
        }

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
    }
}