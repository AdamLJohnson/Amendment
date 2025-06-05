using Amendment.Model.DataModel;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Amendment.Server.Handlers.Handlers.AmendmentHandlers;

public class CloneAmendmentCommandHandler : IRequestHandler<CloneAmendmentCommand, IApiResult>
{
    private readonly IAmendmentService _amendmentService;
    private readonly IAmendmentBodyService _amendmentBodyService;
    private readonly ILogger<CloneAmendmentCommandHandler> _logger;

    public CloneAmendmentCommandHandler(
        IAmendmentService amendmentService,
        IAmendmentBodyService amendmentBodyService,
        ILogger<CloneAmendmentCommandHandler> logger)
    {
        _amendmentService = amendmentService;
        _amendmentBodyService = amendmentBodyService;
        _logger = logger;
    }

    public async Task<IApiResult> Handle(CloneAmendmentCommand request, CancellationToken cancellationToken)
    {
        // Get the source amendment with all its bodies
        var sourceAmendment = await _amendmentService.GetAsync(request.SourceAmendmentId);
        if (sourceAmendment == null)
        {
            return new ApiFailedResult<AmendmentResponse>(HttpStatusCode.NotFound);
        }

        // Get all amendment bodies for the source amendment
        var sourceAmendmentBodies = await _amendmentBodyService.GetByAmentmentId(request.SourceAmendmentId);

        // Create the cloned amendment
        var clonedAmendment = new Model.DataModel.Amendment
        {
            Title = request.Title ?? $"{sourceAmendment.Title} (Clone)",
            Author = request.Author ?? sourceAmendment.Author,
            Motion = request.Motion ?? sourceAmendment.Motion,
            Source = request.Source ?? sourceAmendment.Source,
            LegisId = request.LegisId ?? sourceAmendment.LegisId,
            PrimaryLanguageId = sourceAmendment.PrimaryLanguageId,
            ParentAmendmentId = sourceAmendment.Id,
            IsLive = false, // Cloned amendments start as not live
            IsArchived = false, // Cloned amendments start as not archived
            IsApproved = false, // Cloned amendments start as not approved
            EnteredBy = request.SavingUserId,
            EnteredDate = DateTime.UtcNow,
            LastUpdatedBy = request.SavingUserId,
            LastUpdated = DateTime.UtcNow
        };

        // Save the cloned amendment first
        var createResult = await _amendmentService.CreateAsync(clonedAmendment, request.SavingUserId);
        if (!createResult.Succeeded)
        {
            _logger.LogError(230, "Clone Amendment Failed {result}", createResult);
            return new ApiFailedResult<AmendmentResponse>(
                createResult.Errors.Select(e => new ValidationError() { Message = e }), 
                HttpStatusCode.BadRequest);
        }

        // Clone all amendment bodies
        if (sourceAmendmentBodies != null && sourceAmendmentBodies.Any())
        {
            foreach (var sourceBody in sourceAmendmentBodies)
            {
                var clonedBody = new AmendmentBody
                {
                    AmendId = clonedAmendment.Id,
                    LanguageId = sourceBody.LanguageId,
                    AmendBody = sourceBody.AmendBody,
                    AmendStatus = sourceBody.AmendStatus,
                    IsLive = false, // Cloned bodies start as not live
                    Page = sourceBody.Page,
                    EnteredBy = request.SavingUserId,
                    EnteredDate = DateTime.UtcNow,
                    LastUpdatedBy = request.SavingUserId,
                    LastUpdated = DateTime.UtcNow
                };

                var bodyCreateResult = await _amendmentBodyService.CreateAsync(clonedBody, request.SavingUserId);
                if (!bodyCreateResult.Succeeded)
                {
                    _logger.LogError(231, "Clone Amendment Body Failed {result}", bodyCreateResult);
                    // Continue with other bodies even if one fails
                }
            }
        }

        // Get the complete cloned amendment with bodies for response
        var completeClonedAmendment = await _amendmentService.GetAsync(clonedAmendment.Id);
        return new ApiSuccessResult<AmendmentResponse>(completeClonedAmendment.Adapt<AmendmentResponse>());
    }
}
