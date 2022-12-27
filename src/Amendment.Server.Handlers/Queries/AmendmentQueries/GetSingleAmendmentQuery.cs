using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Queries.AmendmentQueries;

public sealed class GetSingleAmendmentQuery : IRequest<IApiResult<AmendmentResponse>>
{
    public int Id { get; }

    public GetSingleAmendmentQuery(int id)
    {
        Id = id;
    }
}