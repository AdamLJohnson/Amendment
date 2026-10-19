using System.Globalization;
using Amazon.DynamoDBv2.Model;
using Amendment.Domain;
using Amendment.Domain.Model;
using Amendment.Domain.Presentation;
using Item = System.Collections.Generic.Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>;

namespace Amendment.Infrastructure.DynamoDb;

/// <summary>
/// One conditional write inside <see cref="AmendmentStore.TransactAsync"/>. Each write carries the error to report
/// when its condition fails, so a cancelled transaction maps to a precise <see cref="DomainError"/>.
/// </summary>
public sealed class StoreWrite
{
    private const string VersionCondition = "attribute_exists(PK) AND #version = :expected";

    private StoreWrite(TransactWriteItem write, Item key, string description, Func<Item?, DomainError> onConditionFailed)
    {
        Write = write;
        Key = key;
        Description = description;
        OnConditionFailed = onConditionFailed;
    }

    internal TransactWriteItem Write { get; }
    internal Item Key { get; }
    public string Description { get; }

    /// <summary>True for writes and checks on the presentation item (a concurrent screen change; safe to re-plan and retry).</summary>
    public bool TargetsPresentation => Key[AmendmentTable.Pk].S == ItemKeys.PresentationPk;

    /// <summary>Maps the stored item (null when missing) to the error reported for a failed condition.</summary>
    internal Func<Item?, DomainError> OnConditionFailed { get; }

    public static StoreWrite NewAmendment(AmendmentRecord amendment) =>
        PutNew(ItemMapper.ToItem(amendment), amendment.Version, $"Amendment {amendment.Id}");

    public static StoreWrite UpdateAmendment(AmendmentRecord next) =>
        PutVersioned(ItemMapper.ToItem(next), next.Version, $"Amendment {next.Id}");

    public static StoreWrite DeleteAmendment(AmendmentRecord existing) =>
        DeleteVersioned(AmendmentTable.AmendmentKey(existing.Id), existing.Version, $"Amendment {existing.Id}");

    public static StoreWrite NewBody(AmendmentBodyRecord body) =>
        PutNew(ItemMapper.ToItem(body), body.Version, $"Body {body.AmendmentId}/{body.Language}",
            $"A body already exists for this amendment in language '{body.Language}'.");

    public static StoreWrite UpdateBody(AmendmentBodyRecord next) =>
        PutVersioned(ItemMapper.ToItem(next), next.Version, $"Body {next.AmendmentId}/{next.Language}");

    public static StoreWrite DeleteBody(AmendmentBodyRecord existing) =>
        DeleteVersioned(AmendmentTable.BodyKey(existing.AmendmentId, existing.Language), existing.Version,
            $"Body {existing.AmendmentId}/{existing.Language}");

    /// <summary>Saves the domain's next presentation state. A missing item counts as version 1.</summary>
    public static StoreWrite SavePresentation(PresentationState next)
    {
        RequireUpdateVersion(next.Version, "Presentation");
        var expected = next.Version - 1;
        var condition = expected == Versioning.InitialVersion ? "attribute_not_exists(PK) OR #version = :expected" : VersionCondition;
        var item = ItemMapper.ToItem(next);
        return new StoreWrite(
            new TransactWriteItem { Put = new Put { Item = item, ConditionExpression = condition, ExpressionAttributeNames = VersionName(), ExpressionAttributeValues = Expected(expected) } },
            AmendmentTable.PresentationKey(), "Presentation",
            current => VersionError("Presentation", current is null ? Versioning.InitialVersion : VersionOf(current), expected));
    }

    /// <summary>The amendment exists (without writing it).</summary>
    public static StoreWrite AmendmentExists(string amendmentId) => new(
        new TransactWriteItem { ConditionCheck = new ConditionCheck { Key = AmendmentTable.AmendmentKey(amendmentId), ConditionExpression = "attribute_exists(PK)" } },
        AmendmentTable.AmendmentKey(amendmentId), $"Amendment {amendmentId}",
        _ => DomainError.NotFound($"Amendment {amendmentId} was not found."));

    /// <summary>The referenced parent amendment exists; a missing parent is <c>PRECONDITION_FAILED</c> (contracts.md §3).</summary>
    public static StoreWrite ParentExists(string parentAmendmentId) => new(
        new TransactWriteItem { ConditionCheck = new ConditionCheck { Key = AmendmentTable.AmendmentKey(parentAmendmentId), ConditionExpression = "attribute_exists(PK)" } },
        AmendmentTable.AmendmentKey(parentAmendmentId), $"Amendment {parentAmendmentId}",
        _ => DomainError.Precondition($"Parent amendment {parentAmendmentId} was not found."));

    /// <summary>The amendment is not the presented one. Closes the gap between reading the presentation and writing.</summary>
    public static StoreWrite AmendmentNotPresented(string amendmentId) => new(
        new TransactWriteItem
        {
            ConditionCheck = new ConditionCheck
            {
                Key = AmendmentTable.PresentationKey(),
                ConditionExpression = "attribute_not_exists(PK) OR attribute_not_exists(#active) OR #active <> :id",
                ExpressionAttributeNames = new() { ["#active"] = "activeAmendmentId" },
                ExpressionAttributeValues = new() { [":id"] = new AttributeValue { S = amendmentId } }
            }
        },
        AmendmentTable.PresentationKey(), "Presentation",
        _ => DomainError.Precondition("The amendment is currently presented. Clear the presentation first."));

    private static StoreWrite PutNew(Item item, long version, string description, string? duplicateMessage = null)
    {
        if (version != Versioning.InitialVersion)
            throw new ArgumentException($"{description}: new items must have version {Versioning.InitialVersion}.");
        return new StoreWrite(
            new TransactWriteItem { Put = new Put { Item = item, ConditionExpression = "attribute_not_exists(PK)" } },
            KeyOf(item), description,
            _ => DomainError.Conflict(duplicateMessage ?? $"{description} already exists."));
    }

    private static StoreWrite PutVersioned(Item item, long nextVersion, string description)
    {
        RequireUpdateVersion(nextVersion, description);
        var expected = nextVersion - 1;
        return new StoreWrite(
            new TransactWriteItem { Put = new Put { Item = item, ConditionExpression = VersionCondition, ExpressionAttributeNames = VersionName(), ExpressionAttributeValues = Expected(expected) } },
            KeyOf(item), description,
            current => current is null ? DomainError.NotFound($"{description} was not found.") : VersionError(description, VersionOf(current), expected));
    }

    private static StoreWrite DeleteVersioned(Item key, long expected, string description) => new(
        new TransactWriteItem { Delete = new Delete { Key = key, ConditionExpression = VersionCondition, ExpressionAttributeNames = VersionName(), ExpressionAttributeValues = Expected(expected) } },
        key, description,
        current => current is null ? DomainError.NotFound($"{description} was not found.") : VersionError(description, VersionOf(current), expected));

    internal static DomainError VersionError(string description, long current, long expected) =>
        Versioning.Check(description, current, expected) ?? DomainError.Conflict($"{description} was changed by someone else.");

    internal static long VersionOf(Item item) => long.Parse(item["version"].N!, CultureInfo.InvariantCulture);

    private static void RequireUpdateVersion(long nextVersion, string description)
    {
        if (nextVersion <= Versioning.InitialVersion)
            throw new ArgumentException($"{description}: an update must carry a version greater than {Versioning.InitialVersion}.");
    }

    private static Item KeyOf(Item item) => AmendmentTable.Key(item[AmendmentTable.Pk].S!, item[AmendmentTable.Sk].S!);
    private static Dictionary<string, string> VersionName() => new() { ["#version"] = "version" };
    private static Item Expected(long expected) => new() { [":expected"] = new AttributeValue { N = expected.ToString(CultureInfo.InvariantCulture) } };
}
