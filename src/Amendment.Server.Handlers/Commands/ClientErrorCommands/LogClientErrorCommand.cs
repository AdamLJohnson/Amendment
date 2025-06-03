using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Commands.ClientErrorCommands
{
    public sealed record LogClientErrorCommand(
        string ErrorMessage,
        string? StackTrace,
        string Url,
        string UserAgent,
        DateTime Timestamp,
        string? AdditionalContext,
        string ErrorType,
        string? ComponentName,
        string? UserAction,
        int? UserId,
        string? Username
    ) : IRequest<IApiResult<ClientErrorResponse>>;
}
