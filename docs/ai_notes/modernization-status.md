# Modernization status — notes for future AI sessions

Read first: `AMENDMENT_MODERNIZATION_PLAN.md` (checkboxes track progress), then `docs/modernization/decisions.md`.

## Where we are

| Phase | Status | Branch / commit |
|---|---|---|
| 0 Baseline | Approved (Checkpoint 0) | committed on `main` as "Phase 0" |
| 1 Domain contracts | Approved (Checkpoint 1) | committed on `modernization/phase-1-domain` as "Phase 1" |
| 1A .NET 10 retarget | Approved (Checkpoint 1A) | `modernization/phase-1a-net10` |
| 1A follow-up: packages to latest, R14/R17 fixed | Approved (decisions.md §1B) | same branch |
| 2 CDK foundation | Approved (Checkpoint 2, decisions.md §1C) | committed as "Phase 2" |
| 3 DynamoDB persistence and seed data | Approved (Checkpoint 3, decisions.md §1D). DEV deployed with GSI1 and seeded | committed as "Phase 3" |
| 4 GraphQL amendment API | Approved (Checkpoint 4, decisions D15–D20, §1E). DEV deployed | committed as "Phase 4", merged to `main` |

## Rules the user set

- One phase at a time. Stop at each checkpoint for approval. Tick plan checkboxes as items complete.
- Target is `net10.0` everywhere. Every project is now `net10.0`; SDK pinned by `global.json` (D9).
- Ask before changing third-party package versions. MediatR stays at `12.5.0`.
- Do not delete legacy code before cutover. Validate every infrastructure change on Floci first (D12). DEV may be deployed to AWS with the `default` profile after Floci passes; PROD needs explicit approval.
- Tag everything `Application=Amendment` + `Environment=<stage>` (D11).
- GitHub Actions deploys use the role ARN from the **secret** `AWS_DEPLOY_ROLE_ARN` (`${{ secrets.AWS_DEPLOY_ROLE_ARN }}`), defined per GitHub environment (`dev`, `prod`). Do not switch it to a variable. The roles exist already; see the OIDC plan in decisions.md §1C.
- There is no legacy database to migrate. All environments are seeded from the legacy seed data (Phase 3 tool); do not plan PostgreSQL export/import.
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
- Code map (Phase 4): schema `infra/Amendment.Cdk/Schema/amendment.graphql`, read resolvers `infra/Amendment.Cdk/Resolvers`, mutations `src/Amendment.Api.Lambda`, typed Blazor client `src/Amendment.Client.Repository/GraphQL`, flag wiring `src/Amendment.Client/AuthProviders/CognitoGraphQlPreview.cs`, pages `Pages/GraphQlPreview.razor` and `Pages/Authentication.razor`.

## Commands

- `dotnet build Amendment.sln -c Release`
- `dotnet test Amendment.sln -c Release --no-build` (includes `src/Amendment.Tests`)
- `dotnet publish src/Amendment.Server/Amendment.Server.csproj -c Release -o artifacts/publish`
- CI: `.github/workflows/ci.yml`. Docker: `docker build -f src/Amendment.Server/Dockerfile .`
- Seed/verify: `dotnet run --project scripts/seed-dynamodb -- seed|verify --stage dev` (`--endpoint-url` for Floci; PROD needs `--confirm-prod`).
- DynamoDB integration tests: set `AMENDMENT_DYNAMODB_TEST_ENDPOINT=http://localhost:4566` (Floci) or `AMENDMENT_DYNAMODB_TEST_AWS=1` (real AWS); otherwise they are skipped.
- Infra (see README "AWS infrastructure"): from `infra/Amendment.Cdk`, `npm ci` then `npx cdk synth|diff|deploy --all -c stage=dev`. Floci gate: `powershell -File infra/scripts/floci-validate.ps1`. Web upload: `infra/scripts/publish-web.ps1 -Stage dev [-EnableCognitoPreview]` (needs `cdk deploy --outputs-file cdk-outputs-dev.json`; the switch turns on the D17 flag for that upload only). Teardown: `infra/scripts/destroy-stage.ps1 -Stage dev`. Synth/deploy needs the published API Lambda: `dotnet publish src/Amendment.Api.Lambda/Amendment.Api.Lambda.csproj -c Release -o artifacts/lambda/api`.
- Deployed API tests (D18): `powershell -File infra/scripts/test-api-dev.ps1 [-Deploy]` creates throwaway per-role Cognito users, runs `DeployedApiTests` against the DEV GraphQL endpoint, then deletes the users (`-Deploy` publishes the Lambda and deploys DEV first).

## Known baseline issues

- 60 legacy build warnings (nullable only). R14 and R17 are closed.
- Windows full rebuilds hit the WebCompiler LESS error R18. Use an incremental build.

## Gotchas

- In test code, a static field named `Single` collides with `System.Single`. That is why the fixture is called `OneSlide`.
- Legacy approval cleans the child first, then cleans that result again for the parent. The domain keeps this behavior; see the `LegacyParity_*` test.
- Object mapping uses Mapster (`.Adapt<T>()`); AutoMapper was removed. Keep the Mapster version identical across projects.
- Blazorise is 2.x; 1.x component names/parameters no longer compile. Blazorise is now commercially licensed; replacement is deferred to a later phase (risk R19).
- The Docker container defaults to `Production` (`DbType=DB`) and needs `ConnectionStrings__DefaultConnection`.
- CDK tests: the jsii runtime is not thread-safe, so `Amendment.Cdk.Tests` disables xUnit parallelization. `Amendment.Cdk.csproj` excludes `node_modules/` (the CDK CLI ships `.cs` templates that would otherwise compile).
- AppSync L2 `LogConfig` and S3 `autoDeleteObjects` add Lambda custom resources. Avoid them (D14); `ApiStack` sets `LogConfig` on the L1 resource.
- Scripts must run on Windows PowerShell 5.1: no `??`/ternary, and call the native `npx` (not the `npx.ps1` shim) when splatting arguments. Floci needs the Docker socket mounted.
- GSI1 is eventually consistent on AWS but immediate on Floci. Passing on Floci is not enough for anything that reads the index; run the AWS tests too.
- Code map (Phase 3): `src/Amendment.Infrastructure/DynamoDb` (`AmendmentStore`, `AmendmentTable`, `ItemMapper`, `PageToken`), `scripts/seed-dynamodb` (`SeedFixture`, `Seeder`, `SeedVerifier`).
- Floci tests must not leak into real deployments: always clear `AWS_ENDPOINT_URL` and the test credentials before using the `default` profile.
- With the Cognito preview flag on, the legacy `AuthStateProvider` is not registered (the OIDC library owns `AuthenticationStateProvider`), so legacy login/logout and legacy API pages don't work in that mode. Deployed tests use access tokens, like the browser.
- Floci does not validate AppSync resolver code. Check changed resolvers with `aws appsync evaluate-code` (runtime `APPSYNC_JS` 1.0.0, `_shared.js` prepended). APPSYNC_JS rejects an arrow function as an `Array.sort` comparator and ignores a function comparator, so `Query.languages.js` uses an insertion sort.
