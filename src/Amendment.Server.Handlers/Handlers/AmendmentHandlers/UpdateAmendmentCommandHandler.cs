using System.Net;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.AmendmentHandlers;

public sealed class UpdateAmendmentCommandHandler : IRequestHandler<UpdateAmendmentCommand, IApiResult<AmendmentResponse>>
{
    private readonly ILogger<UpdateAmendmentCommandHandler> _logger;
    private readonly IAmendmentService _amendmentService;

    public UpdateAmendmentCommandHandler(ILogger<UpdateAmendmentCommandHandler> logger, IAmendmentService amendmentService)
    {
        _logger = logger;
        _amendmentService = amendmentService;
    }

    public async Task<IApiResult<AmendmentResponse>> Handle(UpdateAmendmentCommand request, CancellationToken cancellationToken)
    {
        var amendment = await _amendmentService.GetAsync(request.Id);
        if (amendment == null)
            return new ApiFailedResult<AmendmentResponse>(HttpStatusCode.NotFound);

        request.Adapt(amendment);

        var result = await _amendmentService.UpdateAsync(amendment, request.SavingUserId);
        if (!result.Succeeded)
        {
            _logger.LogError(210, "Update Amendment Failed", result);
            return new ApiFailedResult<AmendmentResponse>(result.Errors.Select(e => new ValidationError() { Message = e }), HttpStatusCode.BadRequest);
        }

        return new ApiSuccessResult<AmendmentResponse>(amendment.Adapt<AmendmentResponse>());
    }
}