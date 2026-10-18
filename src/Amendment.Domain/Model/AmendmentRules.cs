using Amendment.Domain.Languages;

namespace Amendment.Domain.Model;

/// <summary>Editable amendment metadata (create/update input).</summary>
public sealed record AmendmentInput(
    string? Title,
    string? Author,
    string? PrimaryLanguage,
    string? Motion = null,
    string? Source = null,
    string? LegisId = null,
    string? ParentAmendmentId = null);

/// <summary>Create/update/archive/clone rules for amendment metadata.</summary>
public sealed class AmendmentRules(SupportedLanguages languages)
{
    /// <summary>Legacy <c>AmendmentRequestValidator</c> rejects this exact title.</summary>
    public const string LegacyRejectedTitle = "not allowed";

    public DomainResult<AmendmentRecord> Create(string id, AmendmentInput input, AuditStamp stamp)
    {
        var errors = Validate(input, out var primary);
        if (errors.Count > 0)
            return DomainResult<AmendmentRecord>.Failure(errors);

        return DomainResult<AmendmentRecord>.Success(new AmendmentRecord
        {
            Id = id,
            Title = input.Title!.Trim(),
            Author = input.Author!.Trim(),
            Motion = input.Motion,
            Source = input.Source,
            LegisId = input.LegisId,
            PrimaryLanguage = primary,
            ParentAmendmentId = input.ParentAmendmentId,
            Created = stamp,
            Updated = stamp
        });
    }

    /// <summary>
    /// Metadata update. Approval (<c>IsApproved</c> false → true) is not possible here; it is the separate
    /// <see cref="Approval.ConferenceApproval"/> operation. Un-approving is allowed and has no side effects (legacy).
    /// </summary>
    public DomainResult<AmendmentRecord> Update(AmendmentRecord existing, AmendmentInput input, bool isApproved, long expectedVersion, AuditStamp stamp)
    {
        if (Versioning.Check($"Amendment {existing.Id}", existing.Version, expectedVersion) is { } conflict)
            return conflict;
        if (!existing.IsApproved && isApproved)
            return DomainError.Validation("isApproved", "Use the approveAmendment operation to approve an amendment.");

        var errors = Validate(input, out var primary);
        if (input.ParentAmendmentId == existing.Id)
            errors.Add(DomainError.Validation("parentAmendmentId", "An amendment cannot be its own parent."));
        if (errors.Count > 0)
            return DomainResult<AmendmentRecord>.Failure(errors);

        return DomainResult<AmendmentRecord>.Success(existing with
        {
            Title = input.Title!.Trim(),
            Author = input.Author!.Trim(),
            Motion = input.Motion,
            Source = input.Source,
            LegisId = input.LegisId,
            PrimaryLanguage = primary,
            ParentAmendmentId = input.ParentAmendmentId,
            IsApproved = isApproved,
            Version = Versioning.Next(existing.Version),
            Updated = stamp
        });
    }

    /// <summary>Archive/unarchive. The amendment currently on screen cannot be archived (matches the legacy UI gate).</summary>
    public DomainResult<AmendmentRecord> SetArchived(AmendmentRecord existing, bool archived, long expectedVersion, string? presentedAmendmentId, AuditStamp stamp)
    {
        if (Versioning.Check($"Amendment {existing.Id}", existing.Version, expectedVersion) is { } conflict)
            return conflict;
        if (archived && presentedAmendmentId == existing.Id)
            return DomainError.Precondition("The amendment is currently presented. Clear the presentation before archiving it.");
        if (existing.IsArchived == archived)
            return DomainResult<AmendmentRecord>.Success(existing);

        return DomainResult<AmendmentRecord>.Success(existing with
        {
            IsArchived = archived,
            Version = Versioning.Next(existing.Version),
            Updated = stamp
        });
    }

    /// <summary>Delete precondition. The presented amendment cannot be deleted.</summary>
    public static DomainError? CanDelete(AmendmentRecord existing, long expectedVersion, string? presentedAmendmentId) =>
        Versioning.Check($"Amendment {existing.Id}", existing.Version, expectedVersion)
        ?? (presentedAmendmentId == existing.Id
            ? DomainError.Precondition("The amendment is currently presented. Clear the presentation before deleting it.")
            : null);

    /// <summary>
    /// Legacy clone: copies metadata (with optional overrides) and every body; the copy is a child of the source and is
    /// not archived/approved. Unlike legacy, the clone is all-or-nothing (no per-body skip).
    /// </summary>
    public DomainResult<(AmendmentRecord Amendment, IReadOnlyList<AmendmentBodyRecord> Bodies)> Clone(
        AmendmentRecord source, IReadOnlyList<AmendmentBodyRecord> sourceBodies, string newId, AmendmentInput? overrides, AuditStamp stamp)
    {
        var input = new AmendmentInput(
            overrides?.Title ?? source.Title,
            overrides?.Author ?? source.Author,
            overrides?.PrimaryLanguage ?? source.PrimaryLanguage.Value,
            overrides?.Motion ?? source.Motion,
            overrides?.Source ?? source.Source,
            overrides?.LegisId ?? source.LegisId,
            source.Id);

        var created = Create(newId, input, stamp);
        if (!created.IsSuccess)
            return DomainResult<(AmendmentRecord, IReadOnlyList<AmendmentBodyRecord>)>.Failure(created.Errors);

        var bodies = sourceBodies
            .Select(b => b with { AmendmentId = newId, Version = Versioning.InitialVersion, Created = stamp, Updated = stamp })
            .ToList();
        return DomainResult<(AmendmentRecord, IReadOnlyList<AmendmentBodyRecord>)>.Success((created.Value, bodies));
    }

    private List<DomainError> Validate(AmendmentInput input, out LanguageCode primary)
    {
        var errors = new List<DomainError>();
        if (string.IsNullOrWhiteSpace(input.Title))
            errors.Add(DomainError.Validation("title", "Title is required."));
        else if (input.Title == LegacyRejectedTitle)
            errors.Add(DomainError.Validation("title", $"Title cannot be '{LegacyRejectedTitle}'."));
        if (string.IsNullOrWhiteSpace(input.Author))
            errors.Add(DomainError.Validation("author", "Author is required."));

        var language = languages.Resolve(input.PrimaryLanguage, "primaryLanguage");
        primary = language.IsSuccess ? language.Value : default;
        if (!language.IsSuccess)
            errors.AddRange(language.Errors);
        return errors;
    }
}
