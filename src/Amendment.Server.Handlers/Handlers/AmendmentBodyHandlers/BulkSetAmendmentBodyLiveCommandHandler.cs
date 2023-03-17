using System.Net;
using Amendment.Model.Infrastructure;
using Amendment.Repository;
using Amendment.Server.Mediator.Commands.AmendmentBodyCommands;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.AmendmentBodyHandlers;

public sealed class BulkSetAmendmentBodyLiveCommandHandler : IRequestHandler<BulkSetAmendmentBodyLiveCommand, IApiResult>
{
    private readonly IAmendmentBodyService _service;

    public BulkSetAmendmentBodyLiveCommandHandler(IAmendmentBodyService service)
    {
        _service = service;
    }

    public async Task<IApiResult> Handle(BulkSetAmendmentBodyLiveCommand request, CancellationToken cancellationToken)
    {
        var input = request.BodyLiveCommands.Commands.Select(x => new SetIsLive { Id = x.Id, IsLive = x.IsLive }).ToArray();
        var results = await _service.SetIsLiveForManyAsync(input);
        return new ApiSuccessResult<List<AmendmentBodyResponse>>(results.Adapt<List<AmendmentBodyResponse>>());
    }
}

public sealed class BulkSetAmendmentBodyPageCommandHandler : IRequestHandler<BulkSetAmendmentBodyPageCommand, IApiResult>
{
    private readonly IAmendmentBodyService _service;

    public BulkSetAmendmentBodyPageCommandHandler(IAmendmentBodyService service)
    {
        _service = service;
    }

    public async Task<IApiResult> Handle(BulkSetAmendmentBodyPageCommand request, CancellationToken cancellationToken)
    {
        var input = request.BodyPageCommands.Commands.Select(x => new SetPage { Id = x.Id, Page = x.Page }).ToArray();
        var results = await _service.SetPageForManyAsync(input);
        return new ApiSuccessResult<List<AmendmentBodyResponse>>(results.Adapt<List<AmendmentBodyResponse>>());
    }
}