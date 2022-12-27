using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Queries.AmendmentBodyQueries
{
    public sealed record GetSingleAmendmentBodyQuery(int Id, int AmendmentId) : IRequest<IApiResult<AmendmentBodyResponse>>;
}
