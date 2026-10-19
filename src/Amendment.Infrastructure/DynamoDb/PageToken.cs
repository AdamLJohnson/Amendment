using System.Buffers.Text;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;

namespace Amendment.Infrastructure.DynamoDb;

/// <summary>
/// Opaque continuation token: base64url JSON of DynamoDB's <c>LastEvaluatedKey</c> (string attributes only).
/// A token is bound to the list it came from, so it cannot be replayed against another partition.
/// </summary>
public static class PageToken
{
    public static string? Encode(Dictionary<string, AttributeValue>? lastEvaluatedKey)
    {
        if (lastEvaluatedKey is null || lastEvaluatedKey.Count == 0)
            return null;
        var plain = lastEvaluatedKey.ToDictionary(kv => kv.Key, kv => kv.Value.S
            ?? throw new InvalidOperationException($"Key attribute '{kv.Key}' is not a string."));
        return Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(plain));
    }

    /// <summary>Returns null when the token is malformed or does not belong to <paramref name="expectedGsi1Pk"/>.</summary>
    public static Dictionary<string, AttributeValue>? Decode(string token, string expectedGsi1Pk)
    {
        Dictionary<string, string>? plain;
        try
        {
            plain = JsonSerializer.Deserialize<Dictionary<string, string>>(Base64Url.DecodeFromChars(token));
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return null;
        }

        string[] required = [AmendmentTable.Pk, AmendmentTable.Sk, AmendmentTable.Gsi1Pk, AmendmentTable.Gsi1Sk];
        if (plain is null || plain.Count != required.Length || !required.All(plain.ContainsKey) || plain[AmendmentTable.Gsi1Pk] != expectedGsi1Pk)
            return null;

        return plain.ToDictionary(kv => kv.Key, kv => new AttributeValue { S = kv.Value });
    }
}
