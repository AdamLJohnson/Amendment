using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amendment.Infrastructure.DynamoDb;

namespace Amendment.Infrastructure.Tests;

/// <summary>
/// Integration tests need a DynamoDB endpoint:
/// <c>AMENDMENT_DYNAMODB_TEST_ENDPOINT=http://localhost:4566</c> (Floci, test credentials) or
/// <c>AMENDMENT_DYNAMODB_TEST_AWS=1</c> (real AWS through the standard credential chain, region us-east-1).
/// Without either, the tests are skipped.
/// </summary>
public sealed class DynamoDbFactAttribute : FactAttribute
{
    public DynamoDbFactAttribute()
    {
        if (!DynamoDbTestTable.Enabled)
            Skip = "Set AMENDMENT_DYNAMODB_TEST_ENDPOINT (Floci) or AMENDMENT_DYNAMODB_TEST_AWS=1 to run DynamoDB integration tests.";
    }
}

/// <summary>A temporary table with the production schema (<see cref="AmendmentTable.CreateTableRequest"/>), deleted afterwards.</summary>
public sealed class DynamoDbTestTable : IAsyncLifetime
{
    private static readonly string? Endpoint = Environment.GetEnvironmentVariable("AMENDMENT_DYNAMODB_TEST_ENDPOINT");
    private static readonly bool UseAws = Environment.GetEnvironmentVariable("AMENDMENT_DYNAMODB_TEST_AWS") == "1";

    public static bool Enabled => !string.IsNullOrEmpty(Endpoint) || UseAws;

    private IAmazonDynamoDB? _client;

    public IAmazonDynamoDB Client => _client ??= CreateClient();
    public string TableName { get; } = $"AmendmentData-test-{Guid.NewGuid():N}"[..40];

    public AmendmentStore Store => new(Client, TableName);

    public async Task InitializeAsync()
    {
        if (!Enabled)
            return;
        await Client.CreateTableAsync(AmendmentTable.CreateTableRequest(TableName,
            [new Tag { Key = "Application", Value = "Amendment" }, new Tag { Key = "Environment", Value = "test" }]));
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while ((await Client.DescribeTableAsync(TableName)).Table.TableStatus != TableStatus.ACTIVE)
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Table {TableName} did not become ACTIVE.");
            await Task.Delay(500);
        }
    }

    public async Task DisposeAsync()
    {
        if (_client is null)
            return;
        await _client.DeleteTableAsync(TableName);
        _client.Dispose();
    }

    private static IAmazonDynamoDB CreateClient()
    {
        var config = new AmazonDynamoDBConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        if (string.IsNullOrEmpty(Endpoint))
            return new AmazonDynamoDBClient(config);
        config.ServiceURL = Endpoint;
        return new AmazonDynamoDBClient(new BasicAWSCredentials("test", "test"), config);
    }
}
