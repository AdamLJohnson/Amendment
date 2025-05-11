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

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, IApiResult>
{
    private readonly ILogger<CreateUserCommandHandler> _logger;
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IPasswordHashService _passwordHashService;

    public CreateUserCommandHandler(ILogger<CreateUserCommandHandler> logger, IUserService userService, IRoleService roleService, IPasswordHashService passwordHashService)
    {
        _logger = logger;
        _userService = userService;
        _roleService = roleService;
        _passwordHashService = passwordHashService;
    }

    public async Task<IApiResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User()
        {
            Username = request.Username,
            Name = request.Name,
            Email = request.Email,
            Password = _passwordHashService.HashPassword(request.Password),
            EnteredBy = request.SavingUserId,
            LastUpdatedBy = request.SavingUserId,
            LastUpdated = DateTime.UtcNow,
            EnteredDate = DateTime.UtcNow,
            RequirePasswordChange = request.RequirePasswordChange
        };

        var roles = new List<Role>();
        foreach (var requestRole in request.Roles)
        {
            var role = await _roleService.GetAsync(requestRole);
            roles.Add(role);
        }
        user.Roles = roles;

        var createResult = await _userService.CreateAsync(user, request.SavingUserId);
        if (!createResult.Succeeded)
        {
            _logger.LogError(100, "Create User Failed {createdResult}", createResult);
            return new ApiFailedResult<UserResponse>(createResult.Errors.Select(e => new ValidationError(){Message = e}), HttpStatusCode.BadRequest);
        }

        var createdUser = user.Adapt<UserResponse>();
        return new ApiSuccessResult<UserResponse>(createdUser) { StatusCode = HttpStatusCode.Created };
    }
}