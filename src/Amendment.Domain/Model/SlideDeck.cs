namespace Amendment.Domain.Model;

/// <summary>
/// <c>**NEWSLIDE**</c> slide semantics, identical to legacy <c>AmendmentBody.Pages</c> /
/// <c>ScreenControlService.ChangeBodyPage</c>.
/// </summary>
public static class SlideDeck
{
    public const string Delimiter = "**NEWSLIDE**";

    /// <summary>Raw markdown per slide. Empty/null markdown has no slides; otherwise <c>string.Split</c> semantics (empty slides kept).</summary>
    public static IReadOnlyList<string> Split(string? markdown) =>
        string.IsNullOrEmpty(markdown) ? [] : markdown.Split(Delimiter);

    public static int PageCount(string? markdown) =>
        string.IsNullOrEmpty(markdown) ? 0 : markdown.Split(Delimiter).Length;

    /// <summary>Legacy rule: a page outside <c>[0, pageCount - 1]</c> resets to 0 (not clamped to the last page).</summary>
    public static int NormalizePage(int page, int pageCount) =>
        page < 0 || page > pageCount - 1 ? 0 : page;
}
