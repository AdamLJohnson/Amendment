using System.Net;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Service;
using Amendment.Service.Infrastructure;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amendment.Server.Mediator.Handlers.AmendmentHandlers;

public sealed class UpdateAmendmentCommandHandler : IRequestHandler<UpdateAmendmentCommand, IApiResult>
{
    private readonly ILogger<UpdateAmendmentCommandHandler> _logger;
    private readonly IAmendmentService _amendmentService;
    private readonly IAmendmentCleanupService _amendmentCleanupService;
    private readonly IAmendmentBodyService _amendmentBodyService;
    private readonly IScreenControlService _screenControlService;

    public UpdateAmendmentCommandHandler(
        ILogger<UpdateAmendmentCommandHandler> logger,
        IAmendmentService amendmentService,
        IAmendmentCleanupService amendmentCleanupService,
        IAmendmentBodyService amendmentBodyService,
        IScreenControlService screenControlService)
    {
        _logger = logger;
        _amendmentService = amendmentService;
        _amendmentCleanupService = amendmentCleanupService;
        _amendmentBodyService = amendmentBodyService;
        _screenControlService = screenControlService;
    }

    public async Task<IApiResult> Handle(UpdateAmendmentCommand request, CancellationToken cancellationToken)
    {
        var amendment = await _amendmentService.GetAsync(request.Id);
        if (amendment == null)
            return new ApiFailedResult<AmendmentResponse>(HttpStatusCode.NotFound);

        // Store the original approval status to check if it changed
        bool wasApproved = amendment.IsApproved;

        request.Adapt(amendment);

        // Check if the amendment is being approved
        bool isBeingApproved = !wasApproved && amendment.IsApproved;

        if (isBeingApproved)
        {
            // Clean up all amendment bodies when the amendment is approved
            var amendmentBodies = await _amendmentBodyService.GetByAmentmentId(amendment.Id);

            if (amendmentBodies != null)
            {
                var bodiesToUpdate = new List<Model.DataModel.AmendmentBody>();

                // Only update current amendment bodies if requested
                if (request.UpdateCurrentAmendmentBodies)
                {
                    foreach (var body in amendmentBodies)
                    {
                        var originalText = body.AmendBody;
                        var cleanedText = _amendmentCleanupService.CleanupAmendmentText(originalText);

                        if (!string.Equals(originalText, cleanedText))
                        {
                            body.AmendBody = cleanedText;
                            body.LastUpdatedBy = request.SavingUserId;
                            body.LastUpdated = DateTime.UtcNow;
                            bodiesToUpdate.Add(body);
                        }
                    }

                    // Update all bodies that need cleanup to trigger SignalR notifications
                    foreach (var body in bodiesToUpdate)
                    {
                        await _amendmentBodyService.UpdateAsync(body, request.SavingUserId);
                    }
                }

                // If this amendment has a parent, update the parent's bodies with cleaned text
                if (amendment.ParentAmendmentId.HasValue)
                {
                    await UpdateParentAmendmentBodies(amendment.ParentAmendmentId.Value, amendmentBodies, request.SavingUserId);
                }
            }
        }

        var result = await _amendmentService.UpdateAsync(amendment, request.SavingUserId);
        if (!result.Succeeded)
        {
            _logger.LogError(210, "Update Amendment Failed {result}", result);
            return new ApiFailedResult<AmendmentResponse>(result.Errors.Select(e => new ValidationError() { Message = e }), HttpStatusCode.BadRequest);
        }

        return new ApiSuccessResult<AmendmentResponse>(amendment.Adapt<AmendmentResponse>());
    }

    private async Task UpdateParentAmendmentBodies(int parentAmendmentId, IEnumerable<Model.DataModel.AmendmentBody> approvedBodies, int savingUserId)
    {
        try
        {
            // Get the parent amendment to check if it's live
            var parentAmendment = await _amendmentService.GetAsync(parentAmendmentId);
            if (parentAmendment == null)
            {
                _logger.LogWarning("Parent amendment {ParentAmendmentId} not found when trying to update bodies", parentAmendmentId);
                return;
            }

            // Get the parent amendment's bodies
            var parentBodies = await _amendmentBodyService.GetByAmentmentId(parentAmendmentId);

            if (parentBodies != null)
            {
                foreach (var approvedBody in approvedBodies)
                {
                    // Find the corresponding parent body by language
                    var parentBody = parentBodies.FirstOrDefault(pb => pb.LanguageId == approvedBody.LanguageId);

                    if (parentBody != null)
                    {
                        // Clean the approved body text and update the parent
                        var cleanedText = _amendmentCleanupService.CleanupAmendmentText(approvedBody.AmendBody);

                        if (!string.Equals(parentBody.AmendBody, cleanedText))
                        {
                            parentBody.AmendBody = cleanedText;
                            parentBody.LastUpdatedBy = savingUserId;
                            parentBody.LastUpdated = DateTime.UtcNow;

                            // Update the parent body - this will trigger proper SignalR notifications
                            await _amendmentBodyService.UpdateAsync(parentBody, savingUserId);

                            // Force screen update if the parent amendment is live
                            // This ensures screens get updated even if this parent amendment is not the currently active live amendment
                            if (parentAmendment.IsLive)
                            {
                                await _screenControlService.UpdateBodyAsync(parentBody, forceSend: true);
                            }

                            _logger.LogInformation("Updated parent amendment body {ParentBodyId} for amendment {ParentAmendmentId} with cleaned text from approved child amendment",
                                parentBody.Id, parentAmendmentId);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update parent amendment bodies for parent ID {ParentAmendmentId}", parentAmendmentId);
            // Don't throw - we don't want to fail the approval process if parent update fails
        }
    }
}