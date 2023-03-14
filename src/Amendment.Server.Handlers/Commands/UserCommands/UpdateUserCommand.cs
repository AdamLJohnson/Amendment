using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Commands.UserCommands;

public sealed class UpdateUserCommand : IRequest<IApiResult>
{
    public int SavingUserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public int[] Roles { get; set; } = Array.Empty<int>();
    public string? Password { get; set; }
    public int Id { get; set; }
}