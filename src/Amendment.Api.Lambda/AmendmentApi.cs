using Amendment.Domain;
using Amendment.Domain.Authorization;
using Amendment.Domain.Model;
using Amendment.Infrastructure.DynamoDb;

namespace Amendment.Api.Lambda;

/// <summary>
/// GraphQL mutations (decision D15). Every operation authorizes with <see cref="RoleOperationMatrix"/> before reading
/// data, validates with the domain rules, and commits with one conditional DynamoDB transaction.
/// </summary>
public sealed partial class AmendmentApi(AmendmentStore store, LanguageProvider languages, TimeProvider clock)
{
    private const int MaxAttempts = 3;

    public async Task<ApiResponse> HandleAsync(ApiRequest request, CancellationToken ct = default)
    {
        var principal = CallerPrincipal.From(request.Identity);
        var args = new Args(request.Arguments);
        return request.Field switch
        {
            "createAmendment" => await CreateAmendmentAsync(principal, args, ct),
            "updateAmendment" => await UpdateAmendmentAsync(principal, args, ct),
            "archiveAmendment" => await ArchiveAmendmentAsync(principal, args, ct),
            "cloneAmendment" => await CloneAmendmentAsync(principal, args, ct),
            "deleteAmendment" => await DeleteAmendmentAsync(principal, args, ct),
            "approveAmendment" => await ApproveAmendmentAsync(principal, args, ct),
            "createAmendmentBody" => await CreateBodyAsync(principal, args, ct),
            "updateAmendmentBody" => await UpdateBodyAsync(principal, args, ct),
            "deleteAmendmentBody" => await DeleteBodyAsync(principal, args, ct),
            _ => ApiResponse.Fail(DomainError.Validation("field", $"Unknown operation '{request.Field}'."))
        };
    }

    private async Task<ApiResponse> CreateAmendmentAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.CreateAmendment) is { } forbidden)
            return ApiResponse.Fail(forbidden);

        var rules = new AmendmentRules(await languages.GetAsync(ct));
        var created = rules.Create(AmendmentIds.New(), InputOf(args, "input"), Stamp(principal));
        if (!created.IsSuccess)
            return ApiResponse.Fail(created.Errors);

        List<StoreWrite> writes = [StoreWrite.NewAmendment(created.Value)];
        if (created.Value.ParentAmendmentId is { } parentId)
            writes.Add(StoreWrite.ParentExists(parentId));
        var tx = await store.TransactAsync(writes, ct);
        return tx.IsCommitted ? ApiResponse.Ok(GraphQlShapes.ToDto(created.Value, [])) : ApiResponse.Fail(tx.Error!);
    }

    private async Task<ApiResponse> UpdateAmendmentAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.UpdateAmendment) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        if (FirstError(Required(args, "id", out var id), RequiredVersion(args, out var expected)) is { } invalid)
            return ApiResponse.Fail(invalid);

        var existing = await store.GetAmendmentWithBodiesAsync(id, ct);
        if (existing is null)
            return ApiResponse.Fail(NotFound(id));

        var rules = new AmendmentRules(await languages.GetAsync(ct));
        var next = rules.Update(existing.Amendment, InputOf(args, "input"), args.Bool("isApproved") ?? false, expected, Stamp(principal));
        if (!next.IsSuccess)
            return ApiResponse.Fail(next.Errors);

        List<StoreWrite> writes = [StoreWrite.UpdateAmendment(next.Value)];
        if (next.Value.ParentAmendmentId is { } parentId && parentId != existing.Amendment.ParentAmendmentId)
            writes.Add(StoreWrite.ParentExists(parentId));
        var tx = await store.TransactAsync(writes, ct);
        return tx.IsCommitted ? ApiResponse.Ok(GraphQlShapes.ToDto(next.Value, existing.Bodies)) : ApiResponse.Fail(tx.Error!);
    }

    private async Task<ApiResponse> ArchiveAmendmentAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.ArchiveAmendment) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        if (FirstError(Required(args, "id", out var id), RequiredVersion(args, out var expected)) is { } invalid)
            return ApiResponse.Fail(invalid);
        var archived = args.Bool("archived") ?? true;

        return await WithRetryAsync(async isLastAttempt =>
        {
            var existing = await store.GetAmendmentWithBodiesAsync(id, ct);
            if (existing is null)
                return ApiResponse.Fail(NotFound(id));
            var presentation = await store.GetPresentationAsync(ct);

            var rules = new AmendmentRules(await languages.GetAsync(ct));
            var next = rules.SetArchived(existing.Amendment, archived, expected, presentation.ActiveAmendmentId, Stamp(principal));
            if (!next.IsSuccess)
                return ApiResponse.Fail(next.Errors);
            if (ReferenceEquals(next.Value, existing.Amendment))
                return ApiResponse.Ok(GraphQlShapes.ToDto(existing.Amendment, existing.Bodies));

            List<StoreWrite> writes = [StoreWrite.UpdateAmendment(next.Value)];
            if (archived)
                writes.Add(StoreWrite.AmendmentNotPresented(id));
            var tx = await store.TransactAsync(writes, ct);
            return Committed(tx, isLastAttempt) ?? ApiResponse.Ok(GraphQlShapes.ToDto(next.Value, existing.Bodies));
        });
    }

    private async Task<ApiResponse> CloneAmendmentAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.CloneAmendment) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        if (Required(args, "sourceId", out var sourceId) is { } missing)
            return ApiResponse.Fail(missing);

        var source = await store.GetAmendmentWithBodiesAsync(sourceId, ct);
        if (source is null)
            return ApiResponse.Fail(NotFound(sourceId));

        var rules = new AmendmentRules(await languages.GetAsync(ct));
        var clone = rules.Clone(source.Amendment, source.Bodies, AmendmentIds.New(), args.Object("overrides")?.ToAmendmentInput(), Stamp(principal));
        if (!clone.IsSuccess)
            return ApiResponse.Fail(clone.Errors);

        var (amendment, bodies) = clone.Value;
        List<StoreWrite> writes = [StoreWrite.NewAmendment(amendment), StoreWrite.ParentExists(source.Amendment.Id), .. bodies.Select(StoreWrite.NewBody)];
        var tx = await store.TransactAsync(writes, ct);
        return tx.IsCommitted ? ApiResponse.Ok(GraphQlShapes.ToDto(amendment, bodies)) : ApiResponse.Fail(tx.Error!);
    }

    private async Task<ApiResponse> DeleteAmendmentAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.DeleteAmendment) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        if (FirstError(Required(args, "id", out var id), RequiredVersion(args, out var expected)) is { } invalid)
            return ApiResponse.Fail(invalid);

        return await WithRetryAsync(async isLastAttempt =>
        {
            var existing = await store.GetAmendmentWithBodiesAsync(id, ct);
            if (existing is null)
                return ApiResponse.Fail(NotFound(id));
            var presentation = await store.GetPresentationAsync(ct);
            if (AmendmentRules.CanDelete(existing.Amendment, expected, presentation.ActiveAmendmentId) is { } blocked)
                return ApiResponse.Fail(blocked);

            List<StoreWrite> writes = [StoreWrite.DeleteAmendment(existing.Amendment), StoreWrite.AmendmentNotPresented(id), .. existing.Bodies.Select(StoreWrite.DeleteBody)];
            var tx = await store.TransactAsync(writes, ct);
            if (Committed(tx, isLastAttempt) is { } failed)
                return failed;
            await store.DeleteAmendmentPartitionAsync(id, ct);
            return ApiResponse.Ok(id);
        });
    }
}
