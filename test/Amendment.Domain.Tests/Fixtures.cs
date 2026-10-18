using Amendment.Domain.Authorization;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using Amendment.Domain.Presentation;
using Amendment.Domain.Timer;

namespace Amendment.Domain.Tests;

/// <summary>
/// Representative fixtures (plan Phase 0 item 3): several amendments, multilingual bodies, archived state,
/// varying slide counts, a live presentation and timer states.
/// </summary>
internal static class Fixtures
{
    public static readonly DateTimeOffset T0 = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
    public static readonly AuditStamp Seed = new("seed", T0);
    public static readonly AuditStamp Editor = new("editor-1", T0.AddMinutes(5));

    public static readonly LanguageCode En = LanguageCode.English;
    public static readonly LanguageCode Es = LanguageCode.Spanish;
    public static readonly LanguageCode Fr = LanguageCode.French;

    public static readonly Principal Admin = Principal.User("admin", Roles.Admin);
    public static readonly Principal AmendEditor = Principal.User("ae", Roles.AmendmentEditor);
    public static readonly Principal Translator = Principal.User("tr", Roles.Translator);
    public static readonly Principal ScreenController = Principal.User("sc", Roles.ScreenController);
    public static readonly Principal NoRoles = Principal.User("nobody");

    public const string ThreeSlides = "Slide 1\n**NEWSLIDE**\nSlide 2\n**NEWSLIDE**\nSlide 3";

    public static AmendmentRecord Amendment(string id, string? parentId = null, bool archived = false, bool approved = false, long version = 1) => new()
    {
        Id = id,
        Title = $"Amendment {id}",
        Author = "Author",
        PrimaryLanguage = En,
        ParentAmendmentId = parentId,
        IsArchived = archived,
        IsApproved = approved,
        Version = version,
        Created = Seed,
        Updated = Seed
    };

    public static AmendmentBodyRecord Body(string amendmentId, LanguageCode language, string markdown, long version = 1) => new()
    {
        AmendmentId = amendmentId,
        Language = language,
        Markdown = markdown,
        Status = BodyStatus.Ready,
        Version = version,
        Created = Seed,
        Updated = Seed
    };

    /// <summary>Parent "P" (live, 3 languages), child "C" proposing changes, archived "A", one-slide "S".</summary>
    public static class Conference
    {
        public static readonly AmendmentRecord Parent = Amendment("P", version: 4);
        public static readonly AmendmentRecord Child = Amendment("C", parentId: "P", version: 2);
        public static readonly AmendmentRecord Archived = Amendment("A", archived: true);
        public static readonly AmendmentRecord OneSlide = Amendment("S");

        public static readonly IReadOnlyList<AmendmentBodyRecord> ParentBodies =
        [
            Body("P", En, "We shall proceed.", 3),
            Body("P", Es, "Procederemos.", 2),
            Body("P", Fr, ThreeSlides, 5)
        ];

        public static readonly IReadOnlyList<AmendmentBodyRecord> ChildBodies =
        [
            Body("C", En, "We ~~shall~~ <u>will</u> proceed.", 7),
            Body("C", Es, "~~Procederemos~~ <u>Vamos a proceder</u>.", 2)
        ];

        public static readonly IReadOnlyList<AmendmentBodyRecord> OneSlideBodies = [Body("S", En, "Only one slide")];

        /// <summary>Parent "P" presented: English visible on page 0, Spanish hidden, French visible on page 2.</summary>
        public static PresentationState LivePresentation() =>
            PresentationState.Empty.Present(Parent, ParentBodies, 1, Seed).Value
                .SetLanguages(
                    [new LanguageChange(En, Visible: true), new LanguageChange(Fr, Visible: true, Page: 2)],
                    PageCounts(ParentBodies), 2, Seed).Value;

        public static IReadOnlyDictionary<LanguageCode, int> PageCounts(IEnumerable<AmendmentBodyRecord> bodies) =>
            bodies.ToDictionary(b => b.Language, b => b.PageCount);
    }

    public static class Timers
    {
        public static readonly TimerState Stopped = TimerState.Initial;
        public static readonly TimerState Paused90 = new() { RemainingAtLastChangeSeconds = 90, Version = 3 };
        public static readonly TimerState Running300 = new() { RemainingAtLastChangeSeconds = 300, StartedAt = T0, Running = true, Visible = true, Version = 7 };
    }
}
