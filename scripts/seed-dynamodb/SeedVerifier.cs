using System.Security.Cryptography;
using System.Text;
using Amazon.DynamoDBv2.Model;
using Amendment.Domain.Model;
using Amendment.Infrastructure.DynamoDb;
using Item = System.Collections.Generic.Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>;

namespace Amendment.SeedDynamoDb;

public sealed record VerifyReport(
    int ExpectedItems, int FoundItems, string ExpectedChecksum, string ActualChecksum, IReadOnlyList<string> Problems)
{
    public bool Passed => Problems.Count == 0;
}

/// <summary>
/// Checks a seeded table against the fixture: every item present and identical (SHA-256 over a canonical form),
/// language and setting counts, body languages, page counts and presentation pages.
/// </summary>
public static class SeedVerifier
{
    public static async Task<VerifyReport> VerifyAsync(AmendmentStore store, SeedFixture fixture, CancellationToken ct = default)
    {
        var problems = new List<string>();
        var expected = fixture.AllItems();
        var actual = new List<Item>();

        foreach (var item in expected)
        {
            var key = KeyOf(item);
            var found = await store.GetItemAsync(AmendmentTable.Key(key.Pk, key.Sk), ct);
            if (found is null)
            {
                problems.Add($"missing item {key.Pk} / {key.Sk}");
                continue;
            }
            actual.Add(found);
            if (Canonical(found) != Canonical(item))
                problems.Add($"item differs from the seed: {key.Pk} / {key.Sk}");
        }

        var languages = await store.GetLanguagesAsync(ct);
        if (languages.Count != fixture.Languages.Count)
            problems.Add($"expected {fixture.Languages.Count} languages, found {languages.Count}");
        if (languages.Select(l => l.Code).Distinct().Count() != languages.Count)
            problems.Add("language codes are not unique");
        var settings = await store.GetSettingsAsync(ct);
        if (settings.Count != fixture.Settings.Count)
            problems.Add($"expected {fixture.Settings.Count} settings, found {settings.Count}");

        var known = languages.Select(l => l.Code).ToHashSet();
        foreach (var a in fixture.Amendments)
        {
            var stored = await store.GetAmendmentWithBodiesAsync(a.Id, ct);
            if (stored is null)
                continue;
            foreach (var body in stored.Bodies)
            {
                if (!known.Contains(body.Language))
                    problems.Add($"{a.Title}: body language '{body.Language}' is not a configured language");
                var seed = a.Bodies.FirstOrDefault(b => b.Language == body.Language.Value);
                if (seed is not null && body.PageCount != seed.PageCount)
                    problems.Add($"{a.Title}/{body.Language}: {body.PageCount} pages, expected {seed.PageCount}");
                if (SlideDeck.Split(body.Markdown).Any(string.IsNullOrWhiteSpace))
                    problems.Add($"{a.Title}/{body.Language}: contains an empty slide");
            }
        }

        var presentation = await store.GetPresentationAsync(ct);
        if (presentation.ActiveAmendmentId is { } activeId && await store.GetAmendmentWithBodiesAsync(activeId, ct) is { } active)
        {
            foreach (var (language, view) in presentation.Languages)
            {
                var body = active.Bodies.FirstOrDefault(b => b.Language == language);
                if (body is null || view.Page >= body.PageCount)
                    problems.Add($"presentation page {view.Page} is not valid for '{language}'");
            }
        }

        return new VerifyReport(expected.Count, actual.Count, Checksum(expected), Checksum(actual), problems);
    }

    /// <summary>SHA-256 over the canonical form of the items, ordered by key.</summary>
    public static string Checksum(IEnumerable<Item> items)
    {
        var text = string.Join("\n", items.Select(Canonical).Order(StringComparer.Ordinal));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    /// <summary>Stable text form of an item: attributes sorted by name, values tagged by type.</summary>
    public static string Canonical(Item item)
    {
        var sb = new StringBuilder();
        Append(sb, item);
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, Item map)
    {
        sb.Append('{');
        foreach (var (name, value) in map.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            sb.Append(System.Text.Json.JsonSerializer.Serialize(name)).Append(':');
            Append(sb, value);
            sb.Append(',');
        }
        sb.Append('}');
    }

    private static void Append(StringBuilder sb, AttributeValue value)
    {
        if (value.S is not null) sb.Append("S").Append(System.Text.Json.JsonSerializer.Serialize(value.S));
        else if (value.N is not null) sb.Append("N").Append(decimal.Parse(value.N, System.Globalization.CultureInfo.InvariantCulture).ToString(System.Globalization.CultureInfo.InvariantCulture));
        else if (value.BOOL is not null) sb.Append(value.BOOL.Value ? "BOOL:true" : "BOOL:false");
        else if (value.M is not null) { sb.Append('M'); Append(sb, value.M); }
        else if (value.NULL == true) sb.Append("NULL");
        else throw new NotSupportedException("Unsupported attribute type in seed item.");
    }

    private static (string Pk, string Sk) KeyOf(Item item) => (item[AmendmentTable.Pk].S!, item[AmendmentTable.Sk].S!);
}
