using System.Net;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.AmendmentHandlers;

public sealed class CreateAmendmentCommandHandler : IRequestHandler<CreateAmendmentCommand, IApiResult<AmendmentResponse>>
{
    private readonly ILogger<CreateAmendmentCommandHandler> _logger;
    private readonly IAmendmentService _amendmentService;

    public CreateAmendmentCommandHandler(ILogger<CreateAmendmentCommandHandler> logger, IAmendmentService amendmentService)
    {
        _logger = logger;
        _amendmentService = amendmentService;
    }

    public async Task<IApiResult<AmendmentResponse>> Handle(CreateAmendmentCommand request, CancellationToken cancellationToken)
    {
        var amendment = request.Adapt<Model.DataModel.Amendment>();
        amendment.EnteredBy = request.SavingUserId;
        amendment.LastUpdatedBy = request.SavingUserId;
        amendment.EnteredDate = DateTime.UtcNow;
        amendment.LastUpdated = DateTime.UtcNow;

        var result = await _amendmentService.CreateAsync(amendment, request.SavingUserId);
        if (!result.Succeeded)
        {
            _logger.LogError(200, "Create Amendment Failed", result);
            return new ApiFailedResult<AmendmentResponse>(result.Errors.Select(e => new ValidationError() { Message = e }), HttpStatusCode.BadRequest);
        }
        
        return new ApiSuccessResult<AmendmentResponse>(amendment.Adapt<AmendmentResponse>()) { StatusCode = HttpStatusCode.Created };
    }
}