# Modernization status — notes for future AI sessions

Read first: `AMENDMENT_MODERNIZATION_PLAN.md` (checkboxes track progress), then `docs/modernization/decisions.md`.

## Where we are

| Phase | Status | Branch / commit |
|---|---|---|
| 0 Baseline | Approved (Checkpoint 0) | committed on `main` as "Phase 0" |
| 1 Domain contracts | Done, **awaiting Checkpoint 1 approval** | `modernization/phase-1-domain` |
| 1A .NET 10 retarget | Not started; next after approval | — |

## Rules the user set

- One phase at a time. Stop at each checkpoint for approval. Tick plan checkboxes as items complete.
- Target is `net10.0` everywhere. Legacy projects are still `net9.0` until Phase 1A.
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

- `dotnet build Amendment.sln -c Debug`
- `dotnet test Amendment.sln --no-build`
- `dotnet test src\Amendment.Tests\Amendment.Tests.csproj`. This project is **not** in the solution, so the solution test run skips it.

## Known baseline issues

- `AmendmentBodyControllerTests.PostAction_Returns_CreatedAmendmentBody` fails before any of our changes, most likely because of a duplicate-language seed (R14). It stays unfixed until approved.
- 61 legacy build warnings (mostly nullable).

## Gotchas

- In test code, a static field named `Single` collides with `System.Single`. That is why the fixture is called `OneSlide`.
- Legacy approval cleans the child first, then cleans that result again for the parent. The domain keeps this behavior; see the `LegacyParity_*` test.
