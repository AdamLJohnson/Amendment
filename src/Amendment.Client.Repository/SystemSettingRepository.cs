using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml;
using Amendment.Client.Repository.Infrastructure;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Microsoft.Extensions.Logging;

namespace Amendment.Client.Repository
{
    public interface ISystemSettingRepository : IHttpRepository<SystemSettingRequest, SystemSettingResponse>, INotifyPropertyChanged
    {
        bool ShowDeafSigner { get; }
        bool ShowDeafSignerBox { get; }
        bool InvertedSlideText { get; }
    }
    public class SystemSettingRepository : HttpRepository<SystemSettingRequest, SystemSettingResponse>, ISystemSettingRepository, IDisposable
    {
        private readonly IHubEventService _hubEventService;

        public bool ShowDeafSigner
        {
            get => _showDeafSigner;
            private set
            {
                if (value == _showDeafSigner) return;
                _showDeafSigner = value;
                OnPropertyChanged();
            }
        }

        public bool ShowDeafSignerBox
        {
            get => _showDeafSignerBox;
            private set
            {
                if (value == _showDeafSignerBox) return;
                _showDeafSignerBox = value;
                OnPropertyChanged();
            }
        }

        public bool InvertedSlideText
        {
            get => _invertedSlideText;
            private set
            {
                if (value == _invertedSlideText) return;
                _invertedSlideText = value;
                OnPropertyChanged();
            }
        }

        protected override string BaseUrl { get; set; } = "api/SystemSetting";
        private List<SystemSettingResponse> _systemSettings = new();
        private bool _showDeafSigner;
        private bool _showDeafSignerBox;
        private bool _invertedSlideText;

        public SystemSettingRepository(ILogger<SystemSettingRepository> logger, HttpClient client, IHubEventService hubEventService, INotificationServiceWrapper notificationServiceWrapper) : base(logger, client, notificationServiceWrapper)
        {
            _hubEventService = hubEventService;
            _hubEventService.SystemSettingUpdated += HubEventServiceOnSystemSettingUpdated;
        }

        private void HubEventServiceOnSystemSettingUpdated(object? sender, SignalRResponse<SystemSettingResponse> e)
        {
            var i = _systemSettings.FindIndex(x => x.Id == e.Value.Id);
            if (i > -1)
                _systemSettings[i] = e.Value;
            UpdateSettings();
        }

        public override async Task<IEnumerable<SystemSettingResponse>> GetAsync()
        {
            if (_systemSettings != null && _systemSettings.Any())
                return _systemSettings;

            var results = await base.GetAsync();
            _systemSettings = results.ToList();

            UpdateSettings();

            return _systemSettings;
        }

        private void UpdateSettings()
        {
            ShowDeafSigner = convertToBool(_systemSettings?.FirstOrDefault(x => x.Key == "ShowDeafSigner")?.Value);
            ShowDeafSignerBox = convertToBool(_systemSettings?.FirstOrDefault(x => x.Key == "ShowDeafSignerBox")?.Value);
            InvertedSlideText = convertToBool(_systemSettings?.FirstOrDefault(x => x.Key == "InvertedSlideText")?.Value);
        }

        private bool convertToBool(string? input)
        {
            if(input == null) return false;

            if(bool.TryParse(input, out bool result))
                return result;

            return input != "0";
        }

        public void Dispose()
        {
            _hubEventService.SystemSettingUpdated -= HubEventServiceOnSystemSettingUpdated;
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
