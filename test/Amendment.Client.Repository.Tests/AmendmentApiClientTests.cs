using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Amendment.Client.Repository.GraphQl;

namespace Amendment.Client.Repository.Tests;

/// <summary>Offline tests of the typed Blazor GraphQL client against canned AppSync responses.</summary>
public sealed class AmendmentApiClientTests
{
    private sealed class CannedHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public JsonObject? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!.AsObject();
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static (AmendmentApiClient Client, CannedHandler Handler) Create(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new CannedHandler(status, body);
        return (new AmendmentApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/graphql") }), handler);
    }

    [Fact]
    public async Task Reads_typed_amendments_with_bodies_and_string_versions()
    {
        const string audit = """{"by":"u1","at":"2026-10-19T10:00:00Z"}""";
        var (client, handler) = Create("""
            {"data":{"amendments":{"nextToken":"t2","items":[{"id":"01J","title":"T","author":"A","motion":null,"source":null,"legisId":null,
            "primaryLanguage":"en","isArchived":false,"isApproved":true,"parentAmendmentId":null,"version":"9007199254740993",
            "created":AUDIT,"updated":AUDIT,"bodies":[{"amendmentId":"01J","language":"en","markdown":"Hi","status":"READY",
            "pageCount":1,"version":"2","created":AUDIT,"updated":AUDIT}]}]}}}
            """.Replace("AUDIT", audit));

        var page = await client.GetAmendmentsAsync(archived: true, limit: 10, nextToken: "t1");

        Assert.Equal("t2", page.NextToken);
        var amendment = Assert.Single(page.Items);
        Assert.Equal("9007199254740993", amendment.Version);
        Assert.True(amendment.IsApproved);
        Assert.Equal(new DateTimeOffset(2026, 10, 19, 10, 0, 0, TimeSpan.Zero), amendment.Created.At);
        var body = Assert.Single(amendment.Bodies);
        Assert.Equal((ApiBodyStatus.Ready, "Hi", "2"), (body.Status, body.Markdown, body.Version));
        Assert.Equal("""{"archived":true,"limit":10,"nextToken":"t1"}""", handler.LastRequest!["variables"]!.ToJsonString());
    }

    [Fact]
    public async Task Missing_amendment_is_null()
    {
        var (client, _) = Create("""{"data":{"amendment":null}}""");
        Assert.Null(await client.GetAmendmentAsync("nope"));
    }

    [Fact]
    public async Task GraphQL_errors_keep_the_error_type_and_field()
    {
        var (client, handler) = Create("""
            {"data":{"updateAmendmentBody":null},"errors":[{"errorType":"CONFLICT","message":"The body was changed by someone else.","errorInfo":{"field":"expectedVersion"}}]}
            """);

        var ex = await Assert.ThrowsAsync<AmendmentApiException>(() => client.UpdateBodyAsync("01J", "en", "1", "x", ApiBodyStatus.Draft));

        Assert.Equal((AmendmentApiException.Conflict, "expectedVersion"), (ex.ErrorType, ex.Field));
        Assert.Equal("The body was changed by someone else.", ex.Message);
        Assert.Equal("DRAFT", (string)handler.LastRequest!["variables"]!["status"]!);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Rejected_tokens_are_unauthorized(HttpStatusCode status)
    {
        var (client, _) = Create("""{"errors":[{"errorType":"UnauthorizedException","message":"Valid authorization header not provided."}]}""", status);
        var ex = await Assert.ThrowsAsync<AmendmentApiException>(() => client.GetLanguagesAsync());
        Assert.Equal(AmendmentApiException.Unauthorized, ex.ErrorType);
    }

    [Fact]
    public async Task Approval_sends_the_failure_mode_enum()
    {
        var (client, handler) = Create("""{"data":{"approveAmendment":null},"errors":[{"errorType":"FORBIDDEN","message":"No."}]}""");
        await Assert.ThrowsAsync<AmendmentApiException>(() => client.ApproveAmendmentAsync("01J", "3", bestEffort: true));
        Assert.Equal("BEST_EFFORT", (string)handler.LastRequest!["variables"]!["failureMode"]!);
    }
}
