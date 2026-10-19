using Amendment.Domain;
using Amendment.Domain.Approval;
using Amendment.Domain.Authorization;
using Amendment.Domain.Model;
using Amendment.Infrastructure.DynamoDb;

namespace Amendment.Api.Lambda;

/// <summary>Conference approval (plan §2B, contracts.md §5, decision D5).</summary>
public sealed partial class AmendmentApi
{
    private async Task<ApiResponse> ApproveAmendmentAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (GraphQlShapes.ParseFailureMode(args.String("failureMode")) is not { } mode)
            return ApiResponse.Fail(DomainError.Validation("failureMode", "Unknown failure mode."));
        var operation = mode == ApprovalFailureMode.BestEffort ? Operation.ApproveAmendmentBestEffort : Operation.ApproveAmendment;
        if (RoleOperationMatrix.Authorize(principal, operation) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        if (FirstError(Required(args, "id", out var id), RequiredVersion(args, out var expected)) is { } invalid)
            return ApiResponse.Fail(invalid);

        var request = new ApprovalRequest(id, expected, args.Bool("cleanChildBodies") ?? true, mode);
        var stamp = Stamp(principal);
        return await WithRetryAsync(isLastAttempt => ApproveOnceAsync(principal, request, stamp, isLastAttempt, ct));
    }

    private async Task<ApiResponse> ApproveOnceAsync(Principal principal, ApprovalRequest request, AuditStamp stamp, bool isLastAttempt, CancellationToken ct)
    {
        var child = await store.GetAmendmentWithBodiesAsync(request.AmendmentId, ct);
        ParentSnapshot? parent = null;
        if (child?.Amendment.ParentAmendmentId is { } parentId)
        {
            var loaded = await store.GetAmendmentWithBodiesAsync(parentId, ct);
            parent = new ParentSnapshot(loaded?.Amendment, loaded?.Bodies ?? []);
        }
        var presentation = await store.GetPresentationAsync(ct);

        var planned = ConferenceApproval.Plan(principal, request, child?.Amendment, child?.Bodies ?? [], parent, presentation, stamp);
        if (!planned.IsSuccess)
            return ApiResponse.Fail(planned.Errors);
        var plan = planned.Value;
        if (!plan.IsTransition)
            return ApiResponse.Ok(new ApprovalResultDto(GraphQlShapes.ToDto(plan.Amendment, child!.Bodies), false, [], [], []));

        var reconciled = plan.AtomicBodyWrites.Aggregate(presentation, (state, write) => state.ReconcileBody(write.After, stamp));
        List<StoreWrite> writes = [StoreWrite.UpdateAmendment(plan.Amendment), .. plan.AtomicBodyWrites.Select(w => StoreWrite.UpdateBody(w.After))];
        if (!ReferenceEquals(reconciled, presentation))
            writes.Add(StoreWrite.SavePresentation(reconciled));

        var tx = await store.TransactAsync(writes, ct);
        if (Committed(tx, isLastAttempt) is { } failed)
            return failed;

        var warnings = plan.Warnings.ToList();
        var updatedParent = plan.FailureMode == ApprovalFailureMode.AllOrNothing
            ? plan.ParentBodyWrites.Select(w => w.After.Language.Value).ToList()
            : [];
        foreach (var write in plan.BestEffortBodyWrites)
        {
            var error = await WriteParentBodyBestEffortAsync(write, stamp, ct);
            if (error is null)
                updatedParent.Add(write.After.Language.Value);
            else
                warnings.Add($"The parent '{write.After.Language}' body was not updated: {error.Message}");
        }

        var childBodies = child!.Bodies
            .Select(b => plan.ChildBodyWrites.FirstOrDefault(w => w.Before.Language == b.Language)?.After ?? b)
            .ToList();
        return ApiResponse.Ok(new ApprovalResultDto(
            GraphQlShapes.ToDto(plan.Amendment, childBodies),
            true,
            plan.ChildBodyWrites.Select(w => w.After.Language.Value).ToList(),
            updatedParent,
            warnings));
    }

    /// <summary>BEST_EFFORT parent write (legacy semantics): one body at a time, failures become warnings.</summary>
    private async Task<DomainError?> WriteParentBodyBestEffortAsync(BodyWrite write, AuditStamp stamp, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var presentation = await store.GetPresentationAsync(ct);
            List<StoreWrite> writes = [StoreWrite.UpdateBody(write.After),
                .. PresentationWrites(presentation, presentation.ReconcileBody(write.After, stamp), write.After.AmendmentId)];
            var tx = await store.TransactAsync(writes, ct);
            if (tx.IsCommitted)
                return null;
            if (tx.FailedWrite?.TargetsPresentation != true)
                return tx.Error;
        }
        return DomainError.Conflict("The presentation kept changing.");
    }
}
