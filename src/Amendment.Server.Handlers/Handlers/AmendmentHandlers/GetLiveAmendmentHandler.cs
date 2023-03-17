using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Server.Mediator.Queries.AmendmentQueries;
using Amendment.Service;
using Amendment.Shared.Responses;
using Amendment.Shared;
using MediatR;
using Amendment.Server.Mediator.Handlers.UserHandlers;
using Mapster;
using Microsoft.Extensions.Logging;
using Amendment.Model.DataModel;

namespace Amendment.Server.Mediator.Handlers.AmendmentHandlers;

public sealed class GetLiveAmendmentHandler : IRequestHandler<GetLiveAmendmentQuery, IApiResult<AmendmentFullBodyResponse>>
{
    private readonly ILogger<GetLiveAmendmentHandler> _logger;
    private readonly IAmendmentService _amendmentService;

    public GetLiveAmendmentHandler(ILogger<GetLiveAmendmentHandler> logger, IAmendmentService amendmentService)
    {
        _logger = logger;
        _amendmentService = amendmentService;
    }

    public async Task<IApiResult<AmendmentFullBodyResponse>> Handle(GetLiveAmendmentQuery request, CancellationToken cancellationToken)
    {
        var results = await _amendmentService.GetLiveAsync();
        return new ApiSuccessResult<AmendmentFullBodyResponse>(results.Adapt<AmendmentFullBodyResponse>());
    }
}