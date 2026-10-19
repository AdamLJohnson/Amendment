using System.Text.Json;
using Amendment.Domain;
using Amendment.Domain.Authorization;

namespace Amendment.Api.Lambda;

/// <summary>
/// Payload built by the AppSync JS resolver (<c>infra/Amendment.Cdk/Resolvers/invoke-api.js</c>):
/// the GraphQL field name, its arguments and the verified Cognito identity.
/// </summary>
public sealed record ApiRequest(string Field, JsonElement Arguments, ApiIdentity? Identity);

/// <summary>Cognito identity as verified by AppSync. <c>Groups</c> are Cognito group names (decision D13).</summary>
public sealed record ApiIdentity(string? Sub, IReadOnlyList<string>? Groups);

/// <summary>Either <c>Data</c> or <c>Error</c>. The JS resolver turns <c>Error</c> into a GraphQL error with <c>errorType</c> = <c>Type</c>.</summary>
public sealed record ApiResponse(object? Data, ApiError? Error)
{
    public static ApiResponse Ok(object? data) => new(data, null);

    public static ApiResponse Fail(IReadOnlyList<DomainError> errors)
    {
        var first = errors[0];
        return new(null, new ApiError(
            first.Code.ToWireName(),
            first.Message,
            first.Field,
            errors.Select(e => new ApiFieldError(e.Code.ToWireName(), e.Message, e.Field)).ToList()));
    }

    public static ApiResponse Fail(DomainError error) => Fail([error]);
}

public sealed record ApiError(string Type, string Message, string? Field, IReadOnlyList<ApiFieldError> Errors);

public sealed record ApiFieldError(string Type, string Message, string? Field);

/// <summary>Maps Cognito groups to the domain role names used by <see cref="RoleOperationMatrix"/> (decision D13).</summary>
public static class CallerPrincipal
{
    public static IReadOnlyDictionary<string, string> GroupToRole { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["SystemAdministrator"] = Roles.Admin,
        ["ScreenController"] = Roles.ScreenController,
        ["AmendmentEditor"] = Roles.AmendmentEditor,
        ["Translator"] = Roles.Translator
    };

    /// <summary>Unknown groups are ignored; a missing <c>sub</c> is anonymous (fails closed).</summary>
    public static Principal From(ApiIdentity? identity)
    {
        if (string.IsNullOrWhiteSpace(identity?.Sub))
            return Principal.Anonymous;
        var roles = (identity.Groups ?? []).Where(GroupToRole.ContainsKey).Select(g => GroupToRole[g]).ToArray();
        return Principal.User(identity.Sub, roles);
    }
}
