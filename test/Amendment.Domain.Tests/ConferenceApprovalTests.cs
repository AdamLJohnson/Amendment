using Amendment.Domain.Approval;
using Amendment.Domain.Model;
using Amendment.Domain.Presentation;
using static Amendment.Domain.Tests.Fixtures;
using static Amendment.Domain.Tests.Fixtures.Conference;

namespace Amendment.Domain.Tests;

public class ConferenceApprovalTests
{
    private static DomainResult<ApprovalPlan> Plan(
        ApprovalRequest? request = null,
        AmendmentRecord? child = null,
        IReadOnlyList<AmendmentBodyRecord>? childBodies = null,
        ParentSnapshot? parent = null,
        PresentationState? presentation = null,
        Authorization.Principal? principal = null) =>
        ConferenceApproval.Plan(
            principal ?? AmendEditor,
            request ?? new ApprovalRequest("C", 2),
            child ?? Child,
            childBodies ?? ChildBodies,
            parent ?? new ParentSnapshot(Parent, ParentBodies),
            presentation ?? LivePresentation(),
            Editor);

    private static string ParentText(ApprovalPlan plan, Languages.LanguageCode language) =>
        plan.ParentBodyWrites.Single(w => w.After.Language == language).After.Markdown;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Golden_ParentReceivesCleanFullText_RegardlessOfChildCleanupFlag(bool cleanChild)
    {
        var plan = Plan(new ApprovalRequest("C", 2, cleanChild)).Value;

        Assert.True(plan.IsTransition);
        Assert.Equal("We  will proceed.", ParentText(plan, En));
        Assert.Equal(" Vamos a proceder.", ParentText(plan, Es));
        Assert.DoesNotContain(plan.ParentBodyWrites, w => w.After.Language == Fr);
        Assert.Equal(cleanChild ? 2 : 0, plan.ChildBodyWrites.Count);
        Assert.True(plan.Amendment.IsApproved);
        Assert.Equal(3, plan.Amendment.Version);
    }

    [Fact]
    public void ChildCleanup_WritesOnlyChangedBodies_WithVersionAndAudit()
    {
        var bodies = new[] { Body("C", En, "We ~~shall~~ <u>will</u> proceed.", 7), Body("C", Fr, "No markup") };
        var plan = Plan(childBodies: bodies).Value;

        var write = Assert.Single(plan.ChildBodyWrites);
        Assert.Equal("We  will proceed.", write.After.Markdown);
        Assert.Equal(7, write.Before.Version);
        Assert.Equal(8, write.After.Version);
        Assert.Equal(Editor, write.After.Updated);
    }

    [Fact]
    public void LegacyParity_ParentGetsCleanupOfCleanedChild_WhenChildIsCleaned()
    {
        // Legacy cleans the child first and then cleans that result again for the parent.
        var bodies = new[] { Body("C", En, "x ~<u>~y~</u>~ z") };
        Assert.Equal("x  z", ParentText(Plan(new ApprovalRequest("C", 2, true), childBodies: bodies).Value, En));
        Assert.Equal("x ~~y~~ z", ParentText(Plan(new ApprovalRequest("C", 2, false), childBodies: bodies).Value, En));
    }

    [Fact]
    public void AlreadyApproved_IsNoOp_EvenWithStaleVersion()
    {
        var plan = Plan(new ApprovalRequest("C", 1), child: Child with { IsApproved = true, Version = 9 }).Value;
        Assert.False(plan.IsTransition);
        Assert.Empty(plan.AtomicBodyWrites);
        Assert.Empty(plan.BestEffortBodyWrites);
    }

    [Fact]
    public void StaleVersion_IsConflict()
    {
        Assert.Equal(DomainErrorCode.Conflict, Plan(new ApprovalRequest("C", 1)).Errors.Single().Code);
    }

    [Fact]
    public void UnchangedParentBody_IsNotWritten()
    {
        var parentBodies = new[] { Body("P", En, "We  will proceed.") };
        var plan = Plan(parent: new ParentSnapshot(Parent, parentBodies)).Value;
        Assert.Empty(plan.ParentBodyWrites);
    }

    [Fact]
    public void NoParent_ApprovesWithoutParentWrites()
    {
        var plan = Plan(child: Child with { ParentAmendmentId = null }, parent: null).Value;
        Assert.Empty(plan.ParentBodyWrites);
        Assert.Empty(plan.Warnings);
    }

    [Fact]
    public void MissingParent_AllOrNothing_FailsWithoutWrites()
    {
        var result = Plan(parent: new ParentSnapshot(null, []));
        Assert.Equal(DomainErrorCode.PreconditionFailed, result.Errors.Single().Code);
    }

    [Fact]
    public void MissingParent_BestEffort_Admin_SucceedsWithWarning()
    {
        var plan = Plan(new ApprovalRequest("C", 2, FailureMode: ApprovalFailureMode.BestEffort),
            parent: new ParentSnapshot(null, []), principal: Admin).Value;
        Assert.True(plan.Amendment.IsApproved);
        Assert.Single(plan.Warnings);
        Assert.Equal(2, plan.AtomicBodyWrites.Count);
    }

    [Fact]
    public void BestEffort_RequiresAdmin()
    {
        var result = Plan(new ApprovalRequest("C", 2, FailureMode: ApprovalFailureMode.BestEffort));
        Assert.Equal(DomainErrorCode.Forbidden, result.Errors.Single().Code);
    }

    [Fact]
    public void FailureMode_SplitsAtomicAndBestEffortWrites()
    {
        var atomic = Plan().Value;
        Assert.Equal(4, atomic.AtomicBodyWrites.Count);
        Assert.Empty(atomic.BestEffortBodyWrites);

        var bestEffort = Plan(new ApprovalRequest("C", 2, FailureMode: ApprovalFailureMode.BestEffort), principal: Admin).Value;
        Assert.Equal(2, bestEffort.AtomicBodyWrites.Count);
        Assert.Equal(2, bestEffort.BestEffortBodyWrites.Count);
    }

    [Fact]
    public void ParentWouldBecomeEmpty_AllOrNothing_IsValidationError()
    {
        var bodies = new[] { Body("C", En, "~~everything~~") };
        var result = Plan(new ApprovalRequest("C", 2, false), childBodies: bodies);
        Assert.Equal(DomainErrorCode.ValidationError, result.Errors.Single().Code);
    }

    [Fact]
    public void ProjectorRefresh_OnlyForVisibleLanguagesOfPresentedParent()
    {
        Assert.Equal(new[] { En }, Plan().Value.ProjectorRefreshLanguages);
        Assert.Empty(Plan(presentation: PresentationState.Empty).Value.ProjectorRefreshLanguages);
    }

    [Theory]
    [InlineData("tr")]
    [InlineData("sc")]
    [InlineData("nobody")]
    public void NonEditors_CannotApprove(string userId)
    {
        var principal = new[] { Translator, ScreenController, NoRoles }.Single(p => p.UserId == userId);
        Assert.Equal(DomainErrorCode.Forbidden, Plan(principal: principal).Errors.Single().Code);
    }
}
