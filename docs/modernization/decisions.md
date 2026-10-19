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
| D10 | DEV and PROD deploy to `us-east-1`, so the CloudFront certificate and app resources share one region and stack (no cross-region references). | User, Phase 2 |
| D11 | Every stack and taggable resource carries `Application=Amendment` and `Environment=<stage>` (applied at the CDK app root; asserted by tests). | User, Phase 2 |
| D12 | Infrastructure is validated on Floci (`infra/scripts/floci-validate.ps1`) before real deployments, now and in all future phases. | User, Phase 2 |
| D13 | Cognito group names cannot contain spaces, so legacy roles map to `SystemAdministrator`, `ScreenController`, `AmendmentEditor`, `Translator`; the legacy name is the group description (`IdentityStack.RoleGroups`). | Phase 2 |
| D14 | Phase 2 stacks contain no Lambda-backed custom resources (no `autoDeleteObjects`, no `LogRetention`, no `BucketDeployment`). Static files are uploaded by `infra/scripts/publish-web.ps1`, and `destroy-stage.ps1` empties the bucket before `cdk destroy`. This keeps idle cost at zero and works on Floci. | Phase 2 |

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
| New `.github/workflows/ci.yml` using `setup-dotnet` with `global-json-file`: Release build, tests, publish, WASM publish smoke check | Reproducible pipeline. Runs all tests (R14 fixed in §1B) |

Third-party packages were **unchanged** in this step; they were upgraded afterwards in §1B.

**Results:**
- Release build: 0 errors.
- Debug build: 61 warnings, all nullable warnings plus NU1903. The count is the same as the Phase 0 baseline.
- Tests: 130/130 Domain, 14/14 Client.Repository, 19/19 `Amendment.Tests`, and 45/46 Server. The one failure is the pre-existing R14 test.
- Docker image builds; size 669 MB.
- Container smoke test with Postgres 17: all 9 migrations applied with EF 10.0.13 and no pending model changes; seed data loaded.
- Container smoke test in Development (in-memory database): `/`, `/api/version`, `/swagger/v1/swagger.json`, `blazor.webassembly.js`, `dotnet.js` and the fingerprinted `Amendment.Client.*.wasm` / `Amendment.Shared.*.wasm` return 200, with Brotli served. No `fail`/`crit` log lines.
- **WASM size baseline:** `_framework` has 303 files, 14.23 MB uncompressed and 4.27 MB Brotli. The full server publish is 216 MB.
- **Not yet verified:** the browser console, the network tab and interactive UI routes (login, editor Markdown preview, screen control, projector) need a manual browser check.

## 1B. Package upgrade to latest (Phase 1A follow-up, 2026-10-18, user request)

All packages are on their latest stable version, except **MediatR, which stays at `12.5.0`** (user decision).

| Package | From → To | Notes |
|---|---|---|
| AutoMapper | 14.0.0 → **removed** | Closes R17 (CVE-2026-32933). All mappings moved to Mapster `.Adapt<T>()`; `MappingProfile.cs` and `RegisterMapperProfile.cs` deleted |
| Microsoft.AspNetCore.SignalR.Core | 1.1.0 → **removed** | Closes R17. Test-only, unused (`Amendment.Tests`) |
| Mapster | 7.4.1-pre01 → 10.0.13 | Same version in every project (avoids NU1605 downgrade) |
| Blazorise.\* (Bootstrap5, Components, FluentValidation, Icons.FontAwesome, Markdown, Snackbar) | 1.7.6 → 2.4.0 | Breaking component/parameter renames fixed in client `.razor` files |
| Swashbuckle.AspNetCore | 8.1.1 → 10.3.0 | Uses Microsoft.OpenApi 2.x; serves OpenAPI 3.0.4 |
| FluentValidation (+ DI extensions) | 11.11.0 → 12.2.0 | |
| Autofac / Autofac.Extensions.DependencyInjection | 8.3.0 / 10.0.0 → 9.3.4 / 11.0.2 | |
| Markdig | 0.41.1 → 1.4.1 | |
| QuestPDF | 2025.5.0 → 2026.9.1 | |
| ClosedXML | 0.105.0 → 0.105.1 | |
| HtmlAgilityPack | 1.12.1 → 1.13.1 | |
| Lorem.Universal.NET | 4.0.80 → 5.0.0 | |
| Microsoft.IdentityModel.Tokens / System.IdentityModel.Tokens.Jwt | 8.10.0 → 8.23.0 | |
| Microsoft.VisualStudio.Azure.Containers.Tools.Targets | 1.21.2 → 1.23.0 | |
| Test: Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, coverlet.collector, Moq, NSubstitute | → 18.11.0, 2.9.3, 4.0.1, 10.1.0, 4.21.0, 6.2.0 | Unused `Newtonsoft.Json` usings removed from server tests |

**R14 fix:** `PostAction_Returns_CreatedAmendmentBody` now posts a body in a language that amendment 1 does not already have, so it no longer collides with the seeded `LanguageId = 1` body. `ci.yml` no longer filters it out.

**Results:**
- Release build: 0 errors. Debug build: 60 warnings (nullable only; no NU1903).
- `dotnet list package --vulnerable --include-transitive`: none.
- Tests: 130/130 Domain, 14/14 Client.Repository, 19/19 `Amendment.Tests`, 46/46 Server (stable across repeated runs).
- Docker image builds (645 MB). Development-mode container smoke test: `/`, `/api/version`, `/swagger/v1/swagger.json` (19 paths), `/swagger/index.html`, Blazorise CSS under `_content/` and `blazor.webassembly.js` return 200. No `fail`/`crit` log lines.
- **WASM size:** `_framework` is 18.16 MB uncompressed and 5.54 MB Brotli (was 4.27 MB). `Blazorise.wasm` alone is 341 KB Brotli. Full server publish is 203 MB.
- **Still pending:** the manual browser check from §1A, now also covering the Blazorise 2 UI.

**Follow-ups (not done, need approval):**
- `xunit` 2.9.3 is marked legacy; the successor is `xunit.v3` (a different package, so it counts as a new dependency).
- ~~`Blazored.LocalStorage` 4.5.0 reports "Not found at the sources"~~ **Resolved 2026-10-18:** the package is deprecated, its repository is archived, and every version is unlisted on nuget.org. It was replaced by an in-project `ILocalStorageService`/`LocalStorageService` (`src/Amendment.Client/Services/LocalStorageService.cs`) that calls `localStorage` through `IJSRuntime`. Values are still stored as JSON, so tokens already saved in users' browsers remain readable. No new dependency was added.

## 1C. CDK foundation record (Phase 2, 2026-10-18)

- **Project:** `infra/Amendment.Cdk` (`net10.0`, `Amazon.CDK.Lib` 2.273.0, `Constructs` 10.8.1). CDK CLI pinned to `aws-cdk` 2.1145.0 in `infra/Amendment.Cdk/package.json`. Tests: `test/Amendment.Cdk.Tests` (27 assertions). Both projects are in `Amendment.sln`.
- **Stacks per stage:** `Amendment-{stage}-Data` (DynamoDB `AmendmentData-{stage}`, PK/SK, on-demand, stream), `-Identity` (Cognito pool, 4 groups, public PKCE client), `-Api` (AppSync, Cognito default + IAM, placeholder `apiVersion` query, ERROR logs), `-Web` (private S3 + CloudFront OAC, ACM DNS-validated certificate, A/AAAA aliases in the imported `adamlj.com` zone `Z3A6EHPCI5BABT`), `-Observability` (SNS alert topic, AppSync 5XX alarm, monthly budget filtered on `user:Application$Amendment`).
- **Retention:** DEV is fully disposable (`DESTROY`). PROD retains the table, user pool, bucket and log group, enables deletion protection and point-in-time recovery, and sets stack termination protection.
- **CI:** `ci.yml` installs Node 24 and runs `npx cdk synth` for `dev` and `prod` after the tests.
- **Real DEV deploy (account via `default` profile):** all five stacks deployed in about 9 minutes. Checks passed: `https://dev-amendments.adamlj.com` serves the Blazor client over TLS 1.3 with an Amazon-issued certificate; HTTP redirects to HTTPS (301); unknown routes fall back to `index.html`; `.wasm` is served as `application/wasm`; HSTS is present; direct S3 access returns 403; anonymous AppSync calls return 401; zone NS records match the public delegation. The old `amendments`/`dev-amendments` CNAMEs were deleted by the user beforehand. The client loads, but API calls fail until the backend exists (Phases 3–8).
- **Floci (2.3.0):** bootstrap, deploy, publish and destroy of all five stacks pass. The container needs `/var/run/docker.sock` mounted, because AppSync schema validation runs in a sidecar container. Floci's state is in memory, so the script bootstraps a fresh container each run.

**GitHub OIDC deployment plan (AWS side done; workflow not yet implemented):**
1. **Done (user, manually):** IAM OIDC identity provider for `token.actions.githubusercontent.com` (audience `sts.amazonaws.com`). It is not managed by CDK.
2. **Done:** roles `amendment-github-deploy-dev` and `amendment-github-deploy-prod`, created with the AWS CLI (not CDK). Each trusts only `repo:AdamLJohnson/Amendment:environment:<stage>`. A job that declares a GitHub environment gets an `environment:` subject, not a `ref:` subject; limit each environment's deployment branches to `main` in GitHub. Inline policy `<role>-permissions`: `sts:AssumeRole`/`sts:TagSession` on the CDK bootstrap deploy, file-publishing and lookup roles; `s3:ListBucket` on `amendment-<stage>-web-sitebucket*` and `s3:PutObject`/`DeleteObject` on its objects; `cloudfront:CreateInvalidation` on `distribution/*` (any distribution, user decision). Maximum session 1 hour.
3. Workflow: on push to `main`, run build, test, `cdk diff`, `cdk deploy --all -c stage=dev` and `publish-web.ps1`. Floci validation runs first in the same job. Each deploy job declares `environment: dev|prod`, `permissions: id-token: write`, and passes `role-to-assume: ${{ secrets.AWS_DEPLOY_ROLE_ARN }}`. The ARN is stored as an **environment secret** named `AWS_DEPLOY_ROLE_ARN` in each GitHub environment (user decision), not as a variable.
4. The `prod` GitHub environment needs a required reviewer. That job posts `cdk diff -c stage=prod` for approval before deploying.
5. Later: replace the bootstrap `AdministratorAccess` execution policy with a scoped policy (`cdk bootstrap --cloudformation-execution-policies`).

## 1D. DynamoDB persistence and seed record (Phase 3, 2026-10-19)

- **Projects:** `src/Amendment.Infrastructure` (`AWSSDK.DynamoDBv2` 4.0.107.3, `Ulid` 1.4.1), the seeding tool `scripts/seed-dynamodb` (`Amendment.SeedDynamoDb`), and tests in `test/Amendment.Infrastructure.Tests`. All are in `Amendment.sln`.
- **Store (`AmendmentStore`):** creates use `attribute_not_exists(PK)`, so a duplicate is a `Conflict`. Updates are compare-and-swap on `version`; a stale version is a `Conflict` and a missing item is `NotFound`. A body create fails with `NotFound` when its amendment does not exist. Point reads and partition queries are strongly consistent. New amendment ids are ULIDs.
- **GSI1 (index name `GSI1`; C# constants `DataStack.AmendmentListIndex` and `AmendmentTable.Gsi1`):** `GSI1PK = AMENDMENTS#ACTIVE|ARCHIVED`, `GSI1SK = <created ISO-8601>#<id>`, projection `ALL`. Only `META` items carry these keys. This is the only GSI. `AmendmentTable.CreateTableRequest` mirrors `DataStack`, and a CDK test checks the two match. Page tokens are bound to their list; a token from another list is rejected as a validation error. Page size is 1–100.
- **Eventual consistency (found in AWS tests, not shown by Floci):** GSI1 reads can lag writes on AWS. The listing tests wait for the index. The seeder decides "other amendments exist" with a strongly consistent scan (`ScanAmendmentIdsAsync`), not GSI1. Callers of `ListAmendmentsAsync` must not expect to see their own write immediately.
- **Seed:** `scripts/seed-dynamodb/seed-fixture.json` holds 37 items: 3 languages, 3 settings, 8 amendments, 22 bodies and the presentation. Its checksum is `fa652d90…4022`. `SeedFixtureParityTests` compares it with `AmendmentContext`/`SeedDatabase`. Set `AMENDMENT_REGENERATE_SEED_FIXTURE=1` to rewrite it after a legacy seed change.
- **Validation:** the integration tests (22) pass on Floci (`AMENDMENT_DYNAMODB_TEST_ENDPOINT`) and on real AWS (`AMENDMENT_DYNAMODB_TEST_AWS=1`, temporary tagged tables that are deleted afterwards). `floci-validate.ps1` passes with GSI1. `Amendment-dev-Data` was deployed to DEV (adding GSI1 in place). The DEV seed wrote 37 items, a reseed wrote 0, and verify passed with matching checksums.
- **Backup/rollback:** DEV is disposable: destroy and redeploy the stage, then rerun `seed`. PROD has point-in-time recovery and deletion protection. To roll back, restore the table to a point in time as a new table, verify it with `verify --table <restored>`, then point the stage at it or copy the items back. A reseed never overwrites items, so it is safe after a partial restore. No dual-system sync is needed.

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
| Q10 | What are the production `DbType`, data size, backup state, Redis use, and hosting (Docker/Azure)? Partly answered: there is no production database to migrate (user decision) | `Program.cs`, pipelines |
| Q11 | **Resolved (D6).** | — |
| Q12 | Is it acceptable that timer state lives in one process (lost on restart, not shared)? | `TimerService` singleton |
| Q13 | Languages are hard-coded in `NavMenu` (English/Spanish/French). Should they be data-driven? | `NavMenu.razor` |
| Q14 | Region, DNS and environments are **resolved (D10, §1C)**. Still open: the budget alert email (placeholder `alertEmail` in `stages.json`) and the live-update latency SLO. | Plan §12 |
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
| R13 | DynamoDB seed drifts from the legacy seed (IDs, audit fields, languages, live state, settings). There is no PostgreSQL data to migrate (user decision) | L / M | Fixed seed fixture checked against `AmendmentContext` and `SeedDatabase`; count and checksum verification (Phase 3, A15); the legacy `admin` user is replaced in Cognito (Phase 8) |
| R14 | ~~Pre-existing failing test masks regressions~~ **Closed (§1B):** test fixture fixed; CI runs all tests | — | — |
| R16 | ~~`azure-pipelines.yml` runs `dotnet publish` at the repository root, which picks up `Amendment.sln` and now includes net10.0 projects.~~ **Closed:** the Azure pipeline was unused and `azure-pipelines.yml` has been deleted (user decision). The Docker build (`Amendment.Server` only) is the remaining CI. | — | Phase 1A still pins the SDK in `global.json` and updates the Dockerfile / GitHub workflow SDK |
| R15 | Domain contracts drift from what the legacy migration needs (int ids → string ids, `LanguageId` → codes) | M / M | Phase 3 seeding maps `ENG/SPA/FRA` through `LanguageCode`; there is no legacy database to migrate (user decision) |
| R17 | ~~NU1903 known-vulnerability warnings: `AutoMapper` 14.0.0 and `Microsoft.AspNetCore.SignalR.Core` 1.1.0~~ **Closed (§1B):** both removed; no vulnerable packages reported | — | — |
| R18 | On Windows, `BuildWebCompiler2022` reports `screen.less: --math=always is deprecated` as a build **error**. It happens only on full rebuilds (`--no-incremental`) and also on the pre-upgrade commit. Linux, CI and Docker builds are unaffected because the package is `Windows_NT` only | M / L | Adjust `compilerconfig.json` LESS options or replace the tool (separate change, needs approval) |
| R19 | Blazorise has moved to a commercial license. 2.4.0 pulls in `Blazorise.Licensing` 1.3.0 transitively. Not blocking now (user decision, 2026-10-18) | M / M | Deferred: choose and migrate to a replacement UI component library in a later phase. Used by `Amendment.Client` via `Blazorise.Bootstrap5`, `.Components`, `.FluentValidation`, `.Icons.FontAwesome`, `.Markdown`, `.Snackbar` |

| R20 | Floci is an emulator. It confirms that templates deploy, update and delete, but not real AWS behavior: CloudFront serving, ACM validation timing, IAM enforcement and AppSync resolver results | M / M | Floci is a gate before real DEV deploys, not a replacement for them; check DEV in a browser after deploying |
| R21 | The budget filters on the `Application` tag, so it reports $0 until `Application` is activated as a cost allocation tag in the Billing console. It also covers all Amendment stages together | H / L | Activate the cost allocation tag (one-time console step). Add an `Environment` filter later if per-stage budgets are needed |

## 4. Assumptions (not yet verified)

- A1: Production uses PostgreSQL (`DbType=DB`). The InMemory database is for development and tests.
- A2: There is one conference venue and one presentation session. *(Confirmed by D3.)*
- A3: Projector machines are anonymous (not logged in), so they use screenHub only.
