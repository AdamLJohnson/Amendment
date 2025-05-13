using System.Net;
using Amendment.Model.Enums;
using Amendment.Repository;
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
    private readonly IAmendmentRepository _amendmentRepository;

    public UpdateAmendmentBodyCommandHandler(
        ILogger<UpdateAmendmentBodyCommandHandler> logger,
        IAmendmentBodyService amendmentBodyService,
        IAmendmentRepository amendmentRepository)
    {
        _logger = logger;
        _amendmentBodyService = amendmentBodyService;
        _amendmentRepository = amendmentRepository;
    }

    public async Task<IApiResult<AmendmentBodyResponse>> Handle(UpdateAmendmentBodyCommand request, CancellationToken cancellationToken)
    {
        // Get the current amendment body
        var amendmentBody = await _amendmentBodyService.GetAsync(request.Id);
        if (amendmentBody == null || amendmentBody.AmendId != request.AmendId)
            return new ApiFailedResult<AmendmentBodyResponse>(HttpStatusCode.NotFound);

        // Get the amendment to check if this is the primary language
        var amendment = await _amendmentRepository.GetByIdAsync(amendmentBody.AmendId);
        bool isPrimaryLanguage = amendment.PrimaryLanguageId == amendmentBody.LanguageId;

        // Store the original body text to check if it changed
        string originalBodyText = amendmentBody.AmendBody;

        // Update the amendment body with the new values
        request.Adapt(amendmentBody);

        // Check if the body text has changed
        bool bodyTextChanged = !string.Equals(originalBodyText, amendmentBody.AmendBody);

        // Update the current amendment body
        var result = await _amendmentBodyService.UpdateAsync(amendmentBody, request.SavingUserId);
        if (!result.Succeeded)
        {
            _logger.LogError(310, "Update Amendment Body Failed {result}", result);
            return new ApiFailedResult<AmendmentBodyResponse>(result.Errors.Select(e => new ValidationError() { Message = e }), HttpStatusCode.BadRequest);
        }

        // If this is the primary language and the body text changed, set all non-primary language versions to Draft
        if (isPrimaryLanguage && bodyTextChanged)
        {
            // Get all non-primary language bodies for this amendment
            var nonPrimaryBodies = await _amendmentBodyService.GetByAmentmentId(amendmentBody.AmendId);
            foreach (var body in nonPrimaryBodies.Where(b => b.LanguageId != amendment.PrimaryLanguageId))
            {
                // Only update if not already in Draft or New status
                if (body.AmendStatus != AmendmentBodyStatus.Draft &&
                    body.AmendStatus != AmendmentBodyStatus.New)
                {
                    body.AmendStatus = AmendmentBodyStatus.Draft;
                    body.LastUpdatedBy = request.SavingUserId;
                    body.LastUpdated = DateTime.UtcNow;
                    await _amendmentBodyService.UpdateAsync(body, request.SavingUserId);
                }
            }
        }

        return new ApiSuccessResult<AmendmentBodyResponse>(amendmentBody.Adapt<AmendmentBodyResponse>());
    }
}