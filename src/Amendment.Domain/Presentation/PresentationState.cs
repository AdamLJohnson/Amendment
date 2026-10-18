using System.Collections.Immutable;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;

namespace Amendment.Domain.Presentation;

/// <summary>Per-language screen state. Replaces legacy <c>AmendmentBody.IsLive</c> / <c>AmendmentBody.Page</c>.</summary>
public sealed record LanguageView(bool Visible, int Page);

/// <summary>A requested change for one language; null fields are left unchanged.</summary>
public sealed record LanguageChange(LanguageCode Language, bool? Visible = null, int? Page = null);

/// <summary>
/// The single <c>PRESENTATION#MAIN</c> state (decision D3: exactly one presented amendment).
/// All operations are pure; every effective change increments <see cref="Version"/> once.
/// </summary>
public sealed record PresentationState
{
    public string? ActiveAmendmentId { get; init; }
    public ImmutableDictionary<LanguageCode, LanguageView> Languages { get; init; } = ImmutableDictionary<LanguageCode, LanguageView>.Empty;
    public long Version { get; init; } = Versioning.InitialVersion;
    public AuditStamp? Updated { get; init; }

    public static PresentationState Empty { get; } = new();

    public bool IsVisible(string amendmentId, LanguageCode language) =>
        ActiveAmendmentId == amendmentId && Languages.TryGetValue(language, out var view) && view.Visible;

    /// <summary>
    /// Makes <paramref name="amendment"/> the presented amendment. Switching replaces any previous amendment.
    /// Every existing body starts hidden on page 0. Archived amendments cannot be presented.
    /// </summary>
    public DomainResult<PresentationState> Present(AmendmentRecord amendment, IEnumerable<AmendmentBodyRecord> bodies, long expectedVersion, AuditStamp stamp)
    {
        if (Versioning.Check("Presentation", Version, expectedVersion) is { } conflict)
            return conflict;
        if (amendment.IsArchived)
            return DomainError.Precondition("Archived amendments cannot be presented.");

        var languages = bodies
            .Where(b => b.AmendmentId == amendment.Id)
            .ToImmutableDictionary(b => b.Language, _ => new LanguageView(false, 0));
        return Next(amendment.Id, languages, stamp);
    }

    /// <summary>Legacy "Clear screens": nothing presented. Idempotent (no version bump when already empty).</summary>
    public DomainResult<PresentationState> Clear(long expectedVersion, AuditStamp stamp)
    {
        if (Versioning.Check("Presentation", Version, expectedVersion) is { } conflict)
            return conflict;
        if (ActiveAmendmentId is null && Languages.IsEmpty)
            return DomainResult<PresentationState>.Success(this);
        return Next(null, ImmutableDictionary<LanguageCode, LanguageView>.Empty, stamp);
    }

    /// <summary>
    /// Applies visibility/page changes for one or more languages in one version (legacy hub batches).
    /// Pages are normalized with the legacy reset-to-0 rule against the body's current slide count.
    /// </summary>
    public DomainResult<PresentationState> SetLanguages(
        IReadOnlyCollection<LanguageChange> changes, IReadOnlyDictionary<LanguageCode, int> pageCounts, long expectedVersion, AuditStamp stamp)
    {
        if (Versioning.Check("Presentation", Version, expectedVersion) is { } conflict)
            return conflict;
        if (ActiveAmendmentId is null)
            return DomainError.Precondition("No amendment is presented.");
        if (changes.Count == 0)
            return DomainError.Validation("changes", "At least one language change is required.");

        var builder = Languages.ToBuilder();
        foreach (var change in changes)
        {
            if (!builder.TryGetValue(change.Language, out var current) || !pageCounts.TryGetValue(change.Language, out var count))
                return DomainError.NotFound($"The presented amendment has no '{change.Language}' body.");

            builder[change.Language] = new LanguageView(
                change.Visible ?? current.Visible,
                SlideDeck.NormalizePage(change.Page ?? current.Page, count));
        }

        var updated = builder.ToImmutable();
        return SameViews(updated) ? DomainResult<PresentationState>.Success(this) : Next(ActiveAmendmentId, updated, stamp);
    }

    /// <summary>
    /// After a body edit/creation on the presented amendment: add a hidden entry for a new body, or reset the page
    /// when the slide count shrank. Returns <c>this</c> when nothing changes.
    /// </summary>
    public PresentationState ReconcileBody(AmendmentBodyRecord body, AuditStamp stamp)
    {
        if (body.AmendmentId != ActiveAmendmentId)
            return this;
        if (!Languages.TryGetValue(body.Language, out var view))
            return Next(ActiveAmendmentId, Languages.SetItem(body.Language, new LanguageView(false, 0)), stamp).Value;

        var page = SlideDeck.NormalizePage(view.Page, body.PageCount);
        return page == view.Page ? this : Next(ActiveAmendmentId, Languages.SetItem(body.Language, view with { Page = page }), stamp).Value;
    }

    /// <summary>After a body is deleted from the presented amendment, its language is removed.</summary>
    public PresentationState RemoveBody(string amendmentId, LanguageCode language, AuditStamp stamp) =>
        amendmentId == ActiveAmendmentId && Languages.ContainsKey(language)
            ? Next(ActiveAmendmentId, Languages.Remove(language), stamp).Value
            : this;

    private bool SameViews(ImmutableDictionary<LanguageCode, LanguageView> other) =>
        other.Count == Languages.Count && other.All(kv => Languages.TryGetValue(kv.Key, out var v) && v == kv.Value);

    private DomainResult<PresentationState> Next(string? activeId, ImmutableDictionary<LanguageCode, LanguageView> languages, AuditStamp stamp) =>
        DomainResult<PresentationState>.Success(this with
        {
            ActiveAmendmentId = activeId,
            Languages = languages,
            Version = Versioning.Next(Version),
            Updated = stamp
        });
}
