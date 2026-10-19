using Amendment.Domain;
using Amendment.Domain.Authorization;
using Amendment.Domain.Languages;
using Amendment.Domain.Model;
using Amendment.Domain.Presentation;
using Amendment.Infrastructure.DynamoDb;

namespace Amendment.Api.Lambda;

/// <summary>Presentation reads and operator screen controls (contracts.md §6–7, decisions D3, D6, D22).</summary>
public sealed partial class AmendmentApi
{
    private async Task<ApiResponse> GetPresentationAsync(Principal principal, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.ReadPresentation) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        return ApiResponse.Ok(GraphQlShapes.ToDto(await store.GetPresentationAsync(ct)));
    }

    /// <summary>Anonymous projector read: only the currently visible bodies of the presented amendment (D6).</summary>
    private async Task<ApiResponse> GetPublicPresentationAsync(Principal principal, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.ReadPublicPresentation) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        var state = await store.GetPresentationAsync(ct);
        IReadOnlyList<AmendmentBodyRecord> bodies = state.ActiveAmendmentId is { } id
            ? (await store.GetAmendmentWithBodiesAsync(id, ct))?.Bodies ?? []
            : [];
        return ApiResponse.Ok(GraphQlShapes.ToDto(PublicProjection.Create(state, bodies)));
    }

    private async Task<ApiResponse> PresentAmendmentAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.PresentAmendment) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        if (FirstError(Required(args, "id", out var id), RequiredVersion(args, out var expected)) is { } invalid)
            return ApiResponse.Fail(invalid);

        var loaded = await store.GetAmendmentWithBodiesAsync(id, ct);
        if (loaded is null)
            return ApiResponse.Fail(NotFound(id));
        var state = await store.GetPresentationAsync(ct);
        var next = state.Present(loaded.Amendment, loaded.Bodies, expected, Stamp(principal));
        if (!next.IsSuccess)
            return ApiResponse.Fail(next.Errors);

        var tx = await store.TransactAsync([StoreWrite.SavePresentation(next.Value), StoreWrite.AmendmentUnchanged(loaded.Amendment)], ct);
        return tx.IsCommitted ? ApiResponse.Ok(GraphQlShapes.ToDto(next.Value)) : ApiResponse.Fail(tx.Error!);
    }

    private async Task<ApiResponse> ClearPresentationAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.ClearPresentation) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        if (RequiredVersion(args, out var expected) is { } invalid)
            return ApiResponse.Fail(invalid);

        var state = await store.GetPresentationAsync(ct);
        var next = state.Clear(expected, Stamp(principal));
        return await SaveAsync(state, next, ct);
    }

    private async Task<ApiResponse> SetLanguagePresentationAsync(Principal principal, Args args, CancellationToken ct)
    {
        if (RoleOperationMatrix.Authorize(principal, Operation.SetLanguagePresentation) is { } forbidden)
            return ApiResponse.Fail(forbidden);
        if (RequiredVersion(args, out var expected) is { } invalid)
            return ApiResponse.Fail(invalid);

        var changes = new List<LanguageChange>();
        foreach (var change in args.List("changes") ?? [])
        {
            if (LanguageOf(change, out var language) is { } badLanguage)
                return ApiResponse.Fail(badLanguage);
            changes.Add(new LanguageChange(language, change.Bool("visible"), change.Int("page")));
        }

        var state = await store.GetPresentationAsync(ct);
        IReadOnlyDictionary<LanguageCode, int> pageCounts = state.ActiveAmendmentId is { } id
            ? ((await store.GetAmendmentWithBodiesAsync(id, ct))?.Bodies ?? []).ToDictionary(b => b.Language, b => b.PageCount)
            : new Dictionary<LanguageCode, int>();
        var next = state.SetLanguages(changes, pageCounts, expected, Stamp(principal));
        return await SaveAsync(state, next, ct);
    }

    /// <summary>Saves a changed state with a version check; an unchanged state (idempotent request) is returned as is.</summary>
    private async Task<ApiResponse> SaveAsync(PresentationState before, DomainResult<PresentationState> next, CancellationToken ct)
    {
        if (!next.IsSuccess)
            return ApiResponse.Fail(next.Errors);
        if (ReferenceEquals(next.Value, before))
            return ApiResponse.Ok(GraphQlShapes.ToDto(before));
        var saved = await store.SavePresentationAsync(next.Value, ct);
        return saved.IsSuccess ? ApiResponse.Ok(GraphQlShapes.ToDto(saved.Value)) : ApiResponse.Fail(saved.Errors);
    }
}
