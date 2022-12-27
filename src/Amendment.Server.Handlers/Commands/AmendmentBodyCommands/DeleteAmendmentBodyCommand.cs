using Amendment.Shared;
using MediatR;

namespace Amendment.Server.Mediator.Commands.AmendmentBodyCommands;

public sealed record DeleteAmendmentBodyCommand(int Id, int AmendmentId, int SavingUserId) : IRequest<IApiResult>;