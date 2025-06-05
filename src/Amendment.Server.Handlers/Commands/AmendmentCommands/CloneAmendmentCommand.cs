using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Commands.AmendmentCommands;

public sealed class CloneAmendmentCommand : IRequest<IApiResult>
{
    public int SavingUserId { get; set; }
    public int SourceAmendmentId { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Motion { get; set; }
    public string? Source { get; set; }
    public string? LegisId { get; set; }
}
