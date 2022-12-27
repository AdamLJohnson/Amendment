using System.Net;
using Amendment.Server.Mediator.Commands.AmendmentBodyCommands;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.AmendmentBodyHandlers;

public sealed class CreateAmendmentBodyCommandHandler : IRequestHandler<CreateAmendmentBodyCommand, IApiResult<AmendmentBodyResponse>>
{
    private readonly ILogger<CreateAmendmentBodyCommandHandler> _logger;
    private readonly IAmendmentBodyService _amendmentBodyService;

    public CreateAmendmentBodyCommandHandler(ILogger<CreateAmendmentBodyCommandHandler> logger, IAmendmentBodyService amendmentBodyService)
    {
        _logger = logger;
        _amendmentBodyService = amendmentBodyService;
    }

    public async Task<IApiResult<AmendmentBodyResponse>> Handle(CreateAmendmentBodyCommand request, CancellationToken cancellationToken)
    {
        var amendmentBody = request.Adapt<Model.DataModel.AmendmentBody>();
        amendmentBody.EnteredBy = request.SavingUserId;
        amendmentBody.LastUpdatedBy = request.SavingUserId;
        amendmentBody.EnteredDate = DateTime.UtcNow;
        amendmentBody.LastUpdated = DateTime.UtcNow;

        var result = await _amendmentBodyService.CreateAsync(amendmentBody, request.SavingUserId);
        if (!result.Succeeded)
        {
            _logger.LogError(300, "Create Amendment Body Failed", result);
            return new ApiFailedResult<AmendmentBodyResponse>(result.Errors.Select(e => new ValidationError() { Message = e }), HttpStatusCode.BadRequest);
        }

        return new ApiSuccessResult<AmendmentBodyResponse>(amendmentBody.Adapt<AmendmentBodyResponse>()) { StatusCode = HttpStatusCode.Created };
    }
}