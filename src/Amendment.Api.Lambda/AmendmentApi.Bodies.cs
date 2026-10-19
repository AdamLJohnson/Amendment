using Amendment.Domain;
using Amendment.Domain.Authorization;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using Amendment.Domain.Presentation;
using Amendment.Infrastructure.DynamoDb;

namespace Amendment.Api.Lambda;

/// <summary>Translation-body mutations and shared helpers.</summary>
public sealed partial class AmendmentApi
{
    /// <summary>Returned by an attempt to request another attempt (a concurrent presentation change).</summary>
    private static readonly ApiResponse Retry = new(null, null);

    private async Task<ApiResponse> CreateBodyAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.CreateBody) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        if (Required(args, "amendmentId", out var amendmentId) is { } missing)
            return ApiResponse.Fail(missing);
        if (GraphQlShapes.ParseStatus(args.String("status") ?? "NEW") is not { } status)
            return ApiResponse.Fail(DomainError.Validation("status", "Unknown body status."));
        var rules = new BodyRules(await languages.GetAsync(ct));
        var stamp = Stamp(principal);

        return await WithRetryAsync(async isLastAttempt =>
        {
            var loaded = await store.GetAmendmentWithBodiesAsync(amendmentId, ct);
            var body = rules.Create(loaded?.Amendment, loaded?.Bodies.Select(b => b.Language) ?? [], args.String("language"), args.String("markdown"), status, stamp);
            if (!body.IsSuccess)
                return ApiResponse.Fail(body.Errors);

            var presentation = await store.GetPresentationAsync(ct);
            List<StoreWrite> writes = [StoreWrite.AmendmentExists(amendmentId), StoreWrite.NewBody(body.Value),
                .. PresentationWrites(presentation, presentation.ReconcileBody(body.Value, stamp), amendmentId)];
            var tx = await store.TransactAsync(writes, ct);
            return Committed(tx, isLastAttempt) ?? ApiResponse.Ok(GraphQlShapes.ToDto(body.Value));
        });
    }

    private async Task<ApiResponse> UpdateBodyAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.UpdateBody) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        if (FirstError(Required(args, "amendmentId", out var amendmentId), RequiredVersion(args, out var expected), LanguageOf(args, out var language)) is { } invalid)
            return ApiResponse.Fail(invalid);
        if (GraphQlShapes.ParseStatus(args.String("status")) is not { } status)
            return ApiResponse.Fail(DomainError.Validation("status", "Unknown body status."));
        var stamp = Stamp(principal);

        return await WithRetryAsync(async isLastAttempt =>
        {
            var existing = await store.GetBodyAsync(amendmentId, language, ct);
            if (existing is null)
                return ApiResponse.Fail(BodyNotFound(amendmentId, language));
            var next = BodyRules.Update(existing, args.String("markdown"), status, expected, stamp);
            if (!next.IsSuccess)
                return ApiResponse.Fail(next.Errors);

            var presentation = await store.GetPresentationAsync(ct);
            List<StoreWrite> writes = [StoreWrite.UpdateBody(next.Value),
                .. PresentationWrites(presentation, presentation.ReconcileBody(next.Value, stamp), amendmentId)];
            var tx = await store.TransactAsync(writes, ct);
            return Committed(tx, isLastAttempt) ?? ApiResponse.Ok(GraphQlShapes.ToDto(next.Value));
        });
    }

    private async Task<ApiResponse> DeleteBodyAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.DeleteBody) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        if (FirstError(Required(args, "amendmentId", out var amendmentId), RequiredVersion(args, out var expected), LanguageOf(args, out var language)) is { } invalid)
            return ApiResponse.Fail(invalid);

        return await WithRetryAsync(async isLastAttempt =>
        {
            var existing = await store.GetBodyAsync(amendmentId, language, ct);
            if (existing is null)
                return ApiResponse.Fail(BodyNotFound(amendmentId, language));
            if (Versioning.Check($"Body {amendmentId}/{language}", existing.Version, expected) is { } conflict)
                return ApiResponse.Fail(conflict);

            var presentation = await store.GetPresentationAsync(ct);
            List<StoreWrite> writes = [StoreWrite.DeleteBody(existing),
                .. PresentationWrites(presentation, presentation.RemoveBody(amendmentId, language, Stamp(principal)), amendmentId)];
            var tx = await store.TransactAsync(writes, ct);
            return Committed(tx, isLastAttempt) ?? ApiResponse.Ok(language.Value);
        });
    }

    /// <summary>
    /// Keeps the presentation consistent with a body write (contracts.md §6): save the reconciled state when it changed;
    /// otherwise, for an amendment that is not presented, check that it still is not (else the attempt is retried).
    /// </summary>
    private static IEnumerable<StoreWrite> PresentationWrites(PresentationState before, PresentationState after, string amendmentId)
    {
        if (!ReferenceEquals(before, after))
            yield return StoreWrite.SavePresentation(after);
        else if (before.ActiveAmendmentId != amendmentId)
            yield return StoreWrite.AmendmentNotPresented(amendmentId);
    }

    /// <summary>Null when committed; <see cref="Retry"/> after a presentation race (except on the last attempt); otherwise the error.</summary>
    private static ApiResponse? Committed(TransactionResult tx, bool isLastAttempt)
    {
        if (tx.IsCommitted)
            return null;
        return !isLastAttempt && tx.FailedWrite?.TargetsPresentation == true ? Retry : ApiResponse.Fail(tx.Error!);
    }

    private static async Task<ApiResponse> WithRetryAsync(Func<bool, Task<ApiResponse>> attempt)
    {
        for (var i = 1; ; i++)
        {
            var response = await attempt(i == MaxAttempts);
            if (!ReferenceEquals(response, Retry))
                return response;
        }
    }

    private AuditStamp Stamp(Principal principal) => new(principal.UserId!, clock.GetUtcNow());

    private static AmendmentInput InputOf(Args args, string name) =>
        args.Object(name)?.ToAmendmentInput() ?? new AmendmentInput(null, null, null);

    private static DomainError? FirstError(params DomainError?[] errors) => errors.FirstOrDefault(e => e is not null);

    private static DomainError? Required(Args args, string name, out string value)
    {
        value = args.String(name) ?? "";
        return string.IsNullOrWhiteSpace(value) ? DomainError.Validation(name, $"{name} is required.") : null;
    }

    private static DomainError? RequiredVersion(Args args, out long value)
    {
        value = args.Long("expectedVersion") ?? 0;
        return value < Versioning.InitialVersion ? DomainError.Validation("expectedVersion", "expectedVersion is required.") : null;
    }

    private static DomainError? LanguageOf(Args args, out LanguageCode language) =>
        LanguageCode.TryNormalize(args.String("language"), out language)
            ? null
            : DomainError.Validation("language", $"'{args.String("language")}' is not a valid language code.");

    private static DomainError NotFound(string id) => DomainError.NotFound($"Amendment {id} was not found.");

    private static DomainError BodyNotFound(string amendmentId, LanguageCode language) =>
        DomainError.NotFound($"Amendment {amendmentId} has no '{language}' body.");
}
