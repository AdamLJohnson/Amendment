# Modernization Decisions, Baseline, Questions and Risks

## 0. Confirmed decisions

| ID | Decision | Source |
|---|---|---|
| D1 | Target `net10.0` for all maintained C# projects (Blazor WASM, libraries, tests, Lambdas, C# CDK). Retargeting is executed in **Phase 1A**, not Phase 0. | User instruction |
| D2 | Phase 0 adds only documentation and characterization tests; no AWS infrastructure, no legacy code deletion, no production code changes. | Plan §Phase 0 |

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

## 2. Open questions (plan §12 plus Phase 0 findings)

| ID | Question | Evidence |
|---|---|---|
| Q1 | Exactly one live amendment? The server doesn't enforce it. `GetLiveAsync` returns the first `IsLive` row, and the UI hides Go Live while one is live. | `AmendmentService.GetLiveAsync`, `AmendmentTableRow` |
| Q2 | Is paging per language or global? Both exist (per-body hub calls and All Prev/Next). | `ScreenControl.razor` |
| Q3 | Should live updates go out on save (current behavior) or per keystroke? | REST save → notification |
| Q4 | Approval: expected result when several approved siblings share a parent (currently last write wins)? Rollback expectations? Should approval require a role? (Currently any authenticated user can approve via PUT.) | `UpdateAmendmentCommandHandler` |
| Q5 | Was the parent projector refresh after approval ever observed on anonymous screens? Source says it is not delivered (`AmendmentBodyChange` has no listener). | §3 of `current-state.md` |
| Q6 | What should happen when a projected amendment is archived or deleted? There is no server guard today. | — |
| Q7 | Search/sort/filter needs for the amendment list? Today the full list loads client-side. | `GetAllAmendmentsHandler` |
| Q8 | Are the server role boundaries intentional (e.g., Translators can delete amendments and change settings via the API)? Which roles must be enforced server-side? | Role matrix |
| Q9 | Is the exact Excel/PDF layout a contract? Are templates or audit requirements needed? | `ExcelExportService`, `PdfExportService` |
| Q10 | What are the production `DbType`, data size, backup state, Redis use, and hosting (Docker/Azure)? | `Program.cs`, pipelines |
| Q11 | Should the anonymous `GET api/Amendment/Live` exposing non-live language bodies be kept? | `GetLiveAmendmentHandler` |
| Q12 | Is it acceptable that timer state lives in one process (lost on restart, not shared)? | `TimerService` singleton |
| Q13 | Languages are hard-coded in `NavMenu` (English/Spanish/French). Should they be data-driven? | `NavMenu.razor` |
| Q14 | AWS region, DNS, environments, budget, live-update latency SLO. | Plan §12 |

## 3. Migration risk register

| ID | Risk | Likelihood / Impact | Mitigation (phase) |
|---|---|---|---|
| R1 | Approval semantics regress (parent replacement, false→true only, spacing) | M / **High** | Goldens added in Phase 0; port as domain service with the same fixtures (P1/P4) |
| R2 | Partial approval and swallowed parent errors are carried into the new design | H / High | Transactional writes and detectable failure, subject to approval (P4) |
| R3 | Projector refresh after approval is already broken; parity is ambiguous | M / High | Confirm Q5; target authorized live-body event (A20) |
| R4 | Server authorization is weaker than the UI; a "parity" port could keep the gaps, or tightening could break workflows | H / High | Decide Q8; add role tests; Cognito groups enforced server-side |
| R5 | Anonymous live endpoint leaks non-live translations | M / M | Decide Q11; public projection returns only live bodies |
| R6 | Multiple live amendments possible; `FirstOrDefault` is nondeterministic | M / M | Decide Q1; single presentation-session record |
| R7 | Duplicate and mislabeled notifications (`OperationType.Update` sent for create/delete) hide client bugs | M / M | Single event pipeline with typed operations |
| R8 | Raw HTML in Markdown with no sanitizer → XSS on projector/editor | M / High | Sanitize rendered HTML; keep the `<u>` allow-list |
| R9 | .NET 9 → 10 upgrade (Blazorise, Markdig, ClosedXML, QuestPDF, Mapster pre-release, Moq 4.20, old `Microsoft.AspNetCore.SignalR.Core 1.1.0` in tests) | M / M | Phase 1A per-package compatibility check; `global.json` pin |
| R10 | `src/Amendment.Tests` is outside the solution, so CI may skip it | H / M | Add it to the solution or the CI command in Phase 1A [VERIFY CI] |
| R11 | Timer is in-memory and single-instance | M / M | Persist timer state (target design) |
| R12 | Export output differences (fonts, layout, file names) | M / M | Capture golden files from the legacy app before porting (Q9) |
| R13 | Data migration from PostgreSQL (IDs, audit fields, `ParentAmendmentId` links, users and password hashes) | M / High | Migration tool plus row-count and hash verification; user migration plan (Q10) |
| R14 | Pre-existing failing test masks regressions | L / L | Fix the fixture in Phase 1 (separate, approved change) |

## 4. Assumptions (not yet verified)

- A1: Production uses PostgreSQL (`DbType=DB`). The InMemory database is for development and tests.
- A2: There is one conference venue and one presentation session.
- A3: Projector machines are anonymous (not logged in), so they use screenHub only.
