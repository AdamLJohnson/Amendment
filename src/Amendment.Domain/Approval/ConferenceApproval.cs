using Amendment.Domain.Authorization;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using Amendment.Domain.Presentation;

namespace Amendment.Domain.Approval;

/// <summary>
/// <see cref="AllOrNothing"/> (default, decision D5): any parent problem fails the whole approval and nothing is written.
/// <see cref="BestEffort"/> (Admin only): legacy behavior; parent problems become warnings and approval still succeeds.
/// </summary>
public enum ApprovalFailureMode
{
    AllOrNothing,
    BestEffort
}

public sealed record ApprovalRequest(
    string AmendmentId,
    long ExpectedVersion,
    bool CleanChildBodies = true,
    ApprovalFailureMode FailureMode = ApprovalFailureMode.AllOrNothing);

/// <summary>Parent as loaded by the caller. <see cref="Amendment"/> null means the parent id did not resolve.</summary>
public sealed record ParentSnapshot(AmendmentRecord? Amendment, IReadOnlyList<AmendmentBodyRecord> Bodies);

/// <summary>A conditional body write: expected version is <c>Before.Version</c>.</summary>
public sealed record BodyWrite(AmendmentBodyRecord Before, AmendmentBodyRecord After);

/// <summary>
/// Result of planning an approval. The executor commits <see cref="AtomicBodyWrites"/> plus <see cref="Amendment"/> in one
/// transaction, then (BestEffort only) attempts <see cref="BestEffortBodyWrites"/> and reports failures as warnings.
/// </summary>
public sealed record ApprovalPlan(
    bool IsTransition,
    AmendmentRecord Amendment,
    long ExpectedAmendmentVersion,
    IReadOnlyList<BodyWrite> ChildBodyWrites,
    IReadOnlyList<BodyWrite> ParentBodyWrites,
    ApprovalFailureMode FailureMode,
    IReadOnlyList<LanguageCode> ProjectorRefreshLanguages,
    IReadOnlyList<string> Warnings)
{
    public IReadOnlyList<BodyWrite> AtomicBodyWrites =>
        FailureMode == ApprovalFailureMode.AllOrNothing ? [.. ChildBodyWrites, .. ParentBodyWrites] : ChildBodyWrites;

    public IReadOnlyList<BodyWrite> BestEffortBodyWrites =>
        FailureMode == ApprovalFailureMode.BestEffort ? ParentBodyWrites : [];
}

/// <summary>
/// Conference approval of a child amendment (plan §2B). Pure planning; no I/O.
/// Legacy parity: only false → true triggers; child bodies are cleaned only when requested; matching-language parent
/// bodies are always replaced by the cleaned child text (when the child was cleaned first, the parent receives
/// <c>Cleanup(cleanedChild)</c>, as in legacy); unchanged parent bodies are not written; unmatched languages are skipped.
/// </summary>
public static class ConferenceApproval
{
    public static DomainResult<ApprovalPlan> Plan(
        Principal principal,
        ApprovalRequest request,
        AmendmentRecord? child,
        IReadOnlyList<AmendmentBodyRecord> childBodies,
        ParentSnapshot? parent,
        PresentationState presentation,
        AuditStamp stamp)
    {
        var operation = request.FailureMode == ApprovalFailureMode.BestEffort
            ? Operation.ApproveAmendmentBestEffort
            : Operation.ApproveAmendment;
        if (RoleOperationMatrix.Authorize(principal, operation) is { } forbidden)
            return forbidden;
        if (child is null || child.Id != request.AmendmentId)
            return DomainError.NotFound($"Amendment {request.AmendmentId} not found.");

        // Already approved: a retried request is a safe no-op and never reruns cleanup.
        if (child.IsApproved)
            return DomainResult<ApprovalPlan>.Success(new ApprovalPlan(false, child, child.Version, [], [], request.FailureMode, [], []));

        if (Versioning.Check($"Amendment {child.Id}", child.Version, request.ExpectedVersion) is { } conflict)
            return conflict;

        var errors = new List<DomainError>();
        var warnings = new List<string>();
        var childWrites = new List<BodyWrite>();
        var sourceText = new Dictionary<LanguageCode, string>();

        foreach (var body in childBodies.Where(b => b.AmendmentId == child.Id))
        {
            sourceText[body.Language] = body.Markdown;
            if (!request.CleanChildBodies)
                continue;

            var cleaned = AmendmentTextCleanup.Cleanup(body.Markdown);
            if (cleaned == body.Markdown)
                continue;
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                errors.Add(DomainError.Validation($"bodies.{body.Language}", $"Cleaning the '{body.Language}' body would leave it empty."));
                continue;
            }
            childWrites.Add(new BodyWrite(body, Updated(body, cleaned, stamp)));
            sourceText[body.Language] = cleaned;
        }

        var parentWrites = new List<BodyWrite>();
        var refresh = new List<LanguageCode>();
        if (child.ParentAmendmentId is { } parentId && sourceText.Count > 0)
        {
            var problems = new List<DomainError>();
            if (parent?.Amendment is null || parent.Amendment.Id != parentId)
            {
                problems.Add(DomainError.Precondition($"Parent amendment {parentId} not found."));
            }
            else
            {
                foreach (var parentBody in parent.Bodies.Where(b => b.AmendmentId == parentId))
                {
                    if (!sourceText.TryGetValue(parentBody.Language, out var text))
                        continue;
                    var cleaned = AmendmentTextCleanup.Cleanup(text);
                    if (cleaned == parentBody.Markdown)
                        continue;
                    if (string.IsNullOrWhiteSpace(cleaned))
                    {
                        problems.Add(DomainError.Validation($"parentBodies.{parentBody.Language}", $"The parent '{parentBody.Language}' body would become empty."));
                        continue;
                    }
                    parentWrites.Add(new BodyWrite(parentBody, Updated(parentBody, cleaned, stamp)));
                    if (presentation.IsVisible(parentId, parentBody.Language))
                        refresh.Add(parentBody.Language);
                }
            }

            if (request.FailureMode == ApprovalFailureMode.AllOrNothing)
                errors.AddRange(problems);
            else
                warnings.AddRange(problems.Select(p => p.Message));
        }

        if (errors.Count > 0)
            return DomainResult<ApprovalPlan>.Failure(errors);

        var approved = child with { IsApproved = true, Version = Versioning.Next(child.Version), Updated = stamp };
        return DomainResult<ApprovalPlan>.Success(new ApprovalPlan(
            true, approved, child.Version, childWrites, parentWrites, request.FailureMode, refresh, warnings));
    }

    private static AmendmentBodyRecord Updated(AmendmentBodyRecord body, string markdown, AuditStamp stamp) =>
        body with { Markdown = markdown, Version = Versioning.Next(body.Version), Updated = stamp };
}
