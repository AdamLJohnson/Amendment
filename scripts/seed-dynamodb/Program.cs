using Amazon.DynamoDBv2;
using Amendment.Infrastructure.DynamoDb;
using Amendment.SeedDynamoDb;

const string usage = """
    Seeds AmendmentData-<stage> with the legacy seed data, or verifies a seeded table.

    Usage: dotnet run --project scripts/seed-dynamodb -- <seed|verify> --stage <stage> [options]

      --table <name>         Table name (default AmendmentData-<stage>)
      --region <region>      AWS region (default us-east-1)
      --endpoint-url <url>   DynamoDB endpoint, e.g. http://localhost:4566 for Floci (default: AWS_ENDPOINT_URL, else AWS)
      --fixture <path>       Seed fixture (default: seed-fixture.json next to the tool)
      --confirm-prod         Required for --stage prod

    Credentials come from the standard AWS chain (AWS_PROFILE, environment, SSO).
    """;

if (args.Length == 0 || args[0] is not ("seed" or "verify"))
{
    Console.Error.WriteLine(usage);
    return 2;
}

var command = args[0];
var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
for (var i = 1; i < args.Length; i++)
{
    if (!args[i].StartsWith("--", StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"Unexpected argument '{args[i]}'.\n\n{usage}");
        return 2;
    }
    var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
    options[args[i][2..]] = hasValue ? args[++i] : null;
}

if (!options.TryGetValue("stage", out var stage) || string.IsNullOrWhiteSpace(stage))
{
    Console.Error.WriteLine($"--stage is required.\n\n{usage}");
    return 2;
}
if (stage == "prod" && command == "seed" && !options.ContainsKey("confirm-prod"))
{
    Console.Error.WriteLine("Seeding PROD needs explicit approval: add --confirm-prod.");
    return 2;
}

var tableName = options.GetValueOrDefault("table") ?? AmendmentTable.NameForStage(stage);
var endpoint = options.GetValueOrDefault("endpoint-url") ?? Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL");
var config = new AmazonDynamoDBConfig
{
    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.GetValueOrDefault("region") ?? "us-east-1")
};
if (!string.IsNullOrEmpty(endpoint))
    config.ServiceURL = endpoint;

using var client = new AmazonDynamoDBClient(config);
var store = new AmendmentStore(client, tableName);
var fixture = SeedFixture.Load(options.GetValueOrDefault("fixture"));
Console.WriteLine($"{command}: table {tableName} ({(string.IsNullOrEmpty(endpoint) ? config.RegionEndpoint.SystemName : endpoint)})");

if (command == "seed")
{
    var report = await Seeder.SeedAsync(store, fixture);
    Console.WriteLine($"  written: {report.Written}, already present (left unchanged): {report.AlreadyPresent}");
    if (report.SampleSkipped)
        Console.WriteLine($"  sample amendments skipped: {report.SkipReason}");
    return 0;
}

var result = await SeedVerifier.VerifyAsync(store, fixture);
Console.WriteLine($"  items: {result.FoundItems}/{result.ExpectedItems}");
Console.WriteLine($"  expected checksum: {result.ExpectedChecksum}");
Console.WriteLine($"  actual checksum:   {result.ActualChecksum}");
foreach (var problem in result.Problems)
    Console.WriteLine($"  FAIL: {problem}");
Console.WriteLine(result.Passed ? "  PASS" : "  FAIL");
return result.Passed ? 0 : 1;
