using System.Net;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.AmendmentHandlers;

public sealed class DeleteAmendmentCommandHandler : IRequestHandler<DeleteAmendmentCommand, IApiResult>
{
    private readonly ILogger<DeleteAmendmentCommandHandler> _logger;
    private readonly IAmendmentService _amendmentService;

    public DeleteAmendmentCommandHandler(ILogger<DeleteAmendmentCommandHandler> logger, IAmendmentService amendmentService)
    {
        _logger = logger;
        _amendmentService = amendmentService;
    }

    public async Task<IApiResult> Handle(DeleteAmendmentCommand request, CancellationToken cancellationToken)
    {
        var amendment = await _amendmentService.GetAsync(request.Id);
        if (amendment == null)
            return new ApiFailedResult<AmendmentResponse>(HttpStatusCode.NotFound);

        var result = await _amendmentService.DeleteAsync(amendment, request.SavingUserId);
        if (!result.Succeeded)
        {
            _logger.LogError(220, "Delete Amendment Failed", result);
            return new ApiCommandFailedResult { StatusCode = HttpStatusCode.InternalServerError };
        }

        return new ApiCommandSuccessResult();
    }
}