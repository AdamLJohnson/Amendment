using System.Text.RegularExpressions;

namespace Amendment.Domain.Approval;

/// <summary>
/// Accepts proposed edits. Byte-for-byte equivalent of legacy <c>AmendmentCleanupService</c>:
/// 1. <c>~~(.*?)~~</c> (Singleline) is removed with its text (deletion accepted);
/// 2. <c>&lt;u&gt;(.*?)&lt;/u&gt;</c> (Singleline, IgnoreCase) is unwrapped (addition accepted).
/// No whitespace normalization.
/// </summary>
public static partial class AmendmentTextCleanup
{
    public static string Cleanup(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        return RemoveUnderline(RemoveStrikethrough(text));
    }

    public static string RemoveStrikethrough(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : Strikethrough().Replace(text, "");

    public static string RemoveUnderline(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : Underline().Replace(text, "$1");

    public static bool ContainsAmendmentFormatting(string? text) =>
        !string.IsNullOrEmpty(text) && (Strikethrough().IsMatch(text) || Underline().IsMatch(text));

    [GeneratedRegex("~~(.*?)~~", RegexOptions.Singleline)]
    private static partial Regex Strikethrough();

    [GeneratedRegex("<u>(.*?)</u>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex Underline();
}
