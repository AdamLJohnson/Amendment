using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Queries.AmendmentQueries;

public sealed class GetLiveAmendmentQuery : IRequest<IApiResult<AmendmentFullBodyResponse>>
{
}