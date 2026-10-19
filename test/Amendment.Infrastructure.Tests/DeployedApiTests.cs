using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Amendment.Client.Repository.GraphQl;

namespace Amendment.Infrastructure.Tests;

/// <summary>
/// Runs only when <c>infra/scripts/test-api-dev.ps1</c> supplies the deployed GraphQL URL and access tokens of the
/// throwaway per-role Cognito users (decision D18).
/// </summary>
public sealed class DeployedApiFactAttribute : FactAttribute
{
    public DeployedApiFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DeployedApi.UrlVariable)))
            Skip = $"Run infra/scripts/test-api-dev.ps1 (sets {DeployedApi.UrlVariable}) to run deployed API tests.";
    }
}

/// <summary>GraphQL calls against the deployed AppSync API, signed in as one of the test users.</summary>
public sealed class DeployedApi
{
    public const string UrlVariable = "AMENDMENT_API_TEST_URL";
    private static readonly HttpClient Http = new();

    public static string Token(string role) =>
        Environment.GetEnvironmentVariable($"AMENDMENT_API_TEST_TOKEN_{role}") ?? throw new InvalidOperationException($"No test token for {role}.");

    public static async Task<(HttpStatusCode Status, JsonNode? Body)> Send(string? role, string query, object? variables = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Environment.GetEnvironmentVariable(UrlVariable))
        {
            Content = JsonContent.Create(new { query, variables })
        };
        if (role is not null)
            request.Headers.TryAddWithoutValidation("Authorization", Token(role));
        using var response = await Http.SendAsync(request);
        return (response.StatusCode, JsonNode.Parse(await response.Content.ReadAsStringAsync()));
    }

    /// <summary>The typed Blazor client, signed in as <paramref name="role"/>.</summary>
    public static AmendmentApiClient Client(string role)
    {
        var http = new HttpClient { BaseAddress = new Uri(Environment.GetEnvironmentVariable(UrlVariable)!) };
        http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", Token(role));
        return new AmendmentApiClient(http);
    }

    public static async Task<JsonNode> Ok(string role, string query, object? variables = null)
    {
        var (status, body) = await Send(role, query, variables);
        var errors = body?["errors"];
        Assert.True(status == HttpStatusCode.OK && errors is null, $"{status}: {errors?.ToJsonString()}");
        return body!["data"]!;
    }

    public static async Task<string> ErrorType(string role, string query, object? variables = null)
    {
        var (_, body) = await Send(role, query, variables);
        Assert.Null(body?["data"]?.AsObject().FirstOrDefault().Value);
        return (string)body!["errors"]![0]!["errorType"]!;
    }
}

public sealed class DeployedApiTests : IAsyncLifetime
{
    private const string Admin = "ADMIN", Editor = "EDITOR", Translator = "TRANSLATOR", Screen = "SCREEN", NoRole = "NOROLE";
    private const string CreateAmendment = "mutation($input: AmendmentInput!) { createAmendment(input: $input) { id version } }";
    private const string CreateBody = "mutation($id: ID!, $lang: String!, $md: String!) { createAmendmentBody(amendmentId: $id, language: $lang, markdown: $md) { version } }";
    private const string GetAmendment = "query($id: ID!) { amendment(id: $id) { id version isApproved bodies { language markdown version } } }";
    private readonly List<string> _created = [];

    public Task InitializeAsync() => Task.CompletedTask;

    // Children are created after their parents, so delete newest first.
    public async Task DisposeAsync()
    {
        foreach (var id in Enumerable.Reverse(_created))
        {
            var version = (string?)(await DeployedApi.Ok(Admin, GetAmendment, new { id }))["amendment"]?["version"];
            if (version is not null)
                await DeployedApi.Ok(Admin, "mutation($id: ID!, $v: String!) { deleteAmendment(id: $id, expectedVersion: $v) }", new { id, v = version });
        }
    }

    private async Task<string> Create(string title, string? parentId = null)
    {
        var data = await DeployedApi.Ok(Editor, CreateAmendment, new { input = new { title = $"[api-test] {title}", author = "API test", primaryLanguage = "en", parentAmendmentId = parentId } });
        var id = (string)data["createAmendment"]!["id"]!;
        _created.Add(id);
        return id;
    }

    private static Task AddBody(string id, string lang, string md) => DeployedApi.Ok(Translator, CreateBody, new { id, lang, md });

    private static async Task<Dictionary<string, string>> Bodies(string id) =>
        (await DeployedApi.Ok(NoRole, GetAmendment, new { id }))["amendment"]!["bodies"]!.AsArray()
            .ToDictionary(b => (string)b!["language"]!, b => (string)b!["markdown"]!);

    [DeployedApiFact]
    public async Task Requests_without_a_token_are_rejected()
    {
        var (status, _) = await DeployedApi.Send(null, "{ languages { code } }");
        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [DeployedApiFact]
    public async Task Any_signed_in_user_can_read_and_the_role_matrix_guards_writes()
    {
        var languages = (await DeployedApi.Ok(NoRole, "{ languages { code } }"))["languages"]!.AsArray();
        Assert.Contains(languages, l => (string)l!["code"]! == "en");
        Assert.NotNull((await DeployedApi.Ok(NoRole, "{ amendments { items { id } } }"))["amendments"]);

        var input = new { input = new { title = "[api-test] denied", author = "A", primaryLanguage = "en" } };
        foreach (var role in new[] { NoRole, Translator, Screen })
            Assert.Equal("FORBIDDEN", await DeployedApi.ErrorType(role, CreateAmendment, input));

        var id = await Create("roles");
        Assert.Equal("FORBIDDEN", await DeployedApi.ErrorType(Screen, CreateBody, new { id, lang = "en", md = "x" }));
        Assert.Equal("FORBIDDEN", await DeployedApi.ErrorType(Editor, "mutation($id: ID!) { deleteAmendment(id: $id, expectedVersion: \"1\") }", new { id }));
        Assert.Equal("FORBIDDEN", await DeployedApi.ErrorType(Editor,
            "mutation($id: ID!) { approveAmendment(id: $id, expectedVersion: \"1\", failureMode: BEST_EFFORT) { approvalApplied } }", new { id }));
    }

    [DeployedApiFact]
    public async Task Body_writes_are_validated_and_version_checked()
    {
        var id = await Create("bodies");
        await AddBody(id, "en", "One");

        Assert.Equal("CONFLICT", await DeployedApi.ErrorType(Translator, CreateBody, new { id, lang = "en", md = "Two" }));
        Assert.Equal("VALIDATION_ERROR", await DeployedApi.ErrorType(Translator, CreateBody, new { id, lang = "es", md = "   " }));

        const string update = "mutation($id: ID!, $v: String!) { updateAmendmentBody(amendmentId: $id, language: \"en\", expectedVersion: $v, markdown: \"Edited\", status: READY) { version } }";
        Assert.Equal("2", (string)(await DeployedApi.Ok(Translator, update, new { id, v = "1" }))["updateAmendmentBody"]!["version"]!);
        Assert.Equal("CONFLICT", await DeployedApi.ErrorType(Translator, update, new { id, v = "1" }));
        Assert.Equal("Edited", (await Bodies(id))["en"]);
    }

    [DeployedApiFact]
    public async Task Typed_client_edit_workflow_round_trips()
    {
        var editor = DeployedApi.Client(Editor);
        var translator = DeployedApi.Client(Translator);

        var created = await editor.CreateAmendmentAsync(new ApiAmendmentInput("[api-test] typed client", "API test", "en"));
        _created.Add(created.Id);
        var updated = await editor.UpdateAmendmentAsync(created.Id, created.Version, new ApiAmendmentInput("[api-test] typed client 2", "API test", "en", Motion: "M1"), isApproved: false);
        Assert.Equal(("[api-test] typed client 2", "M1"), (updated.Title, updated.Motion));

        var body = await translator.CreateBodyAsync(created.Id, "es", "Uno**NEWSLIDE**Dos");
        Assert.Equal((ApiBodyStatus.New, 2), (body.Status, body.PageCount));
        var edited = await translator.UpdateBodyAsync(created.Id, "es", body.Version, "Uno", ApiBodyStatus.Ready);
        var conflict = await Assert.ThrowsAsync<AmendmentApiException>(() => translator.UpdateBodyAsync(created.Id, "es", body.Version, "Tres", ApiBodyStatus.Ready));
        Assert.Equal(AmendmentApiException.Conflict, conflict.ErrorType);

        var read = await DeployedApi.Client(NoRole).GetAmendmentAsync(created.Id);
        Assert.Equal(edited.Version, Assert.Single(read!.Bodies).Version);
        var denied = await Assert.ThrowsAsync<AmendmentApiException>(() => translator.ArchiveAmendmentAsync(created.Id, read.Version, true));
        Assert.Equal("FORBIDDEN", denied.ErrorType);

        await translator.DeleteBodyAsync(created.Id, "es", edited.Version);
        Assert.Empty((await editor.GetAmendmentAsync(created.Id))!.Bodies);
    }

    [DeployedApiFact]
    public async Task Conference_approval_updates_the_parent_with_cleaned_child_text()
    {
        var parent = await Create("parent");
        await AddBody(parent, "en", "We shall proceed.");
        await AddBody(parent, "fr", "Nous procéderons.");
        var child = await Create("child", parent);
        await AddBody(child, "en", "We ~~shall~~ <u>will</u> proceed.");

        var result = (await DeployedApi.Ok(Editor,
            "mutation($id: ID!) { approveAmendment(id: $id, expectedVersion: \"1\") { approvalApplied updatedParentLanguages amendment { isApproved } } }",
            new { id = child }))["approveAmendment"]!;

        Assert.True((bool)result["approvalApplied"]!);
        Assert.True((bool)result["amendment"]!["isApproved"]!);
        Assert.Equal("""["en"]""", result["updatedParentLanguages"]!.ToJsonString());
        var parentBodies = await Bodies(parent);
        Assert.Equal("We  will proceed.", parentBodies["en"]);
        Assert.Equal("Nous procéderons.", parentBodies["fr"]);
        Assert.Equal("We  will proceed.", (await Bodies(child))["en"]);
    }
}
