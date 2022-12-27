using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Server.Mediator.Queries.AmendmentBodyQueries;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.AmendmentBodyHandlers;

public sealed class GetAllAmendmentBodiesHandler : IRequestHandler<GetAllAmendmentBodiesQuery, IApiResult<List<AmendmentBodyResponse>>>
{
    private readonly ILogger<GetAllAmendmentBodiesHandler> _logger;
    private readonly IAmendmentBodyService _amendmentBodyService;

    public GetAllAmendmentBodiesHandler(ILogger<GetAllAmendmentBodiesHandler> logger, IAmendmentBodyService amendmentBodyService)
    {
        _logger = logger;
        _amendmentBodyService = amendmentBodyService;
    }

    public async Task<IApiResult<List<AmendmentBodyResponse>>> Handle(GetAllAmendmentBodiesQuery request, CancellationToken cancellationToken)
    {
        var results = await _amendmentBodyService.GetByAmentmentId(request.AmendmentId);
        return new ApiSuccessResult<List<AmendmentBodyResponse>>(results.Adapt<List<AmendmentBodyResponse>>());
    }
}