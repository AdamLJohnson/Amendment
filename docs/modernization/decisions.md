# Modernization Decisions, Baseline, Questions and Risks

## 0. Confirmed decisions

| ID | Decision | Source |
|---|---|---|
| D1 | Target `net10.0` for all maintained C# projects (Blazor WASM, libraries, tests, Lambdas, C# CDK). Retargeting is executed in **Phase 1A**, not Phase 0. | User instruction |
| D2 | Phase 0 adds only documentation and characterization tests; no AWS infrastructure, no legacy code deletion, no production code changes. | Plan §Phase 0 |
| D3 | Exactly one presented amendment, in a single `PRESENTATION#MAIN` state (resolves Q1). | User, Checkpoint 0 review |
| D4 | Server-side role matrix matches the legacy UI gates. Admin can do everything. Amendment Editor: amendment create/update/archive/clone/approve. Translator: body edits only. Screen Controller: screen and timer. Delete amendment: Admin only. Details in `contracts.md` §4 (resolves Q8; approval role part of Q4). | User, Checkpoint 0 review |
| D5 | Conference approval is all-or-nothing by default: parent-update failures fail the approval with nothing written. Admins can choose `BEST_EFFORT`, which keeps the legacy behavior of succeeding with warnings (resolves the rollback part of Q4 and R2). | User, Checkpoint 0 review |
| D6 | The public presentation returns markdown only for currently visible languages (resolves Q11 and R5). | User, Checkpoint 0 review |
| D7 | Phase 1 domain code lives in the new `src/Amendment.Domain` project (net10.0, no packages), tested by `test/Amendment.Domain.Tests`. Both are in `Amendment.sln`. Legacy projects do not reference it yet. | Phase 1 |
| D8 | Deliberate changes from legacy behavior are marked **[Δ]** in `contracts.md`. Each one is covered by a domain test. | Phase 1 |
| D9 | `global.json` pins SDK `10.0.200` with `rollForward: latestFeature`, so any 10.0 SDK in the 2xx band or newer is accepted. All 13 C# projects target `net10.0` and there are **no legacy exceptions**. Details in §1A. | Phase 1A |

## 1. Legacy test baseline (2026-10-18, SDK 10.0.202, Windows)

| Command | Result |
|---|---|
| `dotnet build Amendment.sln -c Debug` | Succeeded, 0 errors, 61 warnings (mostly nullable CS86xx) |
| `dotnet test Amendment.sln --no-build` | `Amendment.Client.Repository.Tests`: 14/14 passed. `Amendment.Server.Tests`: 45/46 passed, 1 failed |
| `dotnet test src\Amendment.Tests\Amendment.Tests.csproj` | 19/19 passed (project is **not** in `Amendment.sln`) |

- Pre-existing failure: `AmendmentBodyControllerTests.PostAction_Returns_CreatedAmendmentBody` returns **400**. Probable cause [VERIFY]: seeded amendment 1 already has a `LanguageId = 1` body, so `ValidateLanguageId` rejects the duplicate. This is a test-fixture issue; it is left unchanged.
- The test host (`CustomWebApplicationFactory`) always authenticates as `System Administrator`, so role boundaries are not covered by tests.
- Phase 0 tests added: 8 in `AmendmentApprovalTests` (`Characterization_*`), 1 in `ClientNotifierTests`, and 9 theory cases in `AmendmentCleanupServiceTests`. All pass.
- Not available: browser/E2E tests, PostgreSQL integration tests, export layout tests, timer tests.

## 1A. .NET 10 upgrade record (Phase 1A, 2026-10-18, SDK 10.0.202)

**Inventory:** `Amendment.Client` (BlazorWebAssembly), `Amendment.Client.Repository`, `Amendment.Server` (Web), `Amendment.Server.Mediator`, `Amendment.Service`, `Amendment.Repository`, `Amendment.Model`, `Amendment.Shared`, `Amendment.Domain`, plus the tests `Amendment.Tests`, `Amendment.Server.Tests`, `Amendment.Client.Repository.Tests` and `Amendment.Domain.Tests`. All are `net10.0`. The shared build file is `Directory.Build.props` (unchanged). There is no `Directory.Packages.props`.

| Change | Reason |
|---|---|
| Microsoft.AspNetCore.\*, Microsoft.EntityFrameworkCore.\*, Microsoft.Extensions.\*, Microsoft.JSInterop: `9.0.5` → `10.0.13` | Framework alignment |
| `Npgsql.EntityFrameworkCore.PostgreSQL` `9.0.4` → `10.0.3` | Required by EF Core 10 |
| `IRepository.ExecuteUpdate` now takes `Action<UpdateSettersBuilder<T>>` instead of `Expression<Func<SetPropertyCalls<T>,…>>` | EF Core 10 breaking change; callers did not need changes |
| Removed `Microsoft.AspNetCore.OpenApi` from `Amendment.Server` | Unused. Its 10.x version pulls in Microsoft.OpenApi 2.x, which breaks Swashbuckle 8.1.1 (`TypeLoadException` at startup) |
| Removed `Microsoft.Extensions.Logging.Configuration` from `Amendment.Server` | NU1510: now part of the shared framework |
| `src/Amendment.Tests` added to `Amendment.sln` | Closes R10 |
| Dockerfile `aspnet:10.0` / `sdk:10.0`; copies `global.json` and `Directory.Build.props` before restore | SDK pin applies inside Docker |
| New `.github/workflows/ci.yml` using `setup-dotnet` with `global-json-file`: Release build, tests, publish, WASM publish smoke check | Reproducible pipeline. Excludes only the R14 test |

Third-party packages are **unchanged**: Blazorise 1.7.6, Markdig 0.41.1, ClosedXML 0.105.0, QuestPDF 2025.5.0, Mapster 7.4.1-pre01, Moq 4.20.69, Swashbuckle 8.1.1, Autofac, MediatR, FluentValidation and AutoMapper. All of them build and pass tests on `net10.0`.

**Results:**
- Release build: 0 errors.
- Debug build: 61 warnings, all nullable warnings plus NU1903. The count is the same as the Phase 0 baseline.
- Tests: 130/130 Domain, 14/14 Client.Repository, 19/19 `Amendment.Tests`, and 45/46 Server. The one failure is the pre-existing R14 test.
- Docker image builds; size 669 MB.
- Container smoke test with Postgres 17: all 9 migrations applied with EF 10.0.13 and no pending model changes; seed data loaded.
- Container smoke test in Development (in-memory database): `/`, `/api/version`, `/swagger/v1/swagger.json`, `blazor.webassembly.js`, `dotnet.js` and the fingerprinted `Amendment.Client.*.wasm` / `Amendment.Shared.*.wasm` return 200, with Brotli served. No `fail`/`crit` log lines.
- **WASM size baseline:** `_framework` has 303 files, 14.23 MB uncompressed and 4.27 MB Brotli. The full server publish is 216 MB.
- **Not yet verified:** the browser console, the network tab and interactive UI routes (login, editor Markdown preview, screen control, projector) need a manual browser check.

## 2. Open questions (plan §12 plus Phase 0 findings)

| ID | Question | Evidence |
|---|---|---|
| Q1 | **Resolved (D3).** ~~Exactly one live amendment?~~ | — |
| Q2 | Is paging per language or global? Both exist (per-body hub calls and All Prev/Next). *Phase 1 supports both: per-language state with batch changes.* | `ScreenControl.razor` |
| Q3 | Should live updates go out on save (current behavior) or per keystroke? | REST save → notification |
| Q4 | Roles and rollback are **resolved (D4, D5)**. Still open: is a `CONFLICT` (re-plan) the right outcome when approved siblings race on one parent? The domain detects it through body versions. | `UpdateAmendmentCommandHandler` |
| Q5 | Was the parent projector refresh after approval ever observed on anonymous screens? Source says it is not delivered (`AmendmentBodyChange` has no listener). | §3 of `current-state.md` |
| Q6 | What should happen when a projected amendment is archived or deleted? There is no server guard today. *Phase 1 proposal: block both while presented, matching the UI gate for archive.* | `AmendmentRules` |
| Q7 | Search/sort/filter needs for the amendment list? Today the full list loads client-side. | `GetAllAmendmentsHandler` |
| Q8 | **Resolved (D4).** Remaining [VERIFY]: should body delete use the body-edit roles? | `RoleOperationMatrix` |
| Q9 | Is the exact Excel/PDF layout a contract? Are templates or audit requirements needed? | `ExcelExportService`, `PdfExportService` |
| Q10 | What are the production `DbType`, data size, backup state, Redis use, and hosting (Docker/Azure)? | `Program.cs`, pipelines |
| Q11 | **Resolved (D6).** | — |
| Q12 | Is it acceptable that timer state lives in one process (lost on restart, not shared)? | `TimerService` singleton |
| Q13 | Languages are hard-coded in `NavMenu` (English/Spanish/French). Should they be data-driven? | `NavMenu.razor` |
| Q14 | AWS region, DNS, environments, budget, live-update latency SLO. | Plan §12 |
| Q15 | When an amendment is presented, should languages start hidden on page 0 (Phase 1 proposal) or reuse each body's last page and visibility (legacy stored them on the body)? | `PresentationState.Present` |
| Q16 | Is a 100,000-character markdown limit acceptable? | `BodyRules.MaxMarkdownLength` |
| Q17 | Should the legacy title rule that rejects exactly `"not allowed"` be kept? It looks like a leftover test value. | `AmendmentRequestValidator` |

## 3. Migration risk register

| ID | Risk | Likelihood / Impact | Mitigation (phase) |
|---|---|---|---|
| R1 | Approval semantics regress (parent replacement, false→true only, spacing) | M / **High** | Goldens added in Phase 0; port as domain service with the same fixtures (P1/P4) |
| R2 | Partial approval and swallowed parent errors are carried into the new design | H / High | D5: `ApprovalPlan.AtomicBodyWrites` defines the transaction; Phase 4 must implement it with `TransactWriteItems` (watch the 100-item limit) |
| R3 | Projector refresh after approval is already broken; parity is ambiguous | M / High | Confirm Q5. Phase 1: parent writes produce a normal `liveBody.updated` event (A20) |
| R4 | Server authorization is weaker than the UI; a "parity" port could keep the gaps, or tightening could break workflows | H / High | D4 matrix with tests; users who relied on API-only access lose it, so communicate before cutover |
| R5 | Anonymous live endpoint leaks non-live translations | M / M | D6: `PublicProjection` tested |
| R6 | Multiple live amendments possible; `FirstOrDefault` is nondeterministic | M / M | D3: `PresentationState` |
| R7 | Duplicate and mislabeled notifications (`OperationType.Update` sent for create/delete) hide client bugs | M / M | Single event pipeline with typed operations |
| R8 | Raw HTML in Markdown with no sanitizer → XSS on projector/editor | M / High | Sanitize rendered HTML; keep the `<u>` allow-list |
| R9 | ~~.NET 9 → 10 upgrade~~ **Mitigated (Phase 1A):** third-party packages are unchanged and work on `net10.0`. The remaining risk is UI-level regressions not covered by tests (manual browser check pending) | L / M | §1A |
| R10 | ~~`src/Amendment.Tests` is outside the solution~~ **Closed:** added to `Amendment.sln` and run by `ci.yml` | — | — |
| R11 | Timer is in-memory and single-instance | M / M | Persist timer state (target design) |
| R12 | Export output differences (fonts, layout, file names) | M / M | Capture golden files from the legacy app before porting (Q9) |
| R13 | Data migration from PostgreSQL (IDs, audit fields, `ParentAmendmentId` links, users and password hashes) | M / High | Migration tool plus row-count and hash verification; user migration plan (Q10) |
| R14 | Pre-existing failing test masks regressions | L / L | Fix the fixture as a separate change once approved (not yet approved; still failing) |
| R16 | ~~`azure-pipelines.yml` runs `dotnet publish` at the repository root, which picks up `Amendment.sln` and now includes net10.0 projects.~~ **Closed:** the Azure pipeline was unused and `azure-pipelines.yml` has been deleted (user decision). The Docker build (`Amendment.Server` only) is the remaining CI. | — | Phase 1A still pins the SDK in `global.json` and updates the Dockerfile / GitHub workflow SDK |
| R15 | Domain contracts drift from what the legacy migration needs (int ids → string ids, `LanguageId` → codes) | M / M | Phase 3 migration maps `ENG/SPA/FRA` through `LanguageCode` and keeps an id map |
| R17 | NU1903 known-vulnerability warnings: `AutoMapper` 14.0.0 (referenced by `Amendment.Model`) and `Microsoft.AspNetCore.SignalR.Core` 1.1.0 (test-only, `Amendment.Tests`). These existed before Phase 1A | M / M | Upgrading needs a third-party version change, so approval is required. Not changed in Phase 1A |
| R18 | On Windows, `BuildWebCompiler2022` reports `screen.less: --math=always is deprecated` as a build **error**. It happens only on full rebuilds (`--no-incremental`) and also on the pre-upgrade commit. Linux, CI and Docker builds are unaffected because the package is `Windows_NT` only | M / L | Adjust `compilerconfig.json` LESS options or replace the tool (separate change, needs approval) |

## 4. Assumptions (not yet verified)

- A1: Production uses PostgreSQL (`DbType=DB`). The InMemory database is for development and tests.
- A2: There is one conference venue and one presentation session. *(Confirmed by D3.)*
- A3: Projector machines are anonymous (not logged in), so they use screenHub only.
