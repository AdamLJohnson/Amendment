using Amendment.Domain.Events;
using Amendment.Domain.Presentation;
using static Amendment.Domain.Tests.Fixtures;
using static Amendment.Domain.Tests.Fixtures.Conference;

namespace Amendment.Domain.Tests;

public class PresentationStateTests
{
    [Fact]
    public void Present_SingleActiveAmendment_LanguagesStartHidden()
    {
        var state = PresentationState.Empty.Present(Parent, ParentBodies, 1, Seed).Value;
        Assert.Equal("P", state.ActiveAmendmentId);
        Assert.Equal(2, state.Version);
        Assert.All(state.Languages.Values, v => Assert.Equal(new LanguageView(false, 0), v));

        var switched = state.Present(OneSlide, OneSlideBodies, 2, Seed).Value;
        Assert.Equal("S", switched.ActiveAmendmentId);
        Assert.Equal(new[] { En }, switched.Languages.Keys);
    }

    [Fact]
    public void Present_ArchivedOrStale_IsRejected()
    {
        Assert.Equal(DomainErrorCode.PreconditionFailed, PresentationState.Empty.Present(Archived, [], 1, Seed).Errors.Single().Code);
        Assert.Equal(DomainErrorCode.Conflict, PresentationState.Empty.Present(Parent, ParentBodies, 7, Seed).Errors.Single().Code);
    }

    [Fact]
    public void SetLanguages_BatchIsOneVersion_AndNormalizesPages()
    {
        var state = LivePresentation();
        Assert.Equal(3, state.Version);
        Assert.Equal(new LanguageView(true, 2), state.Languages[Fr]);

        var next = state.SetLanguages([new LanguageChange(En, Page: 5), new LanguageChange(Fr, Page: 1)], PageCounts(ParentBodies), 3, Seed).Value;
        Assert.Equal(4, next.Version);
        Assert.Equal(0, next.Languages[En].Page);
        Assert.Equal(1, next.Languages[Fr].Page);
    }

    [Fact]
    public void SetLanguages_NoEffectiveChange_KeepsVersion()
    {
        var state = LivePresentation();
        Assert.Same(state, state.SetLanguages([new LanguageChange(En, Visible: true)], PageCounts(ParentBodies), 3, Seed).Value);
    }

    [Fact]
    public void SetLanguages_UnknownLanguageOrNothingPresented_Fails()
    {
        Assert.Equal(DomainErrorCode.PreconditionFailed,
            PresentationState.Empty.SetLanguages([new LanguageChange(En, true)], PageCounts(ParentBodies), 1, Seed).Errors.Single().Code);
        var single = PresentationState.Empty.Present(OneSlide, OneSlideBodies, 1, Seed).Value;
        Assert.Equal(DomainErrorCode.NotFound,
            single.SetLanguages([new LanguageChange(Fr, true)], PageCounts(OneSlideBodies), 2, Seed).Errors.Single().Code);
    }

    [Fact]
    public void Clear_IsIdempotent()
    {
        var cleared = LivePresentation().Clear(3, Seed).Value;
        Assert.Null(cleared.ActiveAmendmentId);
        Assert.Empty(cleared.Languages);
        Assert.Same(cleared, cleared.Clear(cleared.Version, Seed).Value);
    }

    [Fact]
    public void ReconcileBody_ResetsPageWhenSlidesShrink_AndAddsNewLanguageHidden()
    {
        var state = LivePresentation();
        var shrunk = state.ReconcileBody(Body("P", Fr, "one slide now", 6), Editor);
        Assert.Equal(new LanguageView(true, 0), shrunk.Languages[Fr]);
        Assert.Equal(4, shrunk.Version);

        Assert.Same(state, state.ReconcileBody(Body("P", Fr, ThreeSlides, 6), Editor));
        Assert.Same(state, state.ReconcileBody(Body("S", En, "other amendment"), Editor));

        var removed = state.RemoveBody("P", Es, Editor);
        Assert.False(removed.Languages.ContainsKey(Es));
    }
}

public class PublicProjectionTests
{
    [Fact]
    public void OnlyVisibleBodiesExposeMarkdown()
    {
        var bodies = ParentBodies.Concat(ChildBodies).ToList();
        var view = PublicProjection.Create(LivePresentation(), bodies);

        Assert.Equal("P", view.ActiveAmendmentId);
        Assert.Equal(new[] { "en", "es", "fr" }, view.Languages.Select(l => l.Language));
        Assert.Equal("We shall proceed.", view.Languages[0].Markdown);
        Assert.Equal(new PublicLanguageView("es", false, 0, 0, null, null), view.Languages[1]);
        Assert.Equal((2, 3, 5L), (view.Languages[2].Page, view.Languages[2].PageCount, view.Languages[2].BodyVersion!.Value));
        Assert.DoesNotContain(view.Languages, l => l.Markdown?.Contains("~~") == true);
    }

    [Fact]
    public void NothingPresented_ExposesNothing()
    {
        var view = PublicProjection.Create(PresentationState.Empty, ParentBodies);
        Assert.Null(view.ActiveAmendmentId);
        Assert.Empty(view.Languages);
        Assert.False(PublicProjection.ForLanguage(view, En).Visible);
    }
}

public class EventPolicyTests
{
    [Fact]
    public void LiveBodyEdit_PublishesPublicEvent_WithoutMarkdown()
    {
        var evt = EventPolicy.PublicForBody(LivePresentation(), ParentBodies[0] with { Version = 13 });
        Assert.Equal(new PublicPresentationEvent(EventTypes.LiveBodyUpdated, 3, "en", 13), evt);
    }

    [Fact]
    public void HiddenOrNonPresentedBodyEdit_PublishesNoPublicEvent()
    {
        Assert.Null(EventPolicy.PublicForBody(LivePresentation(), ParentBodies[1]));
        Assert.Null(EventPolicy.PublicForBody(LivePresentation(), ChildBodies[0]));
        Assert.Null(EventPolicy.PublicForBody(PresentationState.Empty, ParentBodies[0]));
    }

    [Fact]
    public void EditorialEvents_CarryItemVersion()
    {
        Assert.Equal(new EditorialEvent(EventTypes.BodyChanged, "C", "en", 7), EventPolicy.ForBody(ChildBodies[0]));
        Assert.Equal("/amendments/C/en", Channels.Editorial("C", En));
    }
}
