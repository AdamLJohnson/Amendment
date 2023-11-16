using System.Net;
using Amendment.Server.Mediator.Commands.AmendmentBodyCommands;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.AmendmentBodyHandlers;

public sealed class UpdateAmendmentBodyCommandHandler : IRequestHandler<UpdateAmendmentBodyCommand, IApiResult<AmendmentBodyResponse>>
{
    private readonly ILogger<UpdateAmendmentBodyCommandHandler> _logger;
    private readonly IAmendmentBodyService _amendmentBodyService;

    public UpdateAmendmentBodyCommandHandler(ILogger<UpdateAmendmentBodyCommandHandler> logger, IAmendmentBodyService amendmentBodyService)
    {
        _logger = logger;
        _amendmentBodyService = amendmentBodyService;
    }

    public async Task<IApiResult<AmendmentBodyResponse>> Handle(UpdateAmendmentBodyCommand request, CancellationToken cancellationToken)
    {
        var amendmentBody = await _amendmentBodyService.GetAsync(request.Id);
        if (amendmentBody == null || amendmentBody.AmendId != request.AmendId)
            return new ApiFailedResult<AmendmentBodyResponse>(HttpStatusCode.NotFound);

        request.Adapt(amendmentBody);

        var result = await _amendmentBodyService.UpdateAsync(amendmentBody, request.SavingUserId);
        if (!result.Succeeded)
        {
            _logger.LogError(310, "Update Amendment Body Failed {result}", result);
            return new ApiFailedResult<AmendmentBodyResponse>(result.Errors.Select(e => new ValidationError() { Message = e }), HttpStatusCode.BadRequest);
        }

        return new ApiSuccessResult<AmendmentBodyResponse>(amendmentBody.Adapt<AmendmentBodyResponse>());
    }
}