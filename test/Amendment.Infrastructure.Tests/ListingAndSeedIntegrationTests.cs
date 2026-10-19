using Amazon.DynamoDBv2.Model;
using Amendment.Domain;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using Amendment.Infrastructure.DynamoDb;
using Amendment.SeedDynamoDb;

namespace Amendment.Infrastructure.Tests;

/// <summary>Each test gets its own empty table.</summary>
public abstract class FreshTableTests : IAsyncLifetime
{
    protected DynamoDbTestTable Table { get; } = new();
    protected AmendmentStore Store => Table.Store;
    public Task InitializeAsync() => Table.InitializeAsync();
    public Task DisposeAsync() => Table.DisposeAsync();

    /// <summary>GSI1 is eventually consistent on AWS: wait until the list holds the expected number of amendments.</summary>
    protected async Task WaitForListCountAsync(bool archived, int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while ((await Store.ListAmendmentsAsync(archived, AmendmentStore.MaxPageSize, null)).Value.Items.Count != expected)
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"GSI1 did not show {expected} {(archived ? "archived" : "active")} amendments.");
            await Task.Delay(250);
        }
    }
}

public class AmendmentListingIntegrationTests : FreshTableTests
{
    private static readonly AmendmentRules Rules = new(SupportedLanguages.Default);

    [DynamoDbFact]
    public async Task Lists_active_and_archived_amendments_in_creation_order_with_page_tokens()
    {
        var start = new DateTimeOffset(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);
        var created = new List<AmendmentRecord>();
        for (var i = 0; i < 7; i++)
        {
            var record = Rules.Create(AmendmentIds.New(), new AmendmentInput($"A{i}", "Author", "en"), new AuditStamp("u", start.AddMinutes(i))).Value;
            await Store.CreateAmendmentAsync(record);
            created.Add(record);
        }
        foreach (var archived in created.Skip(5))
            Assert.True((await Store.UpdateAmendmentAsync(Rules.SetArchived(archived, true, 1, null, new AuditStamp("u", start)).Value)).IsSuccess);
        await WaitForListCountAsync(false, 5);
        await WaitForListCountAsync(true, 2);

        var titles = new List<string>();
        string? token = null;
        var pages = 0;
        do
        {
            var page = (await Store.ListAmendmentsAsync(false, 2, token)).Value;
            titles.AddRange(page.Items.Select(a => a.Title));
            token = page.NextPageToken;
            pages++;
        } while (token is not null && pages < 10);

        Assert.Equal(["A0", "A1", "A2", "A3", "A4"], titles);
        var archivedPage = (await Store.ListAmendmentsAsync(true, 10, null)).Value;
        Assert.Equal(["A5", "A6"], archivedPage.Items.Select(a => a.Title));
        Assert.All(archivedPage.Items, a => Assert.True(a.IsArchived));
    }

    [DynamoDbFact]
    public async Task Rejects_bad_page_sizes_and_tokens_from_another_list()
    {
        for (var i = 0; i < 3; i++)
            await Store.CreateAmendmentAsync(Rules.Create(AmendmentIds.New(), new AmendmentInput($"A{i}", "Author", "en"), new AuditStamp("u", DateTimeOffset.UtcNow)).Value);
        await WaitForListCountAsync(false, 3);

        var first = (await Store.ListAmendmentsAsync(false, 1, null)).Value;
        Assert.NotNull(first.NextPageToken);
        Assert.Equal(DomainErrorCode.ValidationError, Assert.Single((await Store.ListAmendmentsAsync(true, 1, first.NextPageToken)).Errors).Code);
        Assert.Equal(DomainErrorCode.ValidationError, Assert.Single((await Store.ListAmendmentsAsync(false, 1, "not-a-token")).Errors).Code);
        Assert.Equal(DomainErrorCode.ValidationError, Assert.Single((await Store.ListAmendmentsAsync(false, 0, null)).Errors).Code);
        Assert.Equal(DomainErrorCode.ValidationError, Assert.Single((await Store.ListAmendmentsAsync(false, AmendmentStore.MaxPageSize + 1, null)).Errors).Code);
    }
}

public class SeederIntegrationTests : FreshTableTests
{
    private static readonly SeedFixture Fixture = SeedFixture.Load();

    [DynamoDbFact]
    public async Task Seeding_a_fresh_table_matches_the_fixture_and_reseeding_changes_nothing() // A15
    {
        var first = await Seeder.SeedAsync(Store, Fixture);
        Assert.Equal(Fixture.AllItems().Count, first.Written);
        Assert.Equal(0, first.AlreadyPresent);

        var verify = await SeedVerifier.VerifyAsync(Store, Fixture);
        Assert.True(verify.Passed, string.Join("; ", verify.Problems));
        Assert.Equal(verify.ExpectedChecksum, verify.ActualChecksum);
        Assert.Equal(3, (await Store.GetLanguagesAsync()).Count);
        await WaitForListCountAsync(false, 8);
        Assert.Equal("Test Amendment 2", (await Store.GetAmendmentAsync((await Store.GetPresentationAsync()).ActiveAmendmentId!))!.Title);

        var second = await Seeder.SeedAsync(Store, Fixture);
        Assert.Equal(0, second.Written);
        Assert.Equal(Fixture.AllItems().Count, second.AlreadyPresent);
        Assert.Equal(verify.ActualChecksum, (await SeedVerifier.VerifyAsync(Store, Fixture)).ActualChecksum);
    }

    [DynamoDbFact]
    public async Task Reseeding_never_overwrites_edited_data()
    {
        await Seeder.SeedAsync(Store, Fixture);
        var seeded = Fixture.BodyRecords().First();
        var edited = BodyRules.Update(seeded, "Edited by a translator", BodyStatus.Draft, 1, new AuditStamp("translator", DateTimeOffset.UtcNow)).Value;
        Assert.True((await Store.UpdateBodyAsync(edited)).IsSuccess);

        await Seeder.SeedAsync(Store, Fixture);
        Assert.Equal(edited, await Store.GetBodyAsync(seeded.AmendmentId, seeded.Language));
        var verify = await SeedVerifier.VerifyAsync(Store, Fixture);
        Assert.Contains(verify.Problems, p => p.Contains("differs") && p.Contains(seeded.AmendmentId));
    }

    [DynamoDbFact]
    public async Task Sample_amendments_are_skipped_when_other_amendments_exist_but_system_items_are_added()
    {
        var rules = new AmendmentRules(SupportedLanguages.Default);
        await Store.CreateAmendmentAsync(rules.Create(AmendmentIds.New(), new AmendmentInput("Real", "Author", "en"), new AuditStamp("u", DateTimeOffset.UtcNow)).Value);

        var report = await Seeder.SeedAsync(Store, Fixture);
        Assert.True(report.SampleSkipped);
        Assert.Equal(Fixture.SystemItems().Count, report.Written);
        Assert.Equal(1, (await Store.ScanAmendmentIdsAsync()).Count);
        Assert.Null((await Store.GetPresentationAsync()).ActiveAmendmentId);
    }

    [DynamoDbFact]
    public async Task A_partially_seeded_table_is_completed()
    {
        var firstAmendment = Fixture.SampleItems()[0];
        Assert.True(await Store.TryPutNewItemAsync(new Dictionary<string, AttributeValue>(firstAmendment)));

        var report = await Seeder.SeedAsync(Store, Fixture);
        Assert.False(report.SampleSkipped);
        Assert.Equal(1, report.AlreadyPresent);
        Assert.True((await SeedVerifier.VerifyAsync(Store, Fixture)).Passed);
    }
}
