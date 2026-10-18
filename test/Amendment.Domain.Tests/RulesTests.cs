using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using static Amendment.Domain.Tests.Fixtures;
using static Amendment.Domain.Tests.Fixtures.Conference;

namespace Amendment.Domain.Tests;

public class AmendmentRulesTests
{
    private readonly AmendmentRules _rules = new(SupportedLanguages.Default);

    [Fact]
    public void Create_StartsAtVersionOne_NotApprovedOrArchived()
    {
        var created = _rules.Create("N", new AmendmentInput(" Title ", "Author", "ENG"), Seed).Value;
        Assert.Equal(1, created.Version);
        Assert.Equal("Title", created.Title);
        Assert.Equal(En, created.PrimaryLanguage);
        Assert.False(created.IsApproved || created.IsArchived);
    }

    [Theory]
    [InlineData(null, "Author", "en", "title")]
    [InlineData("  ", "Author", "en", "title")]
    [InlineData("not allowed", "Author", "en", "title")]
    [InlineData("Title", "", "en", "author")]
    [InlineData("Title", "Author", null, "primaryLanguage")]
    [InlineData("Title", "Author", "de", "primaryLanguage")]
    public void Create_Validation(string? title, string? author, string? language, string field)
    {
        var result = _rules.Create("N", new AmendmentInput(title, author, language), Seed);
        Assert.Contains(result.Errors, e => e.Code == DomainErrorCode.ValidationError && e.Field == field);
    }

    [Fact]
    public void Update_RequiresCurrentVersion_AndIncrements()
    {
        var input = new AmendmentInput("New", "Author", "en");
        Assert.Equal(DomainErrorCode.Conflict, _rules.Update(OneSlide, input, false, 0, Editor).Errors.Single().Code);

        var updated = _rules.Update(OneSlide, input, false, 1, Editor).Value;
        Assert.Equal(2, updated.Version);
        Assert.Equal(Editor, updated.Updated);
    }

    [Fact]
    public void Update_CannotApprove_ButCanUnapprove()
    {
        var input = new AmendmentInput("T", "A", "en");
        Assert.Equal("isApproved", _rules.Update(OneSlide, input, true, 1, Editor).Errors.Single().Field);
        Assert.False(_rules.Update(OneSlide with { IsApproved = true }, input, false, 1, Editor).Value.IsApproved);
    }

    [Fact]
    public void Archive_BlockedWhilePresented_UnarchiveAllowed()
    {
        Assert.Equal(DomainErrorCode.PreconditionFailed, _rules.SetArchived(Parent, true, 4, "P", Editor).Errors.Single().Code);
        Assert.True(_rules.SetArchived(Parent, true, 4, null, Editor).Value.IsArchived);
        Assert.False(_rules.SetArchived(Archived, false, 1, null, Editor).Value.IsArchived);
        Assert.Same(Archived, _rules.SetArchived(Archived, true, 1, null, Editor).Value);
    }

    [Fact]
    public void Delete_BlockedWhilePresented() =>
        Assert.Equal(DomainErrorCode.PreconditionFailed, AmendmentRules.CanDelete(Parent, 4, "P")!.Code);

    [Fact]
    public void Clone_CopiesBodies_AsChildOfSource()
    {
        var (clone, bodies) = _rules.Clone(Parent, ParentBodies, "K", new AmendmentInput("Copy", null, null), Editor).Value;
        Assert.Equal("P", clone.ParentAmendmentId);
        Assert.Equal("Copy", clone.Title);
        Assert.Equal(Parent.Author, clone.Author);
        Assert.False(clone.IsApproved || clone.IsArchived);
        Assert.Equal(3, bodies.Count);
        Assert.All(bodies, b => Assert.True(b.AmendmentId == "K" && b.Version == 1));
        Assert.Equal(ParentBodies.Select(b => b.Markdown), bodies.Select(b => b.Markdown));
    }
}

public class BodyRulesTests
{
    private readonly BodyRules _rules = new(SupportedLanguages.Default);

    [Fact]
    public void Create_DuplicateLanguage_IsConflict_EvenWithLegacyCode()
    {
        var result = _rules.Create(Parent, ParentBodies.Select(b => b.Language), "ENG", "text", BodyStatus.New, Editor);
        Assert.Equal(DomainErrorCode.Conflict, result.Errors.Single().Code);
    }

    [Fact]
    public void Create_RequiresAmendment() =>
        Assert.Equal(DomainErrorCode.NotFound, _rules.Create(null, [], "en", "x", BodyStatus.New, Editor).Errors.Single().Code);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \n\t ")]
    public void BlankMarkdown_IsRejected(string? markdown)
    {
        Assert.Equal("markdown", _rules.Create(OneSlide, [], "es", markdown, BodyStatus.New, Editor).Errors.Single().Field);
        Assert.Equal("markdown", BodyRules.Update(OneSlideBodies[0], markdown, BodyStatus.New, 1, Editor).Errors.Single().Field);
    }

    [Fact]
    public void TooLongMarkdown_IsRejected() =>
        Assert.Single(BodyRules.ValidateMarkdown(new string('x', BodyRules.MaxMarkdownLength + 1)));

    [Fact]
    public void Update_StaleVersion_IsConflict_CurrentVersion_Increments()
    {
        var body = ParentBodies[0];
        Assert.Equal(DomainErrorCode.Conflict, BodyRules.Update(body, "x", BodyStatus.Draft, 2, Editor).Errors.Single().Code);
        var updated = BodyRules.Update(body, "x\n**NEWSLIDE**\ny", BodyStatus.Draft, 3, Editor).Value;
        Assert.Equal(4, updated.Version);
        Assert.Equal(2, updated.PageCount);
    }
}
