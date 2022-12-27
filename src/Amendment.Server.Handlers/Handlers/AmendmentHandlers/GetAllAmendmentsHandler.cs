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
using Amendment.Model.ViewModel.AmendmentBody;

namespace Amendment.Server.Mediator.Handlers.AmendmentHandlers;

public sealed class GetAllAmendmentsHandler : IRequestHandler<GetAllAmendmentsQuery, IApiResult<List<AmendmentResponse>>>
{
    private readonly ILogger<GetAllAmendmentsHandler> _logger;
    private readonly IAmendmentService _amendmentService;

    public GetAllAmendmentsHandler(ILogger<GetAllAmendmentsHandler> logger, IAmendmentService amendmentService)
    {
        _logger = logger;
        _amendmentService = amendmentService;
    }

    public async Task<IApiResult<List<AmendmentResponse>>> Handle(GetAllAmendmentsQuery request, CancellationToken cancellationToken)
    {
        var results = await _amendmentService.GetAllAsync();
        return new ApiSuccessResult<List<AmendmentResponse>>(results.Adapt<List<AmendmentResponse>>());
    }
}