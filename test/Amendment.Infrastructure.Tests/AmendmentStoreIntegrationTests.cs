using Amendment.Domain;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using Amendment.Domain.Presentation;
using Amendment.Infrastructure.DynamoDb;

namespace Amendment.Infrastructure.Tests;

/// <summary>Create/read/update/duplicate/conflict behavior against a real DynamoDB API (Floci or AWS).</summary>
public class AmendmentStoreIntegrationTests(DynamoDbTestTable table) : IClassFixture<DynamoDbTestTable>
{
    private static readonly AmendmentRules Rules = new(SupportedLanguages.Default);
    private static readonly BodyRules Bodies = new(SupportedLanguages.Default);
    private static AuditStamp Stamp(string by = "user-1") => new(by, DateTimeOffset.UtcNow);

    private async Task<AmendmentRecord> CreateAmendmentAsync(string title = "Integration")
    {
        var record = Rules.Create(AmendmentIds.New(), new AmendmentInput(title, "Author", "en", "WC-1", "Floor", "L-1"), Stamp()).Value;
        Assert.True((await table.Store.CreateAmendmentAsync(record)).IsSuccess);
        return record;
    }

    private static AmendmentBodyRecord NewBody(AmendmentRecord a, string language, string markdown = "Text **NEWSLIDE** More") =>
        Bodies.Create(a, [], language, markdown, BodyStatus.Draft, Stamp()).Value;

    [DynamoDbFact]
    public async Task Amendment_round_trips_and_duplicate_create_is_a_conflict()
    {
        var created = await CreateAmendmentAsync();

        Assert.Equal(created, await table.Store.GetAmendmentAsync(created.Id));
        var duplicate = await table.Store.CreateAmendmentAsync(created);
        Assert.Equal(DomainErrorCode.Conflict, Assert.Single(duplicate.Errors).Code);
        Assert.Null(await table.Store.GetAmendmentAsync(AmendmentIds.New()));
    }

    [DynamoDbFact]
    public async Task Update_requires_the_current_version()
    {
        var v1 = await CreateAmendmentAsync();
        var v2 = Rules.Update(v1, new AmendmentInput("Renamed", "Author", "es"), false, 1, Stamp("user-2")).Value;
        Assert.True((await table.Store.UpdateAmendmentAsync(v2)).IsSuccess);
        Assert.Equal(v2, await table.Store.GetAmendmentAsync(v1.Id));

        var stale = Rules.Update(v1, new AmendmentInput("Stale", "Author", "en"), false, 1, Stamp()).Value;
        var result = await table.Store.UpdateAmendmentAsync(stale);
        Assert.Equal(DomainErrorCode.Conflict, Assert.Single(result.Errors).Code);
        Assert.Equal("Renamed", (await table.Store.GetAmendmentAsync(v1.Id))!.Title);

        var missing = await table.Store.UpdateAmendmentAsync(v2 with { Id = AmendmentIds.New() });
        Assert.Equal(DomainErrorCode.NotFound, Assert.Single(missing.Errors).Code);
    }

    [DynamoDbFact]
    public async Task Two_editors_saving_the_same_version_one_wins() // A03
    {
        var v1 = await CreateAmendmentAsync();
        var edits = Enumerable.Range(0, 2)
            .Select(i => Rules.Update(v1, new AmendmentInput($"Edit {i}", "Author", "en"), false, 1, Stamp($"editor-{i}")).Value)
            .Select(next => table.Store.UpdateAmendmentAsync(next));

        var results = await Task.WhenAll(edits);
        Assert.Single(results, r => r.IsSuccess);
        Assert.Equal(DomainErrorCode.Conflict, Assert.Single(results.Single(r => !r.IsSuccess).Errors).Code);
        Assert.Equal(2, (await table.Store.GetAmendmentAsync(v1.Id))!.Version);
    }

    [DynamoDbFact]
    public async Task Body_requires_its_amendment_and_is_unique_per_language()
    {
        var a = await CreateAmendmentAsync();
        var orphan = await table.Store.CreateBodyAsync(NewBody(a, "en") with { AmendmentId = AmendmentIds.New() });
        Assert.Equal(DomainErrorCode.NotFound, Assert.Single(orphan.Errors).Code);

        Assert.True((await table.Store.CreateBodyAsync(NewBody(a, "en"))).IsSuccess);
        var duplicate = await table.Store.CreateBodyAsync(NewBody(a, "EN", "Other"));
        Assert.Equal(DomainErrorCode.Conflict, Assert.Single(duplicate.Errors).Code);
        Assert.Equal("Text **NEWSLIDE** More", (await table.Store.GetBodyAsync(a.Id, LanguageCode.English))!.Markdown);
    }

    [DynamoDbFact]
    public async Task Two_simultaneous_spanish_creations_exactly_one_succeeds() // A02
    {
        var a = await CreateAmendmentAsync();
        var results = await Task.WhenAll(
            table.Store.CreateBodyAsync(NewBody(a, "es", "Uno")),
            table.Store.CreateBodyAsync(NewBody(a, "es", "Dos")));

        Assert.Single(results, r => r.IsSuccess);
        Assert.Equal(DomainErrorCode.Conflict, Assert.Single(results.Single(r => !r.IsSuccess).Errors).Code);
    }

    [DynamoDbFact]
    public async Task Body_update_is_version_checked_and_amendment_reads_with_all_bodies()
    {
        var a = await CreateAmendmentAsync();
        var en = NewBody(a, "en");
        await table.Store.CreateBodyAsync(en);
        await table.Store.CreateBodyAsync(NewBody(a, "fr", "Bonjour"));

        var v2 = BodyRules.Update(en, "Saved", BodyStatus.Ready, 1, Stamp("translator")).Value;
        Assert.True((await table.Store.UpdateBodyAsync(v2)).IsSuccess);
        var stale = BodyRules.Update(en, "Lost", BodyStatus.Ready, 1, Stamp()).Value;
        Assert.Equal(DomainErrorCode.Conflict, Assert.Single((await table.Store.UpdateBodyAsync(stale)).Errors).Code);

        var loaded = (await table.Store.GetAmendmentWithBodiesAsync(a.Id))!;
        Assert.Equal(a, loaded.Amendment);
        Assert.Equal(["en", "fr"], loaded.Bodies.Select(b => b.Language.Value).Order());
        Assert.Equal(v2, loaded.Bodies.Single(b => b.Language == LanguageCode.English));
        Assert.Null(await table.Store.GetAmendmentWithBodiesAsync(AmendmentIds.New()));
    }

    [DynamoDbFact]
    public async Task Presentation_starts_empty_and_saves_are_version_checked()
    {
        var a = await CreateAmendmentAsync("Presented");
        var en = NewBody(a, "en");
        await table.Store.CreateBodyAsync(en);

        var empty = await table.Store.GetPresentationAsync();
        var presented = empty.Present(a, [en], empty.Version, Stamp("operator")).Value;
        Assert.True((await table.Store.SavePresentationAsync(presented)).IsSuccess);

        var stale = empty.Present(a, [en], empty.Version, Stamp("operator-2")).Value;
        Assert.Equal(DomainErrorCode.Conflict, Assert.Single((await table.Store.SavePresentationAsync(stale)).Errors).Code);

        var shown = presented.SetLanguages([new LanguageChange(LanguageCode.English, Visible: true, Page: 1)],
            new Dictionary<LanguageCode, int> { [LanguageCode.English] = en.PageCount }, presented.Version, Stamp("operator")).Value;
        Assert.True((await table.Store.SavePresentationAsync(shown)).IsSuccess);

        var loaded = await table.Store.GetPresentationAsync();
        Assert.Equal(a.Id, loaded.ActiveAmendmentId);
        Assert.Equal(shown.Version, loaded.Version);
        Assert.Equal(new LanguageView(true, 1), loaded.Languages[LanguageCode.English]);
        Assert.Equal(shown.Updated, loaded.Updated);
    }
}
