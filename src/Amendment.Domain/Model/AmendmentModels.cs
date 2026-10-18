using Amendment.Domain.Languages;

namespace Amendment.Domain.Model;

/// <summary>Who made a change and when. <c>By</c> is a stable user identifier (Cognito sub in the target design).</summary>
public sealed record AuditStamp(string By, DateTimeOffset At);

/// <summary>Mirrors legacy <c>AmendmentBodyStatus</c> values so migrated data keeps its meaning.</summary>
public enum BodyStatus
{
    New = 0,
    Draft = 1,
    UnderReview = 2,
    Ready = 3
}

/// <summary>
/// Amendment metadata (target item <c>AMENDMENT#&lt;id&gt;</c> / <c>META</c>).
/// Live state is owned by <see cref="Presentation.PresentationState"/>, not by this record.
/// </summary>
public sealed record AmendmentRecord
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public string? Motion { get; init; }
    public string? Source { get; init; }
    public string? LegisId { get; init; }
    public required LanguageCode PrimaryLanguage { get; init; }
    public bool IsArchived { get; init; }
    public bool IsApproved { get; init; }
    public string? ParentAmendmentId { get; init; }
    public long Version { get; init; } = Versioning.InitialVersion;
    public required AuditStamp Created { get; init; }
    public required AuditStamp Updated { get; init; }
}

/// <summary>One language body (target item <c>AMENDMENT#&lt;id&gt;</c> / <c>BODY#&lt;language&gt;</c>).</summary>
public sealed record AmendmentBodyRecord
{
    public required string AmendmentId { get; init; }
    public required LanguageCode Language { get; init; }
    public required string Markdown { get; init; }
    public BodyStatus Status { get; init; }
    public long Version { get; init; } = Versioning.InitialVersion;
    public required AuditStamp Created { get; init; }
    public required AuditStamp Updated { get; init; }

    public int PageCount => SlideDeck.PageCount(Markdown);
}

/// <summary>Key helpers for the single-table design (plan §4).</summary>
public static class ItemKeys
{
    public static string AmendmentPk(string amendmentId) => $"AMENDMENT#{amendmentId}";
    public const string MetaSk = "META";
    public static string BodySk(LanguageCode language) => $"BODY#{language.Value}";
    public const string PresentationPk = "PRESENTATION#MAIN";
    public const string TimerPk = "TIMER#MAIN";
    public const string StateSk = "STATE";
}
