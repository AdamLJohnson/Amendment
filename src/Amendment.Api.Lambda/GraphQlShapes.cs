using System.Text.Json;
using System.Text.Json.Serialization;
using Amendment.Domain.Approval;
using Amendment.Domain.Model;

namespace Amendment.Api.Lambda;

/// <summary>
/// Response objects in the shape of the GraphQL schema (<c>infra/Amendment.Cdk/Schema/amendment.graphql</c>).
/// Versions are serialized as decimal strings (decision D19).
/// </summary>
public sealed record AuditDto(string By, string At);

public sealed record AmendmentBodyDto(
    string AmendmentId, string Language, string Markdown, string Status, int PageCount,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] long Version,
    AuditDto Created, AuditDto Updated);

public sealed record AmendmentDto(
    string Id, string Title, string Author, string? Motion, string? Source, string? LegisId, string PrimaryLanguage,
    bool IsArchived, bool IsApproved, string? ParentAmendmentId,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] long Version,
    AuditDto Created, AuditDto Updated, IReadOnlyList<AmendmentBodyDto> Bodies);

public sealed record ApprovalResultDto(
    AmendmentDto Amendment, bool ApprovalApplied, IReadOnlyList<string> UpdatedChildLanguages,
    IReadOnlyList<string> UpdatedParentLanguages, IReadOnlyList<string> Warnings);

public static class GraphQlShapes
{
    public static AuditDto ToDto(AuditStamp stamp) =>
        new(stamp.By, stamp.At.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture));

    public static AmendmentBodyDto ToDto(AmendmentBodyRecord b) =>
        new(b.AmendmentId, b.Language.Value, b.Markdown, ToWire(b.Status), b.PageCount, b.Version, ToDto(b.Created), ToDto(b.Updated));

    public static AmendmentDto ToDto(AmendmentRecord a, IEnumerable<AmendmentBodyRecord> bodies) =>
        new(a.Id, a.Title, a.Author, a.Motion, a.Source, a.LegisId, a.PrimaryLanguage.Value, a.IsArchived, a.IsApproved,
            a.ParentAmendmentId, a.Version, ToDto(a.Created), ToDto(a.Updated),
            bodies.OrderBy(b => b.Language.Value, StringComparer.Ordinal).Select(ToDto).ToList());

    /// <summary>GraphQL <c>BodyStatus</c> enum value, e.g. <c>UNDER_REVIEW</c>.</summary>
    public static string ToWire(BodyStatus status) => status switch
    {
        BodyStatus.New => "NEW",
        BodyStatus.Draft => "DRAFT",
        BodyStatus.UnderReview => "UNDER_REVIEW",
        BodyStatus.Ready => "READY",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static BodyStatus? ParseStatus(string? value) => value switch
    {
        "NEW" => BodyStatus.New,
        "DRAFT" => BodyStatus.Draft,
        "UNDER_REVIEW" => BodyStatus.UnderReview,
        "READY" => BodyStatus.Ready,
        _ => null
    };

    public static ApprovalFailureMode? ParseFailureMode(string? value) => value switch
    {
        null or "ALL_OR_NOTHING" => ApprovalFailureMode.AllOrNothing,
        "BEST_EFFORT" => ApprovalFailureMode.BestEffort,
        _ => null
    };
}

/// <summary>Typed access to GraphQL arguments. AppSync has already enforced types and non-null markers.</summary>
public sealed class Args(JsonElement root)
{
    public string? String(string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    public bool? Bool(string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : null;

    /// <summary>AWSBigInt arrives as a JSON number (or a numeric string); null when missing or not an integer.</summary>
    public long? Long(string name)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(name, out var v))
            return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n))
            return n;
        return v.ValueKind == JsonValueKind.String && long.TryParse(v.GetString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : null;
    }

    public Args? Object(string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Object ? new Args(v) : null;

    public AmendmentInput ToAmendmentInput() =>
        new(String("title"), String("author"), String("primaryLanguage"), String("motion"), String("source"), String("legisId"), String("parentAmendmentId"));
}
