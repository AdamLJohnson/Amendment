using System.Net;
using Amendment.Server.Mediator.Queries.AmendmentBodyQueries;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.AmendmentBodyHandlers;

public sealed class GetSingleAmendmentBodyHandler : IRequestHandler<GetSingleAmendmentBodyQuery, IApiResult<AmendmentBodyResponse>>
{
    private readonly ILogger<GetSingleAmendmentBodyHandler> _logger;
    private readonly IAmendmentBodyService _amendmentBodyService;

    public GetSingleAmendmentBodyHandler(ILogger<GetSingleAmendmentBodyHandler> logger, IAmendmentBodyService amendmentBodyService)
    {
        _logger = logger;
        _amendmentBodyService = amendmentBodyService;
    }

    public async Task<IApiResult<AmendmentBodyResponse>> Handle(GetSingleAmendmentBodyQuery request, CancellationToken cancellationToken)
    {
        var result = await _amendmentBodyService.GetAsync(request.Id);
        if (result == null || result.AmendId != request.AmendmentId)
            return new ApiFailedResult<AmendmentBodyResponse>(HttpStatusCode.NotFound);

        return new ApiSuccessResult<AmendmentBodyResponse>(result.Adapt<AmendmentBodyResponse>());
    }
}