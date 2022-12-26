using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Queries.UserQueries;

public sealed class GetSingleUserQuery : IRequest<IApiResult<UserResponse>>
{
    public int Id { get; }

    public GetSingleUserQuery(int id)
    {
        Id = id;
    }
}