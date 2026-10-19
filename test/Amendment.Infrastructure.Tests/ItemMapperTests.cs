using Amazon.DynamoDBv2.Model;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using Amendment.Infrastructure.DynamoDb;
using Amendment.SeedDynamoDb;

namespace Amendment.Infrastructure.Tests;

public class ItemMapperTests
{
    private static readonly AuditStamp Stamp = new("sub-1", new DateTimeOffset(2026, 10, 19, 12, 30, 15, TimeSpan.Zero).AddTicks(1234567));

    private static readonly AmendmentRecord Amendment = new()
    {
        Id = "01JAAAAAAAAAAAAAAAAAAAAAAA",
        Title = "T",
        Author = "A",
        Motion = "WC-1",
        PrimaryLanguage = LanguageCode.Spanish,
        IsArchived = true,
        ParentAmendmentId = "01JBBBBBBBBBBBBBBBBBBBBBBB",
        Version = 7,
        Created = Stamp,
        Updated = Stamp with { By = "sub-2" }
    };

    [Fact]
    public void Amendment_round_trips_and_carries_list_index_keys()
    {
        var item = ItemMapper.ToItem(Amendment);

        Assert.Equal(Amendment, ItemMapper.ToAmendment(item));
        Assert.Equal("AMENDMENT#01JAAAAAAAAAAAAAAAAAAAAAAA", item["PK"].S);
        Assert.Equal("META", item["SK"].S);
        Assert.Equal("AMENDMENTS#ARCHIVED", item["GSI1PK"].S);
        Assert.Equal("2026-10-19T12:30:15.1234567Z#01JAAAAAAAAAAAAAAAAAAAAAAA", item["GSI1SK"].S);
        Assert.False(item.ContainsKey("source"));
        Assert.Equal("7", item["version"].N);
    }

    [Fact]
    public void Body_round_trips_without_list_index_keys()
    {
        var body = new AmendmentBodyRecord
        {
            AmendmentId = Amendment.Id, Language = LanguageCode.French, Markdown = "a **NEWSLIDE** b",
            Status = BodyStatus.UnderReview, Version = 3, Created = Stamp, Updated = Stamp
        };
        var item = ItemMapper.ToItem(body);

        Assert.Equal(body, ItemMapper.ToBody(item));
        Assert.Equal("BODY#fr", item["SK"].S);
        Assert.Equal("UnderReview", item["status"].S);
        Assert.False(item.ContainsKey("GSI1PK"));
    }

    [Fact]
    public void Seed_presentation_round_trips()
    {
        var fixture = SeedFixture.Load(LegacySeed.FixtureSourcePath);
        var state = fixture.PresentationRecord();
        var loaded = ItemMapper.ToPresentation(ItemMapper.ToItem(state));

        Assert.Equal(state.ActiveAmendmentId, loaded.ActiveAmendmentId);
        Assert.Equal(state.Version, loaded.Version);
        Assert.Equal(state.Updated, loaded.Updated);
        Assert.Equal(state.Languages.OrderBy(kv => kv.Key.Value), loaded.Languages.OrderBy(kv => kv.Key.Value));
    }

    [Fact]
    public void Page_token_round_trips_and_is_bound_to_its_list()
    {
        var key = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new() { S = "AMENDMENT#1" }, ["SK"] = new() { S = "META" },
            ["GSI1PK"] = new() { S = "AMENDMENTS#ACTIVE" }, ["GSI1SK"] = new() { S = "2026#1" }
        };
        var token = PageToken.Encode(key)!;

        Assert.Equal(key.ToDictionary(kv => kv.Key, kv => kv.Value.S), PageToken.Decode(token, "AMENDMENTS#ACTIVE")!.ToDictionary(kv => kv.Key, kv => kv.Value.S));
        Assert.Null(PageToken.Decode(token, "AMENDMENTS#ARCHIVED"));
        Assert.Null(PageToken.Decode("%%%", "AMENDMENTS#ACTIVE"));
        Assert.Null(PageToken.Decode(PageToken.Encode(new() { ["PK"] = new() { S = "x" } })!, "AMENDMENTS#ACTIVE"));
        Assert.Null(PageToken.Encode(null));
    }

    [Fact]
    public void New_ids_are_sortable_ulids()
    {
        var first = AmendmentIds.New();
        Thread.Sleep(2);
        var second = AmendmentIds.New();
        Assert.Equal(26, first.Length);
        Assert.True(string.CompareOrdinal(first, second) < 0);
    }
}
