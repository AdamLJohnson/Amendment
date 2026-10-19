using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amendment.Api.Lambda;
using Amendment.Domain;
using Amendment.Domain.Authorization;
using Amendment.Infrastructure.DynamoDb;

namespace Amendment.Infrastructure.Tests;

/// <summary>Write-API contract checks that need no database.</summary>
public class ApiContractTests
{
    [Fact]
    public void Cognito_groups_map_to_domain_roles_and_unknown_groups_are_ignored()
    {
        var principal = CallerPrincipal.From(new ApiIdentity("sub-1", ["AmendmentEditor", "Translator", "Hacker", "System Administrator"]));
        Assert.Equal("sub-1", principal.UserId);
        Assert.Equal([Roles.AmendmentEditor, Roles.Translator], principal.Roles.Order());

        Assert.False(CallerPrincipal.From(null).IsAuthenticated);
        Assert.False(CallerPrincipal.From(new ApiIdentity(" ", ["SystemAdministrator"])).IsAuthenticated);
    }

    [Fact]
    public void Every_cognito_group_maps_to_a_known_role()
    {
        Assert.Equal(Roles.All.Order(), CallerPrincipal.GroupToRole.Values.Order());
    }

    [Fact]
    public async Task Unexpected_failures_return_a_generic_internal_error()
    {
        // Points at a closed port: the store call fails after authorization succeeds.
        var client = new AmazonDynamoDBClient(new BasicAWSCredentials("x", "x"), new AmazonDynamoDBConfig { ServiceURL = "http://127.0.0.1:9", MaxErrorRetry = 0, Timeout = TimeSpan.FromSeconds(2) });
        var store = new AmendmentStore(client, "missing");
        var function = new Function(new AmendmentApi(store, new LanguageProvider(store, TimeProvider.System), TimeProvider.System));

        var response = await function.HandleAsync(
            new ApiRequest("updateAmendment", JsonSerializer.SerializeToElement(new { id = "a", expectedVersion = 1 }), ApiTestClient.Editor),
            new TestContext());

        Assert.Equal("INTERNAL_ERROR", response.Error!.Type);
        Assert.Contains("req-1", response.Error.Message);
        Assert.DoesNotContain("127.0.0.1", response.Error.Message);
    }

    [Fact]
    public void Errors_list_every_validation_problem_with_wire_names()
    {
        var response = ApiResponse.Fail([DomainError.Validation("title", "Title is required."), DomainError.Validation("author", "Author is required.")]);
        Assert.Equal(("VALIDATION_ERROR", "title"), (response.Error!.Type, response.Error.Field));
        Assert.Equal(["title", "author"], response.Error.Errors.Select(e => e.Field));
    }

    private sealed class TestContext : ILambdaContext
    {
        public string AwsRequestId => "req-1";
        public IClientContext ClientContext => null!;
        public string FunctionName => "test";
        public string FunctionVersion => "1";
        public ICognitoIdentity Identity => null!;
        public string InvokedFunctionArn => "arn";
        public ILambdaLogger Logger { get; } = new NullLogger();
        public string LogGroupName => "log";
        public string LogStreamName => "stream";
        public int MemoryLimitInMB => 512;
        public TimeSpan RemainingTime => TimeSpan.FromSeconds(30);
    }

    private sealed class NullLogger : ILambdaLogger
    {
        public void Log(string message) { }
        public void LogLine(string message) { }
    }
}
