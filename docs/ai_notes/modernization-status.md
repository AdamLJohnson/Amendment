# Modernization status — notes for future AI sessions

Read first: `AMENDMENT_MODERNIZATION_PLAN.md` (checkboxes track progress), then `docs/modernization/decisions.md`.

## Where we are

| Phase | Status | Branch / commit |
|---|---|---|
| 0 Baseline | Approved (Checkpoint 0) | committed on `main` as "Phase 0" |
| 1 Domain contracts | Approved (Checkpoint 1) | committed on `modernization/phase-1-domain` as "Phase 1" |
| 1A .NET 10 retarget | Done, **awaiting Checkpoint 1A approval** | `modernization/phase-1a-net10` |

## Rules the user set

- One phase at a time. Stop at each checkpoint for approval. Tick plan checkboxes as items complete.
- Target is `net10.0` everywhere. Every project is now `net10.0`; SDK pinned by `global.json` (D9).
- Ask before changing third-party package versions (e.g. R17).
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

- `AmendmentBodyControllerTests.PostAction_Returns_CreatedAmendmentBody` fails before any of our changes, most likely because of a duplicate-language seed (R14). It stays unfixed until approved.
- 61 legacy build warnings (nullable plus NU1903, see R17).
- Windows full rebuilds hit the WebCompiler LESS error R18. Use an incremental build.

## Gotchas

- In test code, a static field named `Single` collides with `System.Single`. That is why the fixture is called `OneSlide`.
- Legacy approval cleans the child first, then cleans that result again for the parent. The domain keeps this behavior; see the `LegacyParity_*` test.
- Do not re-add `Microsoft.AspNetCore.OpenApi` while Swashbuckle is 8.x. It breaks server startup.
- The Docker container defaults to `Production` (`DbType=DB`) and needs `ConnectionStrings__DefaultConnection`.
