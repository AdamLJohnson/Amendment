using Amendment.Domain.Languages;
using Amendment.Domain.Model;

namespace Amendment.Domain.Presentation;

/// <summary>
/// One language as seen by an anonymous projector. <see cref="Markdown"/> is null unless the language is visible
/// (decision D6: only visible bodies are public). A hidden language shows the "offline" screen.
/// </summary>
public sealed record PublicLanguageView(string Language, bool Visible, int Page, int PageCount, string? Markdown, long? BodyVersion);

/// <summary>Public, anonymous read model (<c>publicPresentation</c> query). Contains no drafts, titles or hidden bodies.</summary>
public sealed record PublicPresentation(long Version, string? ActiveAmendmentId, IReadOnlyList<PublicLanguageView> Languages);

public static class PublicProjection
{
    /// <summary>
    /// Builds the public read model from the authoritative state. Bodies of other amendments are ignored, so a stale or
    /// wrong body list can never leak content.
    /// </summary>
    public static PublicPresentation Create(PresentationState state, IEnumerable<AmendmentBodyRecord> bodies)
    {
        if (state.ActiveAmendmentId is null)
            return new PublicPresentation(state.Version, null, []);

        var byLanguage = bodies
            .Where(b => b.AmendmentId == state.ActiveAmendmentId)
            .ToDictionary(b => b.Language);

        var languages = state.Languages
            .OrderBy(kv => kv.Key.Value, StringComparer.Ordinal)
            .Select(kv =>
            {
                byLanguage.TryGetValue(kv.Key, out var body);
                var visible = kv.Value.Visible && body is not null;
                return visible
                    ? new PublicLanguageView(kv.Key.Value, true, SlideDeck.NormalizePage(kv.Value.Page, body!.PageCount), body.PageCount, body.Markdown, body.Version)
                    : new PublicLanguageView(kv.Key.Value, false, 0, 0, null, null);
            })
            .ToList();

        return new PublicPresentation(state.Version, state.ActiveAmendmentId, languages);
    }

    /// <summary>Single-language view for <c>/View/{Language}</c>; unknown or hidden languages are offline.</summary>
    public static PublicLanguageView ForLanguage(PublicPresentation presentation, LanguageCode language) =>
        presentation.Languages.FirstOrDefault(l => l.Language == language.Value)
        ?? new PublicLanguageView(language.Value, false, 0, 0, null, null);
}
