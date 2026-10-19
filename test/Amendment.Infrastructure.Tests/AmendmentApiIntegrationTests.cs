using Amendment.Api.Lambda;
using Amendment.Domain.Languages;
using static Amendment.Infrastructure.Tests.ApiTestClient;

namespace Amendment.Infrastructure.Tests;

/// <summary>Write API (Lambda) against DynamoDB: roles, validation, concurrency, archive/delete and presentation guards.</summary>
public class AmendmentApiIntegrationTests(DynamoDbTestTable table) : IClassFixture<DynamoDbTestTable>
{
    private readonly ApiTestClient _api = new(table.Store);

    [DynamoDbFact]
    public async Task Role_matrix_is_enforced_before_any_data_is_read()
    {
        var input = new { input = new { title = "T", author = "A", primaryLanguage = "en" } };
        Assert.Equal("FORBIDDEN", (await _api.Fails(null, "createAmendment", input)).Type);
        Assert.Equal("FORBIDDEN", (await _api.Fails(NoRole, "createAmendment", input)).Type);
        Assert.Equal("FORBIDDEN", (await _api.Fails(Translator, "createAmendment", input)).Type);
        Assert.Equal("FORBIDDEN", (await _api.Fails(Screen, "createAmendment", input)).Type);
        var created = await _api.Ok<AmendmentDto>(Admin, "createAmendment", input);

        var body = new { amendmentId = created.Id, language = "es", markdown = "Hola" };
        Assert.Equal("FORBIDDEN", (await _api.Fails(Screen, "createAmendmentBody", body)).Type);
        await _api.Ok<AmendmentBodyDto>(Translator, "createAmendmentBody", body);

        // Unknown amendments still answer FORBIDDEN to callers without the role (no existence leak).
        Assert.Equal("FORBIDDEN", (await _api.Fails(Editor, "deleteAmendment", new { id = "missing", expectedVersion = 1 })).Type);
        Assert.Equal("FORBIDDEN", (await _api.Fails(Editor, "approveAmendment", new { id = created.Id, expectedVersion = 1, failureMode = "BEST_EFFORT" })).Type);
        Assert.Equal(created.Id, await _api.Ok<string>(Admin, "deleteAmendment", new { id = created.Id, expectedVersion = 1 }));
        Assert.Null(await table.Store.GetBodyAsync(created.Id, LanguageCode.Spanish));
    }

    [DynamoDbFact]
    public async Task Invalid_data_is_rejected_with_validation_errors()
    {
        var invalid = await _api.Fails(Editor, "createAmendment", new { input = new { title = " ", author = "", primaryLanguage = "de" } });
        Assert.Equal("VALIDATION_ERROR", invalid.Type);
        Assert.Equal(["author", "primaryLanguage", "title"], invalid.Errors.Select(e => e.Field).Order());

        var a = await _api.CreateAmendment();
        var blank = await _api.Fails(Translator, "createAmendmentBody", new { amendmentId = a.Id, language = "en", markdown = "   " });
        Assert.Equal(("VALIDATION_ERROR", "markdown"), (blank.Type, blank.Field));
        var unknownLanguage = await _api.Fails(Translator, "createAmendmentBody", new { amendmentId = a.Id, language = "de", markdown = "Hallo" });
        Assert.Equal(("VALIDATION_ERROR", "language"), (unknownLanguage.Type, unknownLanguage.Field));
        Assert.Equal("NOT_FOUND", (await _api.Fails(Translator, "createAmendmentBody", new { amendmentId = "missing", language = "en", markdown = "x" })).Type);
        Assert.Equal("VALIDATION_ERROR", (await _api.Fails(Editor, "updateAmendment",
            new { id = a.Id, expectedVersion = 1, isApproved = true, input = new { title = "T", author = "A", primaryLanguage = "en" } })).Type);
        Assert.Equal("PRECONDITION_FAILED", (await _api.Fails(Editor, "createAmendment",
            new { input = new { title = "T", author = "A", primaryLanguage = "en", parentAmendmentId = "missing" } })).Type);
    }

    [DynamoDbFact]
    public async Task Duplicate_bodies_and_stale_body_writes_are_conflicts()
    {
        var a = await _api.CreateAmendment();
        await _api.CreateBody(a.Id, "en", "One");
        Assert.Equal("CONFLICT", (await _api.Fails(Translator, "createAmendmentBody", new { amendmentId = a.Id, language = "ENG", markdown = "Two" })).Type);

        var saves = await Task.WhenAll(Enumerable.Range(0, 2).Select(i =>
            _api.Call(Translator, "updateAmendmentBody", new { amendmentId = a.Id, language = "en", expectedVersion = 1, markdown = $"Edit {i}", status = "READY" })));
        Assert.Single(saves, r => r.Error is null);
        Assert.Equal("CONFLICT", saves.Single(r => r.Error is not null).Error!.Type);
        Assert.Equal(2, (await table.Store.GetBodyAsync(a.Id, LanguageCode.English))!.Version);
    }

    [DynamoDbFact]
    public async Task Presented_amendment_cannot_be_archived_or_deleted_and_new_bodies_join_the_presentation()
    {
        var a = await _api.CreateAmendment("Presented");
        await _api.CreateBody(a.Id, "en", "Slide 1 **NEWSLIDE** Slide 2");
        var loaded = (await table.Store.GetAmendmentWithBodiesAsync(a.Id))!;
        var current = await table.Store.GetPresentationAsync();
        var presented = current.Present(loaded.Amendment, loaded.Bodies, current.Version, new("operator", DateTimeOffset.UtcNow)).Value;
        Assert.True((await table.Store.SavePresentationAsync(presented)).IsSuccess);

        Assert.Equal("PRECONDITION_FAILED", (await _api.Fails(Editor, "archiveAmendment", new { id = a.Id, expectedVersion = 1, archived = true })).Type);
        Assert.Equal("PRECONDITION_FAILED", (await _api.Fails(Admin, "deleteAmendment", new { id = a.Id, expectedVersion = 1 })).Type);

        await _api.CreateBody(a.Id, "fr", "Bonjour");
        var after = await table.Store.GetPresentationAsync();
        Assert.Equal(presented.Version + 1, after.Version);
        Assert.False(after.Languages[LanguageCode.French].Visible);

        var other = await _api.CreateAmendment("Other");
        var archived = await _api.Ok<AmendmentDto>(Editor, "archiveAmendment", new { id = other.Id, expectedVersion = 1, archived = true });
        Assert.True(archived.IsArchived);
        Assert.Equal(2, archived.Version);
        Assert.Equal(2, (await _api.Ok<AmendmentDto>(Editor, "archiveAmendment", new { id = other.Id, expectedVersion = 2, archived = true })).Version);
    }

    [DynamoDbFact]
    public async Task Clone_copies_all_bodies_as_a_child()
    {
        var source = await _api.CreateAmendment("Source");
        await _api.CreateBody(source.Id, "en", "English");
        await _api.CreateBody(source.Id, "es", "Español");

        var clone = await _api.Ok<AmendmentDto>(Editor, "cloneAmendment", new { sourceId = source.Id });
        Assert.Equal(source.Id, clone.ParentAmendmentId);
        Assert.Equal(["en", "es"], clone.Bodies.Select(b => b.Language));
        Assert.Equal(2, (await table.Store.GetAmendmentWithBodiesAsync(clone.Id))!.Bodies.Count);
    }
}
