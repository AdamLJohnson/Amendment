using Amendment.Shared;
using MediatR;

namespace Amendment.Server.Mediator.Commands.UserCommands;

public sealed record DeleteUserCommand (int Id, int SavingUserId) : IRequest<IApiResult>;