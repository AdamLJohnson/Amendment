using Amendment.Infrastructure.DynamoDb;

namespace Amendment.SeedDynamoDb;

public sealed record SeedReport(int Written, int AlreadyPresent, bool SampleSkipped, string? SkipReason);

/// <summary>
/// Writes the fixture with conditional puts (<c>attribute_not_exists</c>), so existing items are never overwritten.
/// Like the legacy seed, sample amendments are only written when the table holds no other amendments; a partially
/// seeded table (only fixture amendments present) is completed.
/// </summary>
public static class Seeder
{
    public static async Task<SeedReport> SeedAsync(AmendmentStore store, SeedFixture fixture, CancellationToken ct = default)
    {
        int written = 0, present = 0;

        async Task PutAll(IEnumerable<Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>> items)
        {
            foreach (var item in items)
            {
                if (await store.TryPutNewItemAsync(item, ct))
                    written++;
                else
                    present++;
            }
        }

        await PutAll(fixture.SystemItems());

        var foreignId = await FindNonFixtureAmendmentAsync(store, fixture, ct);
        if (foreignId is not null)
            return new SeedReport(written, present, true, $"the table already contains amendment {foreignId}, which is not part of the seed");

        await PutAll(fixture.SampleItems());
        return new SeedReport(written, present, false, null);
    }

    private static async Task<string?> FindNonFixtureAmendmentAsync(AmendmentStore store, SeedFixture fixture, CancellationToken ct)
    {
        var seedIds = fixture.Amendments.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);
        return (await store.ScanAmendmentIdsAsync(ct)).FirstOrDefault(id => !seedIds.Contains(id));
    }
}
