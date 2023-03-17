using Amendment.Shared;
using Amendment.Shared.SignalRCommands;
using MediatR;

namespace Amendment.Server.Mediator.Commands.AmendmentBodyCommands;

public sealed record DeleteAmendmentBodyCommand(int Id, int AmendmentId, int SavingUserId) : IRequest<IApiResult>;
public sealed record BulkSetAmendmentBodyPageCommand(SetAmendmentBodyPageCommands BodyPageCommands) : IRequest<IApiResult>;
public sealed record BulkSetAmendmentBodyLiveCommand(SetAmendmentBodyLiveCommands BodyLiveCommands) : IRequest<IApiResult>;