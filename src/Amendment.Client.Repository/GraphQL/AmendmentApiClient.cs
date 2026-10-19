using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Amendment.Client.Repository.GraphQl;

/// <summary>
/// Typed client for the GraphQL amendment API. <see cref="GetLanguagesAsync"/> and <see cref="GetPublicPresentationAsync"/>
/// also work anonymously (public token, decision D21).
/// </summary>
public interface IAmendmentApi
{
    Task<ApiAmendmentPage> GetAmendmentsAsync(bool archived = false, int limit = 50, string? nextToken = null, CancellationToken ct = default);
    Task<ApiAmendment?> GetAmendmentAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<ApiLanguage>> GetLanguagesAsync(CancellationToken ct = default);
    Task<ApiAmendment> CreateAmendmentAsync(ApiAmendmentInput input, CancellationToken ct = default);
    Task<ApiAmendment> UpdateAmendmentAsync(string id, string expectedVersion, ApiAmendmentInput input, bool isApproved, CancellationToken ct = default);
    Task<ApiAmendment> ArchiveAmendmentAsync(string id, string expectedVersion, bool archived, CancellationToken ct = default);
    Task<ApiAmendment> CloneAmendmentAsync(string sourceId, ApiAmendmentInput? overrides = null, CancellationToken ct = default);
    Task<ApiApprovalResult> ApproveAmendmentAsync(string id, string expectedVersion, bool cleanChildBodies = true, bool bestEffort = false, CancellationToken ct = default);
    Task DeleteAmendmentAsync(string id, string expectedVersion, CancellationToken ct = default);
    Task<ApiAmendmentBody> CreateBodyAsync(string amendmentId, string language, string markdown, string status = ApiBodyStatus.New, CancellationToken ct = default);
    Task<ApiAmendmentBody> UpdateBodyAsync(string amendmentId, string language, string expectedVersion, string markdown, string status, CancellationToken ct = default);
    Task DeleteBodyAsync(string amendmentId, string language, string expectedVersion, CancellationToken ct = default);
    Task<ApiPresentation> GetPresentationAsync(CancellationToken ct = default);
    Task<ApiPublicPresentation> GetPublicPresentationAsync(CancellationToken ct = default);
    Task<ApiPresentation> PresentAmendmentAsync(string id, string expectedVersion, CancellationToken ct = default);
    Task<ApiPresentation> ClearPresentationAsync(string expectedVersion, CancellationToken ct = default);
    Task<ApiPresentation> SetLanguagePresentationAsync(string expectedVersion, IReadOnlyList<ApiLanguageChange> changes, CancellationToken ct = default);
}

/// <summary>
/// Sends GraphQL requests with the supplied <see cref="HttpClient"/>, whose base address is the GraphQL endpoint and
/// whose handler adds the Cognito token. GraphQL errors are thrown as <see cref="AmendmentApiException"/>.
/// </summary>
public sealed class AmendmentApiClient(HttpClient http) : IAmendmentApi
{
    private const string AuditFields = "by at";
    private const string BodyFields = $"amendmentId language markdown status pageCount version created {{ {AuditFields} }} updated {{ {AuditFields} }}";
    private const string AmendmentFields = $"id title author motion source legisId primaryLanguage isArchived isApproved parentAmendmentId version created {{ {AuditFields} }} updated {{ {AuditFields} }} bodies {{ {BodyFields} }}";
    private const string PresentationFields = $"activeAmendmentId languages {{ language visible page }} version updated {{ {AuditFields} }}";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ApiAmendmentPage> GetAmendmentsAsync(bool archived = false, int limit = 50, string? nextToken = null, CancellationToken ct = default) =>
        (await SendAsync<ApiAmendmentPage>("amendments",
            $"query($archived: Boolean, $limit: Int, $nextToken: String) {{ amendments(archived: $archived, limit: $limit, nextToken: $nextToken) {{ items {{ {AmendmentFields} }} nextToken }} }}",
            new { archived, limit, nextToken }, ct))!;

    public Task<ApiAmendment?> GetAmendmentAsync(string id, CancellationToken ct = default) =>
        SendAsync<ApiAmendment>("amendment", $"query($id: ID!) {{ amendment(id: $id) {{ {AmendmentFields} }} }}", new { id }, ct);

    public async Task<IReadOnlyList<ApiLanguage>> GetLanguagesAsync(CancellationToken ct = default) =>
        (await SendAsync<List<ApiLanguage>>("languages", "{ languages { code name sortOrder } }", null, ct))!;

    public async Task<ApiAmendment> CreateAmendmentAsync(ApiAmendmentInput input, CancellationToken ct = default) =>
        (await SendAsync<ApiAmendment>("createAmendment",
            $"mutation($input: AmendmentInput!) {{ createAmendment(input: $input) {{ {AmendmentFields} }} }}", new { input }, ct))!;

    public async Task<ApiAmendment> UpdateAmendmentAsync(string id, string expectedVersion, ApiAmendmentInput input, bool isApproved, CancellationToken ct = default) =>
        (await SendAsync<ApiAmendment>("updateAmendment",
            $"mutation($id: ID!, $expectedVersion: String!, $input: AmendmentInput!, $isApproved: Boolean!) {{ updateAmendment(id: $id, expectedVersion: $expectedVersion, input: $input, isApproved: $isApproved) {{ {AmendmentFields} }} }}",
            new { id, expectedVersion, input, isApproved }, ct))!;

    public async Task<ApiAmendment> ArchiveAmendmentAsync(string id, string expectedVersion, bool archived, CancellationToken ct = default) =>
        (await SendAsync<ApiAmendment>("archiveAmendment",
            $"mutation($id: ID!, $expectedVersion: String!, $archived: Boolean!) {{ archiveAmendment(id: $id, expectedVersion: $expectedVersion, archived: $archived) {{ {AmendmentFields} }} }}",
            new { id, expectedVersion, archived }, ct))!;

    public async Task<ApiAmendment> CloneAmendmentAsync(string sourceId, ApiAmendmentInput? overrides = null, CancellationToken ct = default) =>
        (await SendAsync<ApiAmendment>("cloneAmendment",
            $"mutation($sourceId: ID!, $overrides: AmendmentInput) {{ cloneAmendment(sourceId: $sourceId, overrides: $overrides) {{ {AmendmentFields} }} }}",
            new { sourceId, overrides }, ct))!;

    public async Task<ApiApprovalResult> ApproveAmendmentAsync(string id, string expectedVersion, bool cleanChildBodies = true, bool bestEffort = false, CancellationToken ct = default) =>
        (await SendAsync<ApiApprovalResult>("approveAmendment",
            $"mutation($id: ID!, $expectedVersion: String!, $cleanChildBodies: Boolean, $failureMode: ApprovalFailureMode) {{ approveAmendment(id: $id, expectedVersion: $expectedVersion, cleanChildBodies: $cleanChildBodies, failureMode: $failureMode) {{ amendment {{ {AmendmentFields} }} approvalApplied updatedChildLanguages updatedParentLanguages warnings }} }}",
            new { id, expectedVersion, cleanChildBodies, failureMode = bestEffort ? "BEST_EFFORT" : "ALL_OR_NOTHING" }, ct))!;

    public Task DeleteAmendmentAsync(string id, string expectedVersion, CancellationToken ct = default) =>
        SendAsync<string>("deleteAmendment",
            "mutation($id: ID!, $expectedVersion: String!) { deleteAmendment(id: $id, expectedVersion: $expectedVersion) }",
            new { id, expectedVersion }, ct);

    public async Task<ApiAmendmentBody> CreateBodyAsync(string amendmentId, string language, string markdown, string status = ApiBodyStatus.New, CancellationToken ct = default) =>
        (await SendAsync<ApiAmendmentBody>("createAmendmentBody",
            $"mutation($amendmentId: ID!, $language: String!, $markdown: String!, $status: BodyStatus) {{ createAmendmentBody(amendmentId: $amendmentId, language: $language, markdown: $markdown, status: $status) {{ {BodyFields} }} }}",
            new { amendmentId, language, markdown, status }, ct))!;

    public async Task<ApiAmendmentBody> UpdateBodyAsync(string amendmentId, string language, string expectedVersion, string markdown, string status, CancellationToken ct = default) =>
        (await SendAsync<ApiAmendmentBody>("updateAmendmentBody",
            $"mutation($amendmentId: ID!, $language: String!, $expectedVersion: String!, $markdown: String!, $status: BodyStatus!) {{ updateAmendmentBody(amendmentId: $amendmentId, language: $language, expectedVersion: $expectedVersion, markdown: $markdown, status: $status) {{ {BodyFields} }} }}",
            new { amendmentId, language, expectedVersion, markdown, status }, ct))!;

    public Task DeleteBodyAsync(string amendmentId, string language, string expectedVersion, CancellationToken ct = default) =>
        SendAsync<string>("deleteAmendmentBody",
            "mutation($amendmentId: ID!, $language: String!, $expectedVersion: String!) { deleteAmendmentBody(amendmentId: $amendmentId, language: $language, expectedVersion: $expectedVersion) }",
            new { amendmentId, language, expectedVersion }, ct);

    public async Task<ApiPresentation> GetPresentationAsync(CancellationToken ct = default) =>
        (await SendAsync<ApiPresentation>("presentation", $"{{ presentation {{ {PresentationFields} }} }}", null, ct))!;

    public async Task<ApiPublicPresentation> GetPublicPresentationAsync(CancellationToken ct = default) =>
        (await SendAsync<ApiPublicPresentation>("publicPresentation",
            "{ publicPresentation { version activeAmendmentId languages { language visible page pageCount markdown bodyVersion } } }", null, ct))!;

    public async Task<ApiPresentation> PresentAmendmentAsync(string id, string expectedVersion, CancellationToken ct = default) =>
        (await SendAsync<ApiPresentation>("presentAmendment",
            $"mutation($id: ID!, $expectedVersion: String!) {{ presentAmendment(id: $id, expectedVersion: $expectedVersion) {{ {PresentationFields} }} }}",
            new { id, expectedVersion }, ct))!;

    public async Task<ApiPresentation> ClearPresentationAsync(string expectedVersion, CancellationToken ct = default) =>
        (await SendAsync<ApiPresentation>("clearPresentation",
            $"mutation($expectedVersion: String!) {{ clearPresentation(expectedVersion: $expectedVersion) {{ {PresentationFields} }} }}",
            new { expectedVersion }, ct))!;

    public async Task<ApiPresentation> SetLanguagePresentationAsync(string expectedVersion, IReadOnlyList<ApiLanguageChange> changes, CancellationToken ct = default) =>
        (await SendAsync<ApiPresentation>("setLanguagePresentation",
            $"mutation($expectedVersion: String!, $changes: [LanguageChangeInput!]!) {{ setLanguagePresentation(expectedVersion: $expectedVersion, changes: $changes) {{ {PresentationFields} }} }}",
            new { expectedVersion, changes }, ct))!;

    private async Task<T?> SendAsync<T>(string field, string query, object? variables, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("", new { query, variables }, Json, ct);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new AmendmentApiException(AmendmentApiException.Unauthorized, "You are not signed in, or your session has expired.");

        var body = await response.Content.ReadFromJsonAsync<JsonObject>(Json, ct);
        if (body?["errors"] is JsonArray { Count: > 0 } errors)
        {
            var error = errors[0]!;
            throw new AmendmentApiException(
                (string?)error["errorType"] ?? "INTERNAL_ERROR",
                (string?)error["message"] ?? "The operation failed.",
                (string?)error["errorInfo"]?["field"]);
        }
        if (!response.IsSuccessStatusCode || body?["data"] is not JsonObject data)
            throw new AmendmentApiException("INTERNAL_ERROR", $"The API returned HTTP {(int)response.StatusCode}.");

        return data[field] is { } value ? value.Deserialize<T>(Json) : default;
    }
}
