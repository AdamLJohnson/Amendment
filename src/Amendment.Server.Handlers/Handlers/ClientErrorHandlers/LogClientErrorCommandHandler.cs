using Amendment.Server.Mediator.Commands.ClientErrorCommands;
using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Amendment.Server.Mediator.Handlers.ClientErrorHandlers
{
    public sealed class LogClientErrorCommandHandler : IRequestHandler<LogClientErrorCommand, IApiResult<ClientErrorResponse>>
    {
        private readonly ILogger<LogClientErrorCommandHandler> _logger;

        public LogClientErrorCommandHandler(ILogger<LogClientErrorCommandHandler> logger)
        {
            _logger = logger;
        }

        public async Task<IApiResult<ClientErrorResponse>> Handle(LogClientErrorCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Create structured log data
                var errorData = new
                {
                    Source = "CLIENT_SIDE",
                    ErrorType = request.ErrorType,
                    ErrorMessage = request.ErrorMessage,
                    StackTrace = request.StackTrace,
                    Url = request.Url,
                    UserAgent = request.UserAgent,
                    Timestamp = request.Timestamp,
                    ComponentName = request.ComponentName,
                    UserAction = request.UserAction,
                    AdditionalContext = request.AdditionalContext,
                    UserId = request.UserId,
                    Username = request.Username
                };

                // Log the client-side error with appropriate level
                _logger.LogError(
                    "CLIENT_ERROR: {ErrorType} - {ErrorMessage} | User: {Username} ({UserId}) | URL: {Url} | Component: {ComponentName} | UserAgent: {UserAgent} | Details: {ErrorDetails}",
                    request.ErrorType,
                    request.ErrorMessage,
                    request.Username ?? "Anonymous",
                    request.UserId ?? 0,
                    request.Url,
                    request.ComponentName ?? "Unknown",
                    request.UserAgent,
                    JsonSerializer.Serialize(errorData)
                );

                var response = new ClientErrorResponse
                {
                    Success = true,
                    Message = "Error logged successfully",
                    ProcessedAt = DateTime.UtcNow
                };

                return new ApiSuccessResult<ClientErrorResponse>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log client-side error: {ErrorMessage}", request.ErrorMessage);
                
                var response = new ClientErrorResponse
                {
                    Success = false,
                    Message = "Failed to log error",
                    ProcessedAt = DateTime.UtcNow
                };

                return new ApiSuccessResult<ClientErrorResponse>(response);
            }
        }
    }
}
