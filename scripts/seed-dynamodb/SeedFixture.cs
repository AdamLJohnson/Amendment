using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using Amendment.Domain.Presentation;
using Amendment.Infrastructure.DynamoDb;
using Item = System.Collections.Generic.Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>;

namespace Amendment.SeedDynamoDb;

/// <summary>
/// The fixed seed (<c>seed-fixture.json</c>): the legacy seed from <c>AmendmentContext.OnModelCreating</c> and
/// <c>SeedDatabase.SeedSampleAmendment</c>, with the random Lorem text captured once so every run is identical.
/// </summary>
public sealed record SeedFixture
{
    public const string FileName = "seed-fixture.json";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public required DateTimeOffset SeededAt { get; init; }
    public required string SeededBy { get; init; }
    public required List<SeedLanguage> Languages { get; init; }
    public required List<SeedSetting> Settings { get; init; }
    public required List<SeedAmendment> Amendments { get; init; }
    public required SeedPresentation Presentation { get; init; }

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, FileName);

    public static SeedFixture Load(string? path = null) =>
        JsonSerializer.Deserialize<SeedFixture>(File.ReadAllText(path ?? DefaultPath), JsonOptions)
        ?? throw new InvalidDataException("The seed fixture is empty.");

    public AuditStamp Stamp(DateTimeOffset at) => new(SeededBy, at);

    public IEnumerable<LanguageRecord> LanguageRecords() =>
        Languages.Select(l => new LanguageRecord(LanguageCode.Parse(l.Code), l.Name, l.LegacyCode, l.LegacyId));

    public IEnumerable<SettingRecord> SettingRecords() => Settings.Select(s => new SettingRecord
    {
        Key = s.Key, Value = s.Value, Created = Stamp(SeededAt), Updated = Stamp(SeededAt)
    });

    public IEnumerable<AmendmentRecord> AmendmentRecords() => Amendments.Select(a => new AmendmentRecord
    {
        Id = a.Id,
        Title = a.Title,
        Author = a.Author,
        Motion = a.Motion,
        Source = a.Source,
        LegisId = a.LegisId,
        PrimaryLanguage = LanguageCode.Parse(a.PrimaryLanguage),
        Created = Stamp(a.CreatedAt),
        Updated = Stamp(a.CreatedAt)
    });

    public IEnumerable<AmendmentBodyRecord> BodyRecords() =>
        from a in Amendments
        from b in a.Bodies
        select new AmendmentBodyRecord
        {
            AmendmentId = a.Id,
            Language = LanguageCode.Parse(b.Language),
            Markdown = b.Markdown,
            Status = Enum.Parse<BodyStatus>(b.Status),
            Created = Stamp(a.CreatedAt),
            Updated = Stamp(a.CreatedAt)
        };

    public PresentationState PresentationRecord() => new()
    {
        ActiveAmendmentId = Presentation.ActiveAmendmentId,
        Languages = Presentation.Languages.ToImmutableDictionary(
            kv => LanguageCode.Parse(kv.Key), kv => new LanguageView(kv.Value.Visible, kv.Value.Page)),
        Updated = Stamp(SeededAt)
    };

    /// <summary>System items: languages and settings. Always seeded (legacy <c>HasData</c>).</summary>
    public IReadOnlyList<Item> SystemItems() =>
        [.. LanguageRecords().Select(ItemMapper.ToItem), .. SettingRecords().Select(ItemMapper.ToItem)];

    /// <summary>Sample content: amendments, bodies and the presentation. Seeded only into a table without other amendments.</summary>
    public IReadOnlyList<Item> SampleItems() =>
        [.. AmendmentRecords().Select(ItemMapper.ToItem), .. BodyRecords().Select(ItemMapper.ToItem), ItemMapper.ToItem(PresentationRecord())];

    public IReadOnlyList<Item> AllItems() => [.. SystemItems(), .. SampleItems()];
}

public sealed record SeedLanguage(string Code, string Name, string LegacyCode, int LegacyId);

public sealed record SeedSetting(string Key, string Value, int LegacyId);

public sealed record SeedAmendment
{
    public required string Id { get; init; }
    public required int LegacyId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public string? Motion { get; init; }
    public string? LegisId { get; init; }
    public string? Source { get; init; }
    public required string PrimaryLanguage { get; init; }
    public required List<SeedBody> Bodies { get; init; }
}

public sealed record SeedBody(string Language, string Status, int PageCount, string Markdown);

public sealed record SeedPresentation(string ActiveAmendmentId, Dictionary<string, SeedLanguageView> Languages);

public sealed record SeedLanguageView(bool Visible, int Page);
