using Microsoft.AspNetCore.SignalR.Client;
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

namespace Amendment.Client.Services
{
    public interface IAmendmentHubCommandService
    {
        Task Connect();
        Task Disconnect();
        Task SetAmendmentBodyLiveAsync(params SetAmendmentBodyLiveCommand[] bodies);
        Task SetAmendmentBodyPage(params SetAmendmentBodyPageCommand[] bodies);
    }

    public sealed class AmendmentHubCommandService : IAmendmentHubCommandService, IDisposable
    {
        private readonly IHubEventService _hubEventService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly NavigationManager _navigationManager;

        private HubConnection? _hubConnection;
        public AmendmentHubCommandService(IHubEventService hubEventService, IRefreshTokenService refreshTokenService, NavigationManager navigationManager)
        {
            _hubEventService = hubEventService;
            _refreshTokenService = refreshTokenService;
            _navigationManager = navigationManager;
        }

        #region IAmendmentHubCommandService
        public async Task Connect()
        {
            var token = await _refreshTokenService.TryRefreshToken();
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_navigationManager.ToAbsoluteUri("/amendmentHub"), options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token)!;
                })
                .Build();

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

            await _hubConnection.StartAsync();
        }

        public Task Disconnect()
        {
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
            _hubConnection?.DisposeAsync();
        }
    }
}
