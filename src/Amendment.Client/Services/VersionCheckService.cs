using Amendment.Shared.Responses;
using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace Amendment.Client.Services;

public interface IVersionCheckService
{
    Task StartVersionCheckingAsync();
    Task StopVersionCheckingAsync();
    Task<bool> CheckForUpdatesAsync();
    event EventHandler<VersionUpdateAvailableEventArgs>? UpdateAvailable;
}

public class VersionUpdateAvailableEventArgs : EventArgs
{
    public VersionResponse CurrentVersion { get; set; } = new();
    public VersionResponse NewVersion { get; set; } = new();
}

public class VersionCheckService : IVersionCheckService, IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<VersionCheckService> _logger;
    private readonly IJSRuntime _jsRuntime;
    
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<VersionCheckService>? _dotNetReference;
    private bool _isChecking = false;
    private VersionResponse? _currentVersion;
    
    // Check every 5 minutes by default
    private const int CheckIntervalMinutes = 5;
    private const string CurrentVersionKey = "currentAppVersion";

    public event EventHandler<VersionUpdateAvailableEventArgs>? UpdateAvailable;

    public VersionCheckService(
        HttpClient httpClient, 
        ILocalStorageService localStorage, 
        ILogger<VersionCheckService> logger,
        IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _logger = logger;
        _jsRuntime = jsRuntime;
    }

    public async Task StartVersionCheckingAsync()
    {
        if (_isChecking)
            return;

        try
        {
            _isChecking = true;
            
            // Load current version from local storage
            await LoadCurrentVersionAsync();
            
            // Initialize JavaScript module for periodic checking
            _jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/versionChecker.js");
            _dotNetReference = DotNetObjectReference.Create(this);
            
            // Start periodic checking
            await _jsModule.InvokeVoidAsync("startVersionChecking", _dotNetReference, CheckIntervalMinutes);
            
            // Do an initial check
            await CheckForUpdatesAsync();
            
            _logger.LogInformation("Version checking started with {IntervalMinutes} minute intervals", CheckIntervalMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start version checking");
            _isChecking = false;
        }
    }

    public async Task StopVersionCheckingAsync()
    {
        if (!_isChecking)
            return;

        try
        {
            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("stopVersionChecking");
            }
            
            _isChecking = false;
            _logger.LogInformation("Version checking stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping version checking");
        }
    }

    [JSInvokable]
    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            _logger.LogDebug("Checking for application updates...");
            
            var serverVersion = await GetServerVersionAsync();
            if (serverVersion == null)
            {
                _logger.LogWarning("Could not retrieve server version");
                return false;
            }

            if (_currentVersion == null)
            {
                // First time - store the current version
                _currentVersion = serverVersion;
                await SaveCurrentVersionAsync(serverVersion);
                _logger.LogInformation("Initial version stored: {Version}", serverVersion.Version);
                return false;
            }

            // Compare versions
            if (IsNewerVersion(serverVersion, _currentVersion))
            {
                _logger.LogInformation("New version available: {NewVersion} (current: {CurrentVersion})", 
                    serverVersion.Version, _currentVersion.Version);
                
                UpdateAvailable?.Invoke(this, new VersionUpdateAvailableEventArgs
                {
                    CurrentVersion = _currentVersion,
                    NewVersion = serverVersion
                });
                
                return true;
            }

            _logger.LogDebug("No updates available. Current version: {Version}", _currentVersion.Version);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return false;
        }
    }

    private async Task<VersionResponse?> GetServerVersionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/version");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<VersionResponse>();
            }
            
            _logger.LogWarning("Version check failed with status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get server version");
            return null;
        }
    }

    private async Task LoadCurrentVersionAsync()
    {
        try
        {
            var versionJson = await _localStorage.GetItemAsStringAsync(CurrentVersionKey);
            if (!string.IsNullOrEmpty(versionJson))
            {
                _currentVersion = JsonSerializer.Deserialize<VersionResponse>(versionJson);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load current version from local storage");
        }
    }

    private async Task SaveCurrentVersionAsync(VersionResponse version)
    {
        try
        {
            var versionJson = JsonSerializer.Serialize(version);
            await _localStorage.SetItemAsStringAsync(CurrentVersionKey, versionJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save current version to local storage");
        }
    }

    private static bool IsNewerVersion(VersionResponse serverVersion, VersionResponse currentVersion)
    {
        // Compare by build date first (most reliable for deployments)
        if (serverVersion.BuildDate > currentVersion.BuildDate)
            return true;

        // If build dates are the same, compare version strings
        if (serverVersion.BuildDate == currentVersion.BuildDate)
        {
            return string.Compare(serverVersion.Version, currentVersion.Version, StringComparison.OrdinalIgnoreCase) > 0;
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        await StopVersionCheckingAsync();
        
        _dotNetReference?.Dispose();
        
        if (_jsModule != null)
        {
            await _jsModule.DisposeAsync();
        }
    }
}
