using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Commands.AmendmentCommands;

public sealed class UpdateAmendmentCommand : IRequest<IApiResult<AmendmentResponse>>
{
    public int SavingUserId { get; set; }
    public int Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Motion { get; set; }
    public string Source { get; set; }
    public string LegisId { get; set; }
    public int PrimaryLanguageId { get; set; }
    public bool IsLive { get; set; }
}