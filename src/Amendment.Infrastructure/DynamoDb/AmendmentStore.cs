using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amendment.Domain;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using Amendment.Domain.Presentation;
using Item = System.Collections.Generic.Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>;

namespace Amendment.Infrastructure.DynamoDb;

/// <summary>
/// Persistence for amendments, bodies, presentation, languages and settings in <c>AmendmentData-{stage}</c>.
/// Records passed to create methods must have version 1. Records passed to update methods are the domain's
/// next state (version already incremented); the write succeeds only if the stored version is one lower.
/// </summary>
public sealed class AmendmentStore(IAmazonDynamoDB client, string tableName)
{
    public const int MaxPageSize = 100;
    public const int MaxTransactionItems = 100;
    private const string VersionCondition = "attribute_exists(PK) AND #version = :expected";
    private const string NotExistsCondition = "attribute_not_exists(PK)";

    public string TableName { get; } = tableName;

    public async Task<DomainResult<AmendmentRecord>> CreateAmendmentAsync(AmendmentRecord amendment, CancellationToken ct = default)
    {
        RequireInitialVersion(amendment.Version);
        if (await TryPutNewItemAsync(ItemMapper.ToItem(amendment), ct))
            return DomainResult<AmendmentRecord>.Success(amendment);
        return DomainError.Conflict($"Amendment {amendment.Id} already exists.");
    }

    public async Task<AmendmentRecord?> GetAmendmentAsync(string id, CancellationToken ct = default)
    {
        var item = await GetItemAsync(AmendmentTable.AmendmentKey(id), ct);
        return item is null ? null : ItemMapper.ToAmendment(item);
    }

    /// <summary>Metadata and every body of one amendment in a single strongly consistent query.</summary>
    public async Task<AmendmentWithBodies?> GetAmendmentWithBodiesAsync(string id, CancellationToken ct = default)
    {
        var items = await QueryAllAsync(ItemKeys.AmendmentPk(id), skPrefix: null, ct);
        var meta = items.FirstOrDefault(i => i[AmendmentTable.Sk].S == ItemKeys.MetaSk);
        if (meta is null)
            return null;
        var bodies = items.Where(i => i[AmendmentTable.Sk].S!.StartsWith(AmendmentTable.BodySkPrefix, StringComparison.Ordinal))
            .Select(ItemMapper.ToBody).ToList();
        return new AmendmentWithBodies(ItemMapper.ToAmendment(meta), bodies);
    }

    public async Task<DomainResult<AmendmentRecord>> UpdateAmendmentAsync(AmendmentRecord next, CancellationToken ct = default)
    {
        var error = await PutVersionedAsync(ItemMapper.ToItem(next), next.Version, $"Amendment {next.Id}", ct);
        return error ?? DomainResult<AmendmentRecord>.Success(next);
    }

    /// <summary>Active or archived amendments in creation order, using GSI1 (no table scan).</summary>
    public async Task<DomainResult<Page<AmendmentRecord>>> ListAmendmentsAsync(bool archived, int pageSize, string? pageToken, CancellationToken ct = default)
    {
        if (pageSize is < 1 or > MaxPageSize)
            return DomainError.Validation("pageSize", $"Page size must be between 1 and {MaxPageSize}.");

        var partition = AmendmentTable.AmendmentListPk(archived);
        Item? startKey = null;
        if (pageToken is not null && (startKey = PageToken.Decode(pageToken, partition)) is null)
            return DomainError.Validation("pageToken", "The page token is invalid.");

        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = TableName,
            IndexName = AmendmentTable.Gsi1,
            KeyConditionExpression = "#pk = :pk",
            ExpressionAttributeNames = new() { ["#pk"] = AmendmentTable.Gsi1Pk },
            ExpressionAttributeValues = new() { [":pk"] = new AttributeValue { S = partition } },
            Limit = pageSize,
            ExclusiveStartKey = startKey
        }, ct);

        var items = (response.Items ?? []).Select(ItemMapper.ToAmendment).ToList();
        return DomainResult<Page<AmendmentRecord>>.Success(new Page<AmendmentRecord>(items, PageToken.Encode(response.LastEvaluatedKey)));
    }

    /// <summary>
    /// Creates a body only if its amendment exists and no body exists for that language (one transaction).
    /// </summary>
    public async Task<DomainResult<AmendmentBodyRecord>> CreateBodyAsync(AmendmentBodyRecord body, CancellationToken ct = default)
    {
        RequireInitialVersion(body.Version);
        try
        {
            await client.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems =
                [
                    new TransactWriteItem
                    {
                        ConditionCheck = new ConditionCheck
                        {
                            TableName = TableName,
                            Key = AmendmentTable.AmendmentKey(body.AmendmentId),
                            ConditionExpression = "attribute_exists(PK)"
                        }
                    },
                    new TransactWriteItem
                    {
                        Put = new Put { TableName = TableName, Item = ItemMapper.ToItem(body), ConditionExpression = NotExistsCondition }
                    }
                ]
            }, ct);
            return DomainResult<AmendmentBodyRecord>.Success(body);
        }
        catch (TransactionCanceledException)
        {
            if (await GetItemAsync(AmendmentTable.AmendmentKey(body.AmendmentId), ct) is null)
                return DomainError.NotFound($"Amendment {body.AmendmentId} was not found.");
            return DomainError.Conflict($"A body already exists for this amendment in language '{body.Language}'.");
        }
    }

    public async Task<AmendmentBodyRecord?> GetBodyAsync(string amendmentId, LanguageCode language, CancellationToken ct = default)
    {
        var item = await GetItemAsync(AmendmentTable.BodyKey(amendmentId, language), ct);
        return item is null ? null : ItemMapper.ToBody(item);
    }

    public async Task<DomainResult<AmendmentBodyRecord>> UpdateBodyAsync(AmendmentBodyRecord next, CancellationToken ct = default)
    {
        var error = await PutVersionedAsync(ItemMapper.ToItem(next), next.Version, $"Body {next.AmendmentId}/{next.Language}", ct);
        return error ?? DomainResult<AmendmentBodyRecord>.Success(next);
    }

    /// <summary>The stored presentation, or <see cref="PresentationState.Empty"/> (version 1) when none is stored yet.</summary>
    public async Task<PresentationState> GetPresentationAsync(CancellationToken ct = default)
    {
        var item = await GetItemAsync(AmendmentTable.PresentationKey(), ct);
        return item is null ? PresentationState.Empty : ItemMapper.ToPresentation(item);
    }

    /// <summary>Saves the domain's next presentation state. A missing item counts as version 1.</summary>
    public async Task<DomainResult<PresentationState>> SavePresentationAsync(PresentationState next, CancellationToken ct = default)
    {
        var error = await PutVersionedAsync(ItemMapper.ToItem(next), next.Version, "Presentation", ct, missingCountsAsInitial: true);
        return error ?? DomainResult<PresentationState>.Success(next);
    }

    /// <summary>
    /// Commits all writes atomically (<c>TransactWriteItems</c>) or none. A failed condition is reported with the
    /// error of the write that failed; a concurrent transaction on the same items is a <c>Conflict</c>.
    /// </summary>
    public async Task<TransactionResult> TransactAsync(IReadOnlyList<StoreWrite> writes, CancellationToken ct = default)
    {
        if (writes.Count == 0)
            return TransactionResult.Committed;
        if (writes.Count > MaxTransactionItems)
            return new(DomainError.Precondition($"The change touches {writes.Count} items; at most {MaxTransactionItems} can be written together."), null);

        var items = writes.Select(w => w.Write).ToList();
        foreach (var item in items)
        {
            if (item.Put is { } put) { put.TableName = TableName; put.ReturnValuesOnConditionCheckFailure = ReturnValuesOnConditionCheckFailure.ALL_OLD; }
            if (item.Delete is { } del) { del.TableName = TableName; del.ReturnValuesOnConditionCheckFailure = ReturnValuesOnConditionCheckFailure.ALL_OLD; }
            if (item.ConditionCheck is { } check) { check.TableName = TableName; check.ReturnValuesOnConditionCheckFailure = ReturnValuesOnConditionCheckFailure.ALL_OLD; }
        }

        try
        {
            await client.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = items }, ct);
            return TransactionResult.Committed;
        }
        catch (TransactionCanceledException ex)
        {
            var reasons = ex.CancellationReasons ?? [];
            for (var i = 0; i < reasons.Count && i < writes.Count; i++)
            {
                switch (reasons[i].Code)
                {
                    case "ConditionalCheckFailed":
                        var current = reasons[i].Item is { Count: > 0 } old ? old : await GetItemAsync(writes[i].Key, ct);
                        return new(writes[i].OnConditionFailed(current), writes[i]);
                    case "TransactionConflict":
                        return new(DomainError.Conflict($"{writes[i].Description} is being changed by someone else. Reload and try again."), writes[i]);
                    case "ThrottlingError" or "ProvisionedThroughputExceeded":
                        return new(new DomainError(DomainErrorCode.RateLimited, "The service is busy. Try again shortly."), writes[i]);
                }
            }
            return new(DomainError.Conflict("The change could not be saved because the data changed. Reload and try again."), null);
        }
    }

    /// <summary>
    /// Removes any items left in a deleted amendment's partition (for example a body created while the delete was in
    /// flight). Call only after the amendment's <c>META</c> item was deleted.
    /// </summary>
    public async Task DeleteAmendmentPartitionAsync(string amendmentId, CancellationToken ct = default)
    {
        foreach (var item in await QueryAllAsync(ItemKeys.AmendmentPk(amendmentId), skPrefix: null, ct))
        {
            await client.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = TableName,
                Key = AmendmentTable.Key(item[AmendmentTable.Pk].S!, item[AmendmentTable.Sk].S!)
            }, ct);
        }
    }

    public async Task<IReadOnlyList<LanguageRecord>> GetLanguagesAsync(CancellationToken ct = default) =>
        (await QueryAllAsync(AmendmentTable.SystemPk, AmendmentTable.LanguageSkPrefix, ct))
            .Select(ItemMapper.ToLanguage).OrderBy(l => l.SortOrder).ToList();

    public async Task<IReadOnlyList<SettingRecord>> GetSettingsAsync(CancellationToken ct = default) =>
        (await QueryAllAsync(AmendmentTable.SystemPk, AmendmentTable.SettingSkPrefix, ct))
            .Select(ItemMapper.ToSetting).ToList();

    /// <summary>Writes the item only if no item with its key exists. Returns false when it already exists.</summary>
    public async Task<bool> TryPutNewItemAsync(Item item, CancellationToken ct = default)
    {
        try
        {
            await client.PutItemAsync(new PutItemRequest { TableName = TableName, Item = item, ConditionExpression = NotExistsCondition }, ct);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task<Item?> GetItemAsync(Item key, CancellationToken ct = default)
    {
        var response = await client.GetItemAsync(new GetItemRequest { TableName = TableName, Key = key, ConsistentRead = true }, ct);
        return response.Item is { Count: > 0 } item ? item : null;
    }

    /// <summary>All items of one partition (optionally filtered by sort-key prefix), following every page.</summary>
    public async Task<List<Item>> QueryAllAsync(string pk, string? skPrefix, CancellationToken ct = default)
    {
        var request = new QueryRequest
        {
            TableName = TableName,
            ConsistentRead = true,
            KeyConditionExpression = skPrefix is null ? "#pk = :pk" : "#pk = :pk AND begins_with(#sk, :prefix)",
            ExpressionAttributeNames = new() { ["#pk"] = AmendmentTable.Pk },
            ExpressionAttributeValues = new() { [":pk"] = new AttributeValue { S = pk } }
        };
        if (skPrefix is not null)
        {
            request.ExpressionAttributeNames["#sk"] = AmendmentTable.Sk;
            request.ExpressionAttributeValues[":prefix"] = new AttributeValue { S = skPrefix };
        }

        var items = new List<Item>();
        do
        {
            var response = await client.QueryAsync(request, ct);
            items.AddRange(response.Items ?? []);
            request.ExclusiveStartKey = response.LastEvaluatedKey is { Count: > 0 } last ? last : null;
        } while (request.ExclusiveStartKey is not null);
        return items;
    }

    /// <summary>
    /// Ids of every amendment, from a strongly consistent table scan. For tools such as the seeder that must see
    /// writes immediately; GSI1 is eventually consistent. Not for request paths.
    /// </summary>
    public async Task<List<string>> ScanAmendmentIdsAsync(CancellationToken ct = default)
    {
        var request = new ScanRequest
        {
            TableName = TableName,
            ConsistentRead = true,
            FilterExpression = "begins_with(#pk, :prefix) AND #sk = :meta",
            ProjectionExpression = "#pk",
            ExpressionAttributeNames = new() { ["#pk"] = AmendmentTable.Pk, ["#sk"] = AmendmentTable.Sk },
            ExpressionAttributeValues = new()
            {
                [":prefix"] = new AttributeValue { S = ItemKeys.AmendmentPk("") },
                [":meta"] = new AttributeValue { S = ItemKeys.MetaSk }
            }
        };

        var ids = new List<string>();
        do
        {
            var response = await client.ScanAsync(request, ct);
            ids.AddRange((response.Items ?? []).Select(i => i[AmendmentTable.Pk].S![ItemKeys.AmendmentPk("").Length..]));
            request.ExclusiveStartKey = response.LastEvaluatedKey is { Count: > 0 } last ? last : null;
        } while (request.ExclusiveStartKey is not null);
        return ids;
    }

    private async Task<DomainError?> PutVersionedAsync(Item item, long nextVersion, string description, CancellationToken ct, bool missingCountsAsInitial = false)
    {
        if (nextVersion <= Versioning.InitialVersion)
            throw new ArgumentException($"{description}: an update must carry a version greater than {Versioning.InitialVersion}.");

        var expected = nextVersion - 1;
        var allowMissing = missingCountsAsInitial && expected == Versioning.InitialVersion;
        try
        {
            await client.PutItemAsync(new PutItemRequest
            {
                TableName = TableName,
                Item = item,
                ConditionExpression = allowMissing ? "attribute_not_exists(PK) OR #version = :expected" : VersionCondition,
                ExpressionAttributeNames = new() { ["#version"] = "version" },
                ExpressionAttributeValues = new() { [":expected"] = new AttributeValue { N = expected.ToString(System.Globalization.CultureInfo.InvariantCulture) } }
            }, ct);
            return null;
        }
        catch (ConditionalCheckFailedException)
        {
            var key = AmendmentTable.Key(item[AmendmentTable.Pk].S!, item[AmendmentTable.Sk].S!);
            var current = await GetItemAsync(key, ct);
            if (current is null)
                return missingCountsAsInitial
                    ? Versioning.Check(description, Versioning.InitialVersion, expected) ?? DomainError.Conflict($"{description} was changed by someone else.")
                    : DomainError.NotFound($"{description} was not found.");
            var currentVersion = long.Parse(current["version"].N!, System.Globalization.CultureInfo.InvariantCulture);
            return Versioning.Check(description, currentVersion, expected) ?? DomainError.Conflict($"{description} was changed by someone else.");
        }
    }

    private static void RequireInitialVersion(long version)
    {
        if (version != Versioning.InitialVersion)
            throw new ArgumentException($"New items must have version {Versioning.InitialVersion}.");
    }
}
