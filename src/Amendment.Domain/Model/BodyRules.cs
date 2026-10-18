using Amendment.Domain.Languages;

namespace Amendment.Domain.Model;

/// <summary>Create/update rules for language bodies.</summary>
public sealed class BodyRules(SupportedLanguages languages)
{
    /// <summary>Proposed limit (characters); keeps a body item well under the 400 KB DynamoDB item limit. [VERIFY]</summary>
    public const int MaxMarkdownLength = 100_000;

    /// <summary>
    /// Create. Exactly one body per (amendment, language): a duplicate is a <see cref="DomainErrorCode.Conflict"/>
    /// (legacy returned a 400 validation error). The parent amendment must exist.
    /// </summary>
    public DomainResult<AmendmentBodyRecord> Create(
        AmendmentRecord? amendment, IEnumerable<LanguageCode> existingLanguages, string? language, string? markdown, BodyStatus status, AuditStamp stamp)
    {
        if (amendment is null)
            return DomainError.NotFound("Amendment not found.");

        var errors = new List<DomainError>();
        var code = languages.Resolve(language);
        if (!code.IsSuccess)
            errors.AddRange(code.Errors);
        errors.AddRange(ValidateMarkdown(markdown));
        if (errors.Count > 0)
            return DomainResult<AmendmentBodyRecord>.Failure(errors);

        if (existingLanguages.Contains(code.Value))
            return DomainError.Conflict($"Amendment {amendment.Id} already has a '{code.Value}' body.");

        return DomainResult<AmendmentBodyRecord>.Success(new AmendmentBodyRecord
        {
            AmendmentId = amendment.Id,
            Language = code.Value,
            Markdown = markdown!,
            Status = status,
            Created = stamp,
            Updated = stamp
        });
    }

    /// <summary>
    /// Update markdown/status with compare-and-swap. The language is the item key and cannot change
    /// (legacy allowed changing <c>LanguageId</c>; target requires delete + create).
    /// </summary>
    public static DomainResult<AmendmentBodyRecord> Update(AmendmentBodyRecord existing, string? markdown, BodyStatus status, long expectedVersion, AuditStamp stamp)
    {
        if (Versioning.Check($"Body {existing.AmendmentId}/{existing.Language}", existing.Version, expectedVersion) is { } conflict)
            return conflict;

        var errors = ValidateMarkdown(markdown);
        if (errors.Count > 0)
            return DomainResult<AmendmentBodyRecord>.Failure(errors);

        return DomainResult<AmendmentBodyRecord>.Success(existing with
        {
            Markdown = markdown!,
            Status = status,
            Version = Versioning.Next(existing.Version),
            Updated = stamp
        });
    }

    /// <summary>Blank or whitespace-only markdown is rejected (legacy FluentValidation <c>NotEmpty</c>).</summary>
    public static List<DomainError> ValidateMarkdown(string? markdown)
    {
        var errors = new List<DomainError>();
        if (string.IsNullOrWhiteSpace(markdown))
            errors.Add(DomainError.Validation("markdown", "Body text is required."));
        else if (markdown.Length > MaxMarkdownLength)
            errors.Add(DomainError.Validation("markdown", $"Body text cannot exceed {MaxMarkdownLength} characters."));
        return errors;
    }
}
