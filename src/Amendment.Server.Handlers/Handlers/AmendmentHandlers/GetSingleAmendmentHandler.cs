using System.Net;
using Amendment.Server.Mediator.Queries.AmendmentQueries;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.AmendmentHandlers;

public sealed class GetSingleAmendmentHandler : IRequestHandler<GetSingleAmendmentQuery, IApiResult<AmendmentResponse>>
{
    private readonly ILogger<GetSingleAmendmentHandler> _logger;
    private readonly IAmendmentService _amendmentService;

    public GetSingleAmendmentHandler(ILogger<GetSingleAmendmentHandler> logger, IAmendmentService amendmentService)
    {
        _logger = logger;
        _amendmentService = amendmentService;
    }

    public async Task<IApiResult<AmendmentResponse>> Handle(GetSingleAmendmentQuery request, CancellationToken cancellationToken)
    {
        var result = await _amendmentService.GetAsync(request.Id);
        if (result == null)
            return new ApiFailedResult<AmendmentResponse>(HttpStatusCode.NotFound);

        return new ApiSuccessResult<AmendmentResponse>(result.Adapt<AmendmentResponse>());
    }
}