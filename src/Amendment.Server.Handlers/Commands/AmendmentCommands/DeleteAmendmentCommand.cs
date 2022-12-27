using Amendment.Shared;
using MediatR;

namespace Amendment.Server.Mediator.Commands.AmendmentCommands;

public sealed record DeleteAmendmentCommand(int Id, int SavingUserId) : IRequest<IApiResult>;