using System.Collections.Immutable;
using System.Globalization;
using Amazon.DynamoDBv2.Model;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using Amendment.Domain.Presentation;
using Item = System.Collections.Generic.Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>;

namespace Amendment.Infrastructure.DynamoDb;

/// <summary>Converts domain records to DynamoDB items and back. Attribute names are camelCase except the keys.</summary>
public static class ItemMapper
{
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

    public static Item ToItem(AmendmentRecord a)
    {
        var item = AmendmentTable.AmendmentKey(a.Id);
        item["entityType"] = S("Amendment");
        item["id"] = S(a.Id);
        item["title"] = S(a.Title);
        item["author"] = S(a.Author);
        SetOptional(item, "motion", a.Motion);
        SetOptional(item, "source", a.Source);
        SetOptional(item, "legisId", a.LegisId);
        item["primaryLanguage"] = S(a.PrimaryLanguage.Value);
        item["isArchived"] = new AttributeValue { BOOL = a.IsArchived };
        item["isApproved"] = new AttributeValue { BOOL = a.IsApproved };
        SetOptional(item, "parentAmendmentId", a.ParentAmendmentId);
        item["version"] = N(a.Version);
        SetAudit(item, a.Created, a.Updated);
        item[AmendmentTable.Gsi1Pk] = S(AmendmentTable.AmendmentListPk(a.IsArchived));
        item[AmendmentTable.Gsi1Sk] = S($"{FormatTimestamp(a.Created.At)}#{a.Id}");
        return item;
    }

    public static AmendmentRecord ToAmendment(Item item) => new()
    {
        Id = Str(item, "id"),
        Title = Str(item, "title"),
        Author = Str(item, "author"),
        Motion = OptStr(item, "motion"),
        Source = OptStr(item, "source"),
        LegisId = OptStr(item, "legisId"),
        PrimaryLanguage = LanguageCode.Parse(Str(item, "primaryLanguage")),
        IsArchived = Bool(item, "isArchived"),
        IsApproved = Bool(item, "isApproved"),
        ParentAmendmentId = OptStr(item, "parentAmendmentId"),
        Version = Long(item, "version"),
        Created = Audit(item, "created"),
        Updated = Audit(item, "updated")
    };

    public static Item ToItem(AmendmentBodyRecord b)
    {
        var item = AmendmentTable.BodyKey(b.AmendmentId, b.Language);
        item["entityType"] = S("AmendmentBody");
        item["amendmentId"] = S(b.AmendmentId);
        item["language"] = S(b.Language.Value);
        item["markdown"] = S(b.Markdown);
        item["status"] = S(b.Status.ToString());
        item["version"] = N(b.Version);
        SetAudit(item, b.Created, b.Updated);
        return item;
    }

    public static AmendmentBodyRecord ToBody(Item item) => new()
    {
        AmendmentId = Str(item, "amendmentId"),
        Language = LanguageCode.Parse(Str(item, "language")),
        Markdown = Str(item, "markdown"),
        Status = Enum.Parse<BodyStatus>(Str(item, "status")),
        Version = Long(item, "version"),
        Created = Audit(item, "created"),
        Updated = Audit(item, "updated")
    };

    public static Item ToItem(PresentationState p)
    {
        var item = AmendmentTable.PresentationKey();
        item["entityType"] = S("Presentation");
        SetOptional(item, "activeAmendmentId", p.ActiveAmendmentId);
        item["languages"] = new AttributeValue
        {
            M = p.Languages.ToDictionary(kv => kv.Key.Value, kv => new AttributeValue
            {
                M = new Item { ["visible"] = new AttributeValue { BOOL = kv.Value.Visible }, ["page"] = N(kv.Value.Page) }
            })
        };
        item["version"] = N(p.Version);
        if (p.Updated is { } u)
        {
            item["updatedBy"] = S(u.By);
            item["updatedAt"] = S(FormatTimestamp(u.At));
        }
        return item;
    }

    public static PresentationState ToPresentation(Item item) => new()
    {
        ActiveAmendmentId = OptStr(item, "activeAmendmentId"),
        Languages = (item.TryGetValue("languages", out var langs) && langs.M is { } map ? map : [])
            .ToImmutableDictionary(
                kv => LanguageCode.Parse(kv.Key),
                kv => new LanguageView(Bool(kv.Value.M, "visible"), (int)Long(kv.Value.M, "page"))),
        Version = Long(item, "version"),
        Updated = item.ContainsKey("updatedBy") ? Audit(item, "updated") : null
    };

    public static Item ToItem(LanguageRecord l)
    {
        var item = AmendmentTable.Key(AmendmentTable.SystemPk, AmendmentTable.LanguageSk(l.Code));
        item["entityType"] = S("Language");
        item["code"] = S(l.Code.Value);
        item["name"] = S(l.Name);
        item["legacyCode"] = S(l.LegacyCode);
        item["sortOrder"] = N(l.SortOrder);
        return item;
    }

    public static LanguageRecord ToLanguage(Item item) =>
        new(LanguageCode.Parse(Str(item, "code")), Str(item, "name"), Str(item, "legacyCode"), (int)Long(item, "sortOrder"));

    public static Item ToItem(SettingRecord s)
    {
        var item = AmendmentTable.Key(AmendmentTable.SystemPk, AmendmentTable.SettingSk(s.Key));
        item["entityType"] = S("Setting");
        item["key"] = S(s.Key);
        item["value"] = S(s.Value);
        item["version"] = N(s.Version);
        SetAudit(item, s.Created, s.Updated);
        return item;
    }

    public static SettingRecord ToSetting(Item item) => new()
    {
        Key = Str(item, "key"),
        Value = Str(item, "value"),
        Version = Long(item, "version"),
        Created = Audit(item, "created"),
        Updated = Audit(item, "updated")
    };

    public static string FormatTimestamp(DateTimeOffset value) =>
        value.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture);

    public static DateTimeOffset ParseTimestamp(string value) =>
        DateTimeOffset.ParseExact(value, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    private static AttributeValue S(string value) => new() { S = value };
    private static AttributeValue N(long value) => new() { N = value.ToString(CultureInfo.InvariantCulture) };

    private static void SetOptional(Item item, string name, string? value)
    {
        if (value is not null)
            item[name] = S(value);
    }

    private static void SetAudit(Item item, AuditStamp created, AuditStamp updated)
    {
        item["createdBy"] = S(created.By);
        item["createdAt"] = S(FormatTimestamp(created.At));
        item["updatedBy"] = S(updated.By);
        item["updatedAt"] = S(FormatTimestamp(updated.At));
    }

    private static AuditStamp Audit(Item item, string prefix) =>
        new(Str(item, prefix + "By"), ParseTimestamp(Str(item, prefix + "At")));

    private static string Str(Item item, string name) =>
        item.TryGetValue(name, out var v) && v.S is not null ? v.S : throw new InvalidDataException($"Item attribute '{name}' is missing or not a string.");

    private static string? OptStr(Item item, string name) => item.TryGetValue(name, out var v) ? v.S : null;

    private static bool Bool(Item item, string name) => item.TryGetValue(name, out var v) && v.BOOL == true;

    private static long Long(Item item, string name) =>
        item.TryGetValue(name, out var v) && v.N is not null
            ? long.Parse(v.N, NumberStyles.Integer, CultureInfo.InvariantCulture)
            : throw new InvalidDataException($"Item attribute '{name}' is missing or not a number.");
}
