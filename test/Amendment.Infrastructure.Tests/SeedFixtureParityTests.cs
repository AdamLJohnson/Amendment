using System.Text.Json;
using System.Text.RegularExpressions;
using Amendment.Domain.Model;
using Amendment.SeedDynamoDb;

namespace Amendment.Infrastructure.Tests;

/// <summary>
/// The checked-in fixture must match what the legacy application seeds. Text that the legacy seed generates
/// randomly (Lorem) is compared by shape: same lines, headings, slide breaks, sentences and word counts
/// (Lorem places commas randomly, so commas are ignored).
/// </summary>
public class SeedFixtureParityTests
{
    private static SeedFixture Fixture => SeedFixture.Load(LegacySeed.FixtureSourcePath);

    [Fact]
    public async Task Fixture_matches_the_legacy_seed()
    {
        var first = (await LegacySeed.RunAsync()).ToFixture();
        var second = (await LegacySeed.RunAsync()).ToFixture();

        Assert.Equal(first.Languages, Fixture.Languages);
        Assert.Equal(first.Settings, Fixture.Settings);
        Assert.Equal(first.Presentation.Languages, Fixture.Presentation.Languages);
        Assert.Equal(first.Amendments.Select(a => a.Id), Fixture.Amendments.Select(a => a.Id));
        Assert.Equal(first.Presentation.ActiveAmendmentId, Fixture.Presentation.ActiveAmendmentId);

        for (var i = 0; i < first.Amendments.Count; i++)
        {
            var (legacy, rerun, seed) = (first.Amendments[i], second.Amendments[i], Fixture.Amendments[i]);
            Assert.Equal(legacy with { Author = seed.Author, Bodies = seed.Bodies }, seed);
            AssertSame(legacy.Author, rerun.Author, seed.Author);

            Assert.Equal(legacy.Bodies.Select(b => (b.Language, b.Status, b.PageCount)), seed.Bodies.Select(b => (b.Language, b.Status, b.PageCount)));
            for (var b = 0; b < legacy.Bodies.Count; b++)
                AssertSame(legacy.Bodies[b].Markdown, rerun.Bodies[b].Markdown, seed.Bodies[b].Markdown);
        }
    }

    [Fact]
    public void Fixture_has_the_expected_legacy_content()
    {
        Assert.Equal(["en", "es", "fr"], Fixture.Languages.Select(l => l.Code));
        Assert.Equal(["ENG", "SPA", "FRA"], Fixture.Languages.Select(l => l.LegacyCode));
        Assert.Equal(["ShowDeafSigner", "ShowDeafSignerBox", "InvertedSlideText"], Fixture.Settings.Select(s => s.Key));
        Assert.All(Fixture.Settings, s => Assert.Equal("1", s.Value));

        Assert.Equal(
            ["Test Amendment 1", "Test Amendment 2", "Test Amendment 4", "Test Amendment 5", "Test Amendment 6", "Test Amendment 7", "Test Amendment 8", "Test Amendment 9"],
            Fixture.Amendments.Select(a => a.Title));
        Assert.Equal(Fixture.Amendments.Select(a => a.Id).Order(StringComparer.Ordinal), Fixture.Amendments.Select(a => a.Id));
        Assert.Equal(["en"], Fixture.Amendments[0].Bodies.Select(b => b.Language));
        Assert.All(Fixture.Amendments.Skip(1), a => Assert.Equal(["en", "es", "fr"], a.Bodies.Select(b => b.Language)));
        Assert.All(Fixture.Amendments[1].Bodies, b => Assert.Equal(3, SlideDeck.PageCount(b.Markdown)));
        Assert.All(Fixture.Amendments.SelectMany(a => a.Bodies), b => Assert.Equal(b.PageCount, SlideDeck.PageCount(b.Markdown)));

        var live = Fixture.Amendments.Single(a => a.Id == Fixture.Presentation.ActiveAmendmentId);
        Assert.Equal("Test Amendment 2", live.Title);
        Assert.All(Fixture.Presentation.Languages.Values, v => Assert.Equal(new SeedLanguageView(true, 0), v));
    }

    [Fact]
    public void Fixture_items_are_stable() =>
        Assert.Equal(SeedVerifier.Checksum(Fixture.AllItems()), SeedVerifier.Checksum(SeedFixture.Load(LegacySeed.FixtureSourcePath).AllItems()));

    [Fact]
    public void Fixture_markdown_uses_lf_line_endings() =>
        Assert.All(Fixture.Amendments.SelectMany(a => a.Bodies), b => Assert.DoesNotContain('\r', b.Markdown));

    /// <summary>Deterministic legacy values must match exactly; random ones must match in shape.</summary>
    private static void AssertSame(string legacy, string rerun, string seed)
    {
        if (legacy == rerun)
            Assert.Equal(legacy, seed);
        else
            Assert.Equal(Shape(legacy), Shape(seed));
    }

    private static readonly Regex Word = new(@"\p{L}+", RegexOptions.CultureInvariant);

    private static string Shape(string text) => string.Join("\n", text.Split('\n').Select(line =>
        line.StartsWith('#') || line.Trim() == SlideDeck.Delimiter ? line : Word.Replace(line.Replace(",", ""), "w")));

    /// <summary>Regenerates <c>seed-fixture.json</c> from the legacy seed. Run only when the legacy seed changes.</summary>
    [RegenerateFixtureFact]
    public async Task Regenerate_fixture_from_the_legacy_seed()
    {
        var fixture = (await LegacySeed.RunAsync()).ToFixture();
        await File.WriteAllTextAsync(LegacySeed.FixtureSourcePath, JsonSerializer.Serialize(fixture, SeedFixture.JsonOptions) + "\n");
    }
}

public sealed class RegenerateFixtureFactAttribute : FactAttribute
{
    public const string Variable = "AMENDMENT_REGENERATE_SEED_FIXTURE";

    public RegenerateFixtureFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(Variable) != "1")
            Skip = $"Set {Variable}=1 to regenerate the seed fixture.";
    }
}
