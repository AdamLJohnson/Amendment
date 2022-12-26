using System.Net;
using Amendment.Server.Mediator.Queries.UserQueries;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.UserHandlers;

public sealed class GetSingleUserHandler : IRequestHandler<GetSingleUserQuery, IApiResult<UserResponse>>
{
    private readonly ILogger<GetSingleUserHandler> _logger;
    private readonly IUserService _userService;

    public GetSingleUserHandler(ILogger<GetSingleUserHandler> logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    public async Task<IApiResult<UserResponse>> Handle(GetSingleUserQuery request, CancellationToken cancellationToken)
    {
        var results = await _userService.GetAsync(request.Id);
        if (results is null)
            return new ApiFailedResult<UserResponse>(HttpStatusCode.NotFound);

        var output = results.Adapt<UserResponse>();
        return new ApiSuccessResult<UserResponse>(output);
    }
}