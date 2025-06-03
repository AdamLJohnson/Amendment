using Amendment.Server.Extensions;
using Amendment.Server.Mediator.Commands.ClientErrorCommands;
using Amendment.Shared.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Amendment.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientErrorController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ClientErrorController> _logger;
        private static readonly Dictionary<string, DateTime> _rateLimitTracker = new();
        private static readonly object _rateLimitLock = new();
        private const int RateLimitWindowMinutes = 1;
        private const int MaxErrorsPerWindow = 10;

        public ClientErrorController(IMediator mediator, ILogger<ClientErrorController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost("log")]
        [AllowAnonymous] // Allow anonymous to catch errors from unauthenticated users
        public async Task<IResult> LogError([FromBody] ClientErrorRequest request)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(request.ErrorMessage))
                {
                    return Results.BadRequest("Error message is required");
                }

                // Rate limiting by IP address
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (!IsWithinRateLimit(clientIp))
                {
                    _logger.LogWarning("Rate limit exceeded for client IP: {ClientIp}", clientIp);
                    return Results.StatusCode(429); // Too Many Requests
                }

                // Get user information if authenticated
                int? userId = null;
                string? username = null;
                
                if (User.Identity?.IsAuthenticated == true)
                {
                    if (int.TryParse(User.FindFirst("id")?.Value, out int parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                    username = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("username")?.Value;
                }

                // Sanitize and limit data sizes
                var sanitizedRequest = SanitizeRequest(request);

                var command = new LogClientErrorCommand(
                    sanitizedRequest.ErrorMessage,
                    sanitizedRequest.StackTrace,
                    sanitizedRequest.Url,
                    sanitizedRequest.UserAgent,
                    sanitizedRequest.Timestamp,
                    sanitizedRequest.AdditionalContext,
                    sanitizedRequest.ErrorType,
                    sanitizedRequest.ComponentName,
                    sanitizedRequest.UserAction,
                    userId,
                    username
                );

                var result = await _mediator.Send(command);
                return result.ToResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process client error logging request");
                return Results.Problem("Internal server error while logging client error");
            }
        }

        private bool IsWithinRateLimit(string clientIdentifier)
        {
            lock (_rateLimitLock)
            {
                var now = DateTime.UtcNow;
                var windowStart = now.AddMinutes(-RateLimitWindowMinutes);

                // Clean up old entries
                var keysToRemove = _rateLimitTracker
                    .Where(kvp => kvp.Value < windowStart)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _rateLimitTracker.Remove(key);
                }

                // Count recent requests from this client
                var recentRequests = _rateLimitTracker
                    .Count(kvp => kvp.Key.StartsWith(clientIdentifier) && kvp.Value >= windowStart);

                if (recentRequests >= MaxErrorsPerWindow)
                {
                    return false;
                }

                // Add this request
                _rateLimitTracker[$"{clientIdentifier}_{now.Ticks}"] = now;
                return true;
            }
        }

        private ClientErrorRequest SanitizeRequest(ClientErrorRequest request)
        {
            const int maxLength = 5000;
            const int maxStackTraceLength = 10000;

            return new ClientErrorRequest
            {
                ErrorMessage = TruncateString(request.ErrorMessage, maxLength),
                StackTrace = TruncateString(request.StackTrace, maxStackTraceLength),
                Url = TruncateString(request.Url, 2000),
                UserAgent = TruncateString(request.UserAgent, 1000),
                Timestamp = request.Timestamp,
                AdditionalContext = TruncateString(request.AdditionalContext, maxLength),
                ErrorType = TruncateString(request.ErrorType, 100),
                ComponentName = TruncateString(request.ComponentName, 200),
                UserAction = TruncateString(request.UserAction, 500)
            };
        }

        private string? TruncateString(string? input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Length <= maxLength ? input : input.Substring(0, maxLength) + "...";
        }
    }
}
