using System.Net;
using Amendment.Server.Mediator.Commands.AmendmentBodyCommands;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.AmendmentBodyHandlers;

public sealed class DeleteAmendmentBodyCommandHandler : IRequestHandler<DeleteAmendmentBodyCommand, IApiResult>
{
    private readonly ILogger<DeleteAmendmentBodyCommandHandler> _logger;
    private readonly IAmendmentBodyService _amendmentBodyService;

    public DeleteAmendmentBodyCommandHandler(ILogger<DeleteAmendmentBodyCommandHandler> logger, IAmendmentBodyService amendmentBodyService)
    {
        _logger = logger;
        _amendmentBodyService = amendmentBodyService;
    }

    public async Task<IApiResult> Handle(DeleteAmendmentBodyCommand request, CancellationToken cancellationToken)
    {
        var amendmentBody = await _amendmentBodyService.GetAsync(request.Id);
        if (amendmentBody == null || amendmentBody.AmendId != request.AmendmentId)
            return new ApiFailedResult<AmendmentBodyResponse>(HttpStatusCode.NotFound);

        var result = await _amendmentBodyService.DeleteAsync(amendmentBody, request.SavingUserId);
        if (!result.Succeeded)
        {
            _logger.LogError(220, "Delete AmendmentBody Failed", result);
            return new ApiCommandFailedResult { StatusCode = HttpStatusCode.InternalServerError };
        }

        return new ApiCommandSuccessResult();
    }
}