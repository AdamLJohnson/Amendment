# Modernization status — notes for future AI sessions

Read first: `AMENDMENT_MODERNIZATION_PLAN.md` (checkboxes track progress), then `docs/modernization/decisions.md`.

## Where we are

| Phase | Status | Branch / commit |
|---|---|---|
| 0 Baseline | Approved (Checkpoint 0) | committed on `main` as "Phase 0" |
| 1 Domain contracts | Approved (Checkpoint 1) | committed on `modernization/phase-1-domain` as "Phase 1" |
| 1A .NET 10 retarget | Done, **awaiting Checkpoint 1A approval** | `modernization/phase-1a-net10` |
| 1A follow-up: packages to latest, R14/R17 fixed | Done (decisions.md §1B) | same branch, uncommitted |

## Rules the user set

- One phase at a time. Stop at each checkpoint for approval. Tick plan checkboxes as items complete.
- Target is `net10.0` everywhere. Every project is now `net10.0`; SDK pinned by `global.json` (D9).
- Ask before changing third-party package versions. MediatR stays at `12.5.0`.
- No AWS resources before Phase 2. Do not delete legacy code before cutover.
- Decisions D3–D6 (single presentation, role matrix, all-or-nothing approval with an Admin best-effort override, public shows only visible bodies) are fixed. Do not change them silently.

## Code map (Phase 1)

- `src/Amendment.Domain`: pure domain with no I/O and no packages.
  - Approval: `Approval/ConferenceApproval.cs` (planner), `Approval/AmendmentTextCleanup.cs` (legacy regexes).
  - Presentation: `Presentation/PresentationState.cs`, `PublicPresentation.cs`.
  - Timer: `Timer/TimerState.cs`.
  - Authorization: `Authorization/RoleOperationMatrix.cs`.
  - Rules: `Model/AmendmentRules.cs`, `BodyRules.cs`, `SlideDeck.cs`, `Versioning.cs`.
  - Events: `Events/EventContracts.cs`.
- `test/Amendment.Domain.Tests`: shared data in `Fixtures.cs`. Cleanup goldens are identical to the legacy `Characterization_CleanupAmendmentText_Golden`.
- Contracts: `docs/modernization/contracts.md` ([L] = legacy behavior, [Δ] = deliberate change) and `schema.draft.graphql`.

## Commands

- `dotnet build Amendment.sln -c Release`
- `dotnet test Amendment.sln -c Release --no-build` (includes `src/Amendment.Tests`)
- `dotnet publish src/Amendment.Server/Amendment.Server.csproj -c Release -o artifacts/publish`
- CI: `.github/workflows/ci.yml`. Docker: `docker build -f src/Amendment.Server/Dockerfile .`

## Known baseline issues

- 60 legacy build warnings (nullable only). R14 and R17 are closed.
- Windows full rebuilds hit the WebCompiler LESS error R18. Use an incremental build.

## Gotchas

- In test code, a static field named `Single` collides with `System.Single`. That is why the fixture is called `OneSlide`.
- Legacy approval cleans the child first, then cleans that result again for the parent. The domain keeps this behavior; see the `LegacyParity_*` test.
- Object mapping uses Mapster (`.Adapt<T>()`); AutoMapper was removed. Keep the Mapster version identical across projects.
- Blazorise is 2.x; 1.x component names/parameters no longer compile. Blazorise is now commercially licensed; replacement is deferred to a later phase (risk R19).
- The Docker container defaults to `Production` (`DbType=DB`) and needs `ConnectionStrings__DefaultConnection`.
