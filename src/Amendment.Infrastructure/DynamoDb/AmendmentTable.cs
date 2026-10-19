using Amazon.DynamoDBv2.Model;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;

namespace Amendment.Infrastructure.DynamoDb;

/// <summary>
/// Single-table layout of <c>AmendmentData-{stage}</c> (plan §4). Must match <c>DataStack</c> in <c>infra/Amendment.Cdk</c>.
/// </summary>
public static class AmendmentTable
{
    public static string NameForStage(string stage) => $"AmendmentData-{stage}";

    public const string Pk = "PK";
    public const string Sk = "SK";

    /// <summary>Amendment listing in creation order. Only <c>META</c> items carry these attributes.</summary>
    public const string Gsi1 = "GSI1";
    public const string Gsi1Pk = "GSI1PK";
    public const string Gsi1Sk = "GSI1SK";

    public const string SystemPk = "SYSTEM";
    public const string LanguageSkPrefix = "LANGUAGE#";
    public const string SettingSkPrefix = "SETTING#";
    public const string BodySkPrefix = "BODY#";

    public static string LanguageSk(LanguageCode code) => LanguageSkPrefix + code.Value;
    public static string SettingSk(string key) => SettingSkPrefix + key;
    public static string AmendmentListPk(bool archived) => archived ? "AMENDMENTS#ARCHIVED" : "AMENDMENTS#ACTIVE";

    public static Dictionary<string, AttributeValue> AmendmentKey(string amendmentId) =>
        Key(ItemKeys.AmendmentPk(amendmentId), ItemKeys.MetaSk);

    public static Dictionary<string, AttributeValue> BodyKey(string amendmentId, LanguageCode language) =>
        Key(ItemKeys.AmendmentPk(amendmentId), ItemKeys.BodySk(language));

    public static Dictionary<string, AttributeValue> PresentationKey() => Key(ItemKeys.PresentationPk, ItemKeys.StateSk);

    public static Dictionary<string, AttributeValue> Key(string pk, string sk) => new()
    {
        [Pk] = new AttributeValue { S = pk },
        [Sk] = new AttributeValue { S = sk }
    };

    /// <summary>Table definition used by integration tests and local emulators; mirrors <c>DataStack</c>.</summary>
    public static CreateTableRequest CreateTableRequest(string tableName, IEnumerable<Tag>? tags = null) => new()
    {
        TableName = tableName,
        BillingMode = Amazon.DynamoDBv2.BillingMode.PAY_PER_REQUEST,
        AttributeDefinitions =
        [
            new AttributeDefinition(Pk, Amazon.DynamoDBv2.ScalarAttributeType.S),
            new AttributeDefinition(Sk, Amazon.DynamoDBv2.ScalarAttributeType.S),
            new AttributeDefinition(Gsi1Pk, Amazon.DynamoDBv2.ScalarAttributeType.S),
            new AttributeDefinition(Gsi1Sk, Amazon.DynamoDBv2.ScalarAttributeType.S)
        ],
        KeySchema =
        [
            new KeySchemaElement(Pk, Amazon.DynamoDBv2.KeyType.HASH),
            new KeySchemaElement(Sk, Amazon.DynamoDBv2.KeyType.RANGE)
        ],
        GlobalSecondaryIndexes =
        [
            new GlobalSecondaryIndex
            {
                IndexName = Gsi1,
                KeySchema =
                [
                    new KeySchemaElement(Gsi1Pk, Amazon.DynamoDBv2.KeyType.HASH),
                    new KeySchemaElement(Gsi1Sk, Amazon.DynamoDBv2.KeyType.RANGE)
                ],
                Projection = new Projection { ProjectionType = Amazon.DynamoDBv2.ProjectionType.ALL }
            }
        ],
        Tags = tags?.ToList()
    };
}
