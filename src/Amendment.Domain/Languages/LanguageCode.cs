using System.Text.RegularExpressions;

namespace Amendment.Domain.Languages;

/// <summary>
/// Normalized, lowercase BCP-47 language code (e.g. <c>en</c>, <c>es</c>, <c>fr</c>, <c>pt-br</c>).
/// Used as the body key (<c>BODY#&lt;code&gt;</c>) so duplicate languages cannot exist.
/// </summary>
public readonly partial record struct LanguageCode
{
    private static readonly Dictionary<string, string> LegacyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        // Legacy Language.LanguageCode seed values (AmendmentContext)
        ["ENG"] = "en",
        ["SPA"] = "es",
        ["FRA"] = "fr",
        // Legacy Language.LanguageName values (used by /View/{Language})
        ["English"] = "en",
        ["Spanish"] = "es",
        ["French"] = "fr"
    };

    private LanguageCode(string value) => Value = value;

    public string Value { get; }

    public override string ToString() => Value;

    /// <summary>Normalizes a BCP-47 tag or a legacy code/name. Returns false if the input is not a valid tag.</summary>
    public static bool TryNormalize(string? input, out LanguageCode code)
    {
        code = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim();
        if (LegacyAliases.TryGetValue(trimmed, out var alias))
        {
            code = new LanguageCode(alias);
            return true;
        }

        var candidate = trimmed.Replace('_', '-').ToLowerInvariant();
        if (!Bcp47Pattern().IsMatch(candidate))
            return false;

        code = new LanguageCode(candidate);
        return true;
    }

    public static LanguageCode Parse(string input) =>
        TryNormalize(input, out var code) ? code : throw new FormatException($"'{input}' is not a valid language code.");

    public static readonly LanguageCode English = new("en");
    public static readonly LanguageCode Spanish = new("es");
    public static readonly LanguageCode French = new("fr");

    [GeneratedRegex("^[a-z]{2,3}(-[a-z0-9]{2,8})*$", RegexOptions.CultureInvariant)]
    private static partial Regex Bcp47Pattern();
}

/// <summary>Configured language set. Default matches the legacy seed (English, Spanish, French).</summary>
public sealed class SupportedLanguages
{
    private readonly HashSet<LanguageCode> _codes;

    public SupportedLanguages(IEnumerable<LanguageCode> codes) => _codes = [.. codes];

    public static SupportedLanguages Default { get; } =
        new([LanguageCode.English, LanguageCode.Spanish, LanguageCode.French]);

    public IReadOnlyCollection<LanguageCode> Codes => _codes;

    public bool Contains(LanguageCode code) => _codes.Contains(code);

    /// <summary>Normalizes and checks membership in one step.</summary>
    public DomainResult<LanguageCode> Resolve(string? input, string field = "language")
    {
        if (!LanguageCode.TryNormalize(input, out var code))
            return DomainError.Validation(field, $"'{input}' is not a valid language code.");
        if (!Contains(code))
            return DomainError.Validation(field, $"Language '{code}' is not configured.");
        return DomainResult<LanguageCode>.Success(code);
    }
}
