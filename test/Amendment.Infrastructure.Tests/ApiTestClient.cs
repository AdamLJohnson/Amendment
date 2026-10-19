using System.Text.Json;
using Amendment.Api.Lambda;
using Amendment.Infrastructure.DynamoDb;

namespace Amendment.Infrastructure.Tests;

/// <summary>Calls <see cref="AmendmentApi"/> the way the AppSync resolver does, as a given Cognito user.</summary>
public sealed class ApiTestClient(AmendmentStore store)
{
    private readonly AmendmentApi _api = new(store, new LanguageProvider(store, TimeProvider.System), TimeProvider.System);

    public static readonly ApiIdentity Admin = new("sub-admin", ["SystemAdministrator"]);
    public static readonly ApiIdentity Editor = new("sub-editor", ["AmendmentEditor"]);
    public static readonly ApiIdentity Translator = new("sub-translator", ["Translator"]);
    public static readonly ApiIdentity Screen = new("sub-screen", ["ScreenController"]);
    public static readonly ApiIdentity NoRole = new("sub-norole", []);

    public Task<ApiResponse> Call(ApiIdentity? identity, string field, object args) =>
        _api.HandleAsync(new ApiRequest(field, JsonSerializer.SerializeToElement(args), identity));

    public async Task<T> Ok<T>(ApiIdentity identity, string field, object args)
    {
        var response = await Call(identity, field, args);
        Assert.True(response.Error is null, $"{field} failed: {response.Error?.Type} {response.Error?.Message}");
        return Assert.IsType<T>(response.Data);
    }

    public async Task<ApiError> Fails(ApiIdentity? identity, string field, object args)
    {
        var response = await Call(identity, field, args);
        Assert.Null(response.Data);
        return Assert.IsType<ApiError>(response.Error);
    }

    public Task<AmendmentDto> CreateAmendment(string title = "Amendment", string? parentId = null) =>
        Ok<AmendmentDto>(Editor, "createAmendment", new { input = new { title, author = "Author", primaryLanguage = "en", parentAmendmentId = parentId } });

    public Task<AmendmentBodyDto> CreateBody(string amendmentId, string language, string markdown) =>
        Ok<AmendmentBodyDto>(Translator, "createAmendmentBody", new { amendmentId, language, markdown });
}
