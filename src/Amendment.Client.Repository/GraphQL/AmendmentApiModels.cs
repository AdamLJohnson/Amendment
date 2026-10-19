namespace Amendment.Client.Repository.GraphQl;

// Shapes of infra/Amendment.Cdk/Schema/amendment.graphql. Versions are decimal strings of 64-bit integers.

public sealed record ApiAudit(string By, DateTimeOffset At);

public sealed record ApiAmendment(
    string Id,
    string Title,
    string Author,
    string? Motion,
    string? Source,
    string? LegisId,
    string PrimaryLanguage,
    bool IsArchived,
    bool IsApproved,
    string? ParentAmendmentId,
    string Version,
    ApiAudit Created,
    ApiAudit Updated,
    IReadOnlyList<ApiAmendmentBody> Bodies);

public sealed record ApiAmendmentBody(
    string AmendmentId,
    string Language,
    string Markdown,
    string Status,
    int PageCount,
    string Version,
    ApiAudit Created,
    ApiAudit Updated);

public sealed record ApiAmendmentPage(IReadOnlyList<ApiAmendment> Items, string? NextToken);

public sealed record ApiLanguage(string Code, string Name, int SortOrder);

public sealed record ApiApprovalResult(
    ApiAmendment Amendment,
    bool ApprovalApplied,
    IReadOnlyList<string> UpdatedChildLanguages,
    IReadOnlyList<string> UpdatedParentLanguages,
    IReadOnlyList<string> Warnings);

public sealed record ApiAmendmentInput(
    string Title,
    string Author,
    string PrimaryLanguage,
    string? Motion = null,
    string? Source = null,
    string? LegisId = null,
    string? ParentAmendmentId = null);

public sealed record ApiLanguageScreen(string Language, bool Visible, int Page);

public sealed record ApiPresentation(string? ActiveAmendmentId, IReadOnlyList<ApiLanguageScreen> Languages, string Version, ApiAudit? Updated);

/// <summary>A language change for <c>setLanguagePresentation</c>; null fields are left unchanged.</summary>
public sealed record ApiLanguageChange(string Language, bool? Visible = null, int? Page = null);

public sealed record ApiPublicLanguageView(string Language, bool Visible, int Page, int PageCount, string? Markdown, string? BodyVersion)
{
    public const string SlideDelimiter = "**NEWSLIDE**";

    /// <summary>Markdown of the current slide (<c>**NEWSLIDE**</c> split, as in <c>SlideDeck</c>); empty when hidden.</summary>
    public string CurrentSlide
    {
        get
        {
            if (!Visible || string.IsNullOrEmpty(Markdown))
                return "";
            var slides = Markdown.Split(SlideDelimiter);
            return Page >= 0 && Page < slides.Length ? slides[Page] : slides[0];
        }
    }
}

public sealed record ApiPublicPresentation(string Version, string? ActiveAmendmentId, IReadOnlyList<ApiPublicLanguageView> Languages);

/// <summary>Body status values of the GraphQL <c>BodyStatus</c> enum.</summary>
public static class ApiBodyStatus
{
    public const string New = "NEW", Draft = "DRAFT", UnderReview = "UNDER_REVIEW", Ready = "READY";
}

/// <summary>A GraphQL error. <see cref="ErrorType"/> follows the API error contract (plan section 5).</summary>
public sealed class AmendmentApiException(string errorType, string message, string? field = null) : Exception(message)
{
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Conflict = "CONFLICT";

    public string ErrorType { get; } = errorType;
    public string? Field { get; } = field;
}
