using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace Amendment.Client.Services
{
    public class ClientErrorService : IClientErrorService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClientErrorService> _logger;
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;
        private static readonly Queue<DateTime> _recentErrors = new();
        private static readonly object _rateLimitLock = new();
        private const int MaxErrorsPerMinute = 5;

        public ClientErrorService(
            HttpClient httpClient, 
            ILogger<ClientErrorService> logger, 
            NavigationManager navigationManager,
            IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _logger = logger;
            _navigationManager = navigationManager;
            _jsRuntime = jsRuntime;
        }

        public async Task LogErrorAsync(ClientErrorRequest errorRequest)
        {
            try
            {
                // Rate limiting
                if (!IsWithinRateLimit())
                {
                    _logger.LogWarning("Client error logging rate limit exceeded");
                    return;
                }

                // Ensure required fields are populated
                if (string.IsNullOrWhiteSpace(errorRequest.Url))
                {
                    errorRequest.Url = _navigationManager.Uri;
                }

                if (string.IsNullOrWhiteSpace(errorRequest.UserAgent))
                {
                    try
                    {
                        errorRequest.UserAgent = await _jsRuntime.InvokeAsync<string>("eval", "navigator.userAgent");
                    }
                    catch
                    {
                        errorRequest.UserAgent = "Unknown";
                    }
                }

                errorRequest.Timestamp = DateTime.UtcNow;

                // Send to server
                var response = await _httpClient.PostAsJsonAsync("api/ClientError/log", errorRequest);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to send client error to server. Status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // Don't let error logging cause more errors
                _logger.LogError(ex, "Failed to log client error: {ErrorMessage}", errorRequest.ErrorMessage);
            }
        }

        public async Task LogErrorAsync(string errorMessage, string? stackTrace = null, string? componentName = null, string? userAction = null, string? additionalContext = null)
        {
            var errorRequest = new ClientErrorRequest
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                ErrorType = "Blazor",
                ComponentName = componentName,
                UserAction = userAction,
                AdditionalContext = additionalContext
            };

            await LogErrorAsync(errorRequest);
        }

        public async Task LogJavaScriptErrorAsync(string errorMessage, string? stackTrace = null, string? url = null, string? additionalContext = null)
        {
            var errorRequest = new ClientErrorRequest
            {
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                Url = url ?? _navigationManager.Uri,
                ErrorType = "JavaScript",
                AdditionalContext = additionalContext
            };

            await LogErrorAsync(errorRequest);
        }

        private bool IsWithinRateLimit()
        {
            lock (_rateLimitLock)
            {
                var now = DateTime.UtcNow;
                var oneMinuteAgo = now.AddMinutes(-1);

                // Remove old entries
                while (_recentErrors.Count > 0 && _recentErrors.Peek() < oneMinuteAgo)
                {
                    _recentErrors.Dequeue();
                }

                // Check if we're within the limit
                if (_recentErrors.Count >= MaxErrorsPerMinute)
                {
                    return false;
                }

                // Add this error
                _recentErrors.Enqueue(now);
                return true;
            }
        }
    }
}
