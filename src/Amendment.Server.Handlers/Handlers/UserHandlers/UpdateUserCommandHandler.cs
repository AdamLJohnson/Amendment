using System.Net;
using Amendment.Model.DataModel;
using Amendment.Server.Mediator.Commands.UserCommands;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.UserHandlers;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, IApiResult<UserResponse>>
{
    private readonly ILogger<CreateUserCommandHandler> _logger;
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IPasswordHashService _passwordHashService;

    public UpdateUserCommandHandler(ILogger<CreateUserCommandHandler> logger, IUserService userService, IRoleService roleService, IPasswordHashService passwordHashService)
    {
        _logger = logger;
        _userService = userService;
        _roleService = roleService;
        _passwordHashService = passwordHashService;
    }

    public async Task<IApiResult<UserResponse>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userService.GetAsync(request.Id);
        if (user == null)
            return new ApiFailedResult<UserResponse>(HttpStatusCode.NotFound);

        user.Username = request.Username;
        user.Name = request.Name;
        user.Email = request.Email;
        user.Password = string.IsNullOrEmpty(request.Password) ? user.Password : _passwordHashService.HashPassword(request.Password);
        user.LastUpdatedBy = request.SavingUserId;
        user.LastUpdated = DateTime.UtcNow;

        var roles = new List<Role>();
        foreach (var requestRole in request.Roles)
        {
            var role = await _roleService.GetAsync(requestRole);
            roles.Add(role);
        }
        user.Roles = roles;

        var updateResults = await _userService.UpdateAsync(user, request.SavingUserId);
        if (!updateResults.Succeeded)
        {
            _logger.LogError(110, "Update User Failed", updateResults);
            return new ApiFailedResult<UserResponse>(updateResults.Errors.Select(e => new ValidationError() { Message = e }), HttpStatusCode.BadRequest);
        }

        var createdUser = user.Adapt<UserResponse>();
        return new ApiSuccessResult<UserResponse>(createdUser);
    }
}