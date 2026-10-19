using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amendment.Domain;
using Amendment.Domain.Languages;
using Amendment.Infrastructure.DynamoDb;

[assembly: LambdaSerializer(typeof(CamelCaseLambdaJsonSerializer))]

namespace Amendment.Api.Lambda;

/// <summary>Lambda entry point (handler <c>Amendment.Api.Lambda::Amendment.Api.Lambda.Function::HandleAsync</c>).</summary>
public sealed class Function
{
    public const string TableNameVariable = "AMENDMENT_TABLE_NAME";

    private readonly AmendmentApi _api;

    public Function()
    {
        var tableName = Environment.GetEnvironmentVariable(TableNameVariable)
            ?? throw new InvalidOperationException($"{TableNameVariable} is not set.");
        var store = new AmendmentStore(new AmazonDynamoDBClient(), tableName);
        _api = new AmendmentApi(store, new LanguageProvider(store, TimeProvider.System), TimeProvider.System);
    }

    public Function(AmendmentApi api) => _api = api;

    public async Task<ApiResponse> HandleAsync(ApiRequest request, ILambdaContext context)
    {
        try
        {
            return await _api.HandleAsync(request);
        }
        catch (Exception ex)
        {
            // Details stay in the logs; the caller gets a generic error (plan §5 error contract).
            context.Logger.LogError($"{request.Field} failed ({context.AwsRequestId}): {ex}");
            return ApiResponse.Fail(new DomainError(DomainErrorCode.InternalError, $"The operation failed. Reference: {context.AwsRequestId}"));
        }
    }
}

/// <summary>Configured languages from the table, cached for five minutes per Lambda instance.</summary>
public sealed class LanguageProvider(AmendmentStore store, TimeProvider clock)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private SupportedLanguages? _cached;
    private DateTimeOffset _expires;

    public async Task<SupportedLanguages> GetAsync(CancellationToken ct = default)
    {
        if (_cached is not null && clock.GetUtcNow() < _expires)
            return _cached;
        var records = await store.GetLanguagesAsync(ct);
        _cached = records.Count == 0 ? SupportedLanguages.Default : new SupportedLanguages(records.Select(r => r.Code));
        _expires = clock.GetUtcNow() + CacheDuration;
        return _cached;
    }
}
