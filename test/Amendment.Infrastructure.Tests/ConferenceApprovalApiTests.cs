using Amendment.Api.Lambda;
using Amendment.Domain.Languages;
using static Amendment.Infrastructure.Tests.ApiTestClient;

namespace Amendment.Infrastructure.Tests;

/// <summary>Conference approval parity checkpoint (plan §2B) through the write API against DynamoDB.</summary>
public class ConferenceApprovalApiTests(DynamoDbTestTable table) : IClassFixture<DynamoDbTestTable>
{
    private const string Proposed = "We ~~shall~~ <u>will</u> proceed.";
    private const string Cleaned = "We  will proceed.";
    private readonly ApiTestClient _api = new(table.Store);

    private async Task<(AmendmentDto Parent, AmendmentDto Child)> ParentAndChildAsync()
    {
        var parent = await _api.CreateAmendment("Parent");
        await _api.CreateBody(parent.Id, "en", "We shall proceed.");
        await _api.CreateBody(parent.Id, "es", "Procederemos.");
        await _api.CreateBody(parent.Id, "fr", "Nous procéderons.");
        var child = await _api.CreateAmendment("Child", parent.Id);
        await _api.CreateBody(child.Id, "en", Proposed);
        await _api.CreateBody(child.Id, "es", "~~Procederemos~~<u>Seguiremos</u>.");
        return (parent, child);
    }

    private Task<ApprovalResultDto> Approve(string id, bool cleanChild = true, ApiIdentity? as_ = null, long version = 1, string mode = "ALL_OR_NOTHING") =>
        _api.Ok<ApprovalResultDto>(as_ ?? Editor, "approveAmendment", new { id, expectedVersion = version, cleanChildBodies = cleanChild, failureMode = mode });

    private async Task<string> Markdown(string id, LanguageCode language) => (await table.Store.GetBodyAsync(id, language))!.Markdown;

    [DynamoDbFact]
    public async Task Approval_replaces_matching_parent_bodies_with_cleaned_child_text()
    {
        var (parent, child) = await ParentAndChildAsync();

        var result = await Approve(child.Id);

        Assert.True(result.ApprovalApplied);
        Assert.True(result.Amendment.IsApproved);
        Assert.Equal(["en", "es"], result.UpdatedChildLanguages.Order());
        Assert.Equal(["en", "es"], result.UpdatedParentLanguages.Order());
        Assert.Equal(Cleaned, await Markdown(parent.Id, LanguageCode.English));
        Assert.Equal("Seguiremos.", await Markdown(parent.Id, LanguageCode.Spanish));
        Assert.Equal("Nous procéderons.", await Markdown(parent.Id, LanguageCode.French)); // no matching child language
        Assert.Equal(Cleaned, await Markdown(child.Id, LanguageCode.English));
        Assert.True((await table.Store.GetAmendmentAsync(child.Id))!.IsApproved);
    }

    [DynamoDbFact]
    public async Task Without_child_cleanup_the_parent_is_still_updated()
    {
        var (parent, child) = await ParentAndChildAsync();

        var result = await Approve(child.Id, cleanChild: false);

        Assert.Empty(result.UpdatedChildLanguages);
        Assert.Equal(Proposed, await Markdown(child.Id, LanguageCode.English));
        Assert.Equal(Cleaned, await Markdown(parent.Id, LanguageCode.English));
    }

    [DynamoDbFact]
    public async Task Repeated_approval_is_a_safe_no_op()
    {
        var (parent, child) = await ParentAndChildAsync();
        await Approve(child.Id);
        var parentVersion = (await table.Store.GetBodyAsync(parent.Id, LanguageCode.English))!.Version;

        var retry = await Approve(child.Id); // same (now stale) expected version
        Assert.False(retry.ApprovalApplied);
        Assert.Equal(parentVersion, (await table.Store.GetBodyAsync(parent.Id, LanguageCode.English))!.Version);
        Assert.Equal(2, (await table.Store.GetAmendmentAsync(child.Id))!.Version);
    }

    [DynamoDbFact]
    public async Task Missing_parent_fails_all_or_nothing_and_warns_in_best_effort()
    {
        var parent = await _api.CreateAmendment("Doomed parent");
        await _api.CreateBody(parent.Id, "en", "Old");
        var child = await _api.CreateAmendment("Orphan", parent.Id);
        await _api.CreateBody(child.Id, "en", Proposed);
        await _api.Ok<string>(Admin, "deleteAmendment", new { id = parent.Id, expectedVersion = 1 });

        var failed = await _api.Fails(Editor, "approveAmendment", new { id = child.Id, expectedVersion = 1 });
        Assert.Equal("PRECONDITION_FAILED", failed.Type);
        Assert.False((await table.Store.GetAmendmentAsync(child.Id))!.IsApproved);
        Assert.Equal(Proposed, await Markdown(child.Id, LanguageCode.English)); // nothing written

        var bestEffort = await Approve(child.Id, as_: Admin, mode: "BEST_EFFORT");
        Assert.True(bestEffort.ApprovalApplied);
        Assert.Single(bestEffort.Warnings);
        Assert.Empty(bestEffort.UpdatedParentLanguages);
    }

    [DynamoDbFact]
    public async Task Stale_version_and_concurrent_sibling_approvals_never_overwrite_blindly()
    {
        var (parent, child) = await ParentAndChildAsync();
        Assert.Equal("CONFLICT", (await _api.Fails(Editor, "approveAmendment", new { id = child.Id, expectedVersion = 7 })).Type);

        var sibling = await _api.CreateAmendment("Sibling", parent.Id);
        await _api.CreateBody(sibling.Id, "en", "We <u>must</u> proceed.");
        var results = await Task.WhenAll(
            _api.Call(Editor, "approveAmendment", new { id = child.Id, expectedVersion = 1 }),
            _api.Call(Editor, "approveAmendment", new { id = sibling.Id, expectedVersion = 1 }));

        Assert.All(results, r => Assert.True(r.Error is null || r.Error.Type == "CONFLICT", r.Error?.Message));
        var successes = results.Count(r => r.Error is null);
        Assert.InRange(successes, 1, 2);
        Assert.Equal(1 + successes, (await table.Store.GetBodyAsync(parent.Id, LanguageCode.English))!.Version);
    }
}
