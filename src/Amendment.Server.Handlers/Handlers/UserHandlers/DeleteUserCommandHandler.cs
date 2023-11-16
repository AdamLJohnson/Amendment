using System.Net;
using Amendment.Server.Mediator.Commands.UserCommands;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.UserHandlers;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, IApiResult>
{
    private readonly ILogger<CreateUserCommandHandler> _logger;
    private readonly IUserService _userService;

    public DeleteUserCommandHandler(ILogger<CreateUserCommandHandler> logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    public async Task<IApiResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userService.GetAsync(request.Id);
        if (user == null)
            return new ApiFailedResult<UserResponse>(HttpStatusCode.NotFound);

        var deleteResults = await _userService.DeleteAsync(user, request.SavingUserId);
        if (!deleteResults.Succeeded)
        {
            _logger.LogError(120, "Delete User Failed {deleteResults}", deleteResults);
            return new ApiCommandFailedResult { StatusCode = HttpStatusCode.InternalServerError };
        }
        
        return new ApiCommandSuccessResult();
    }
}