# Amendment — AWS-Native Modernization Blueprint and Phased Implementation Plan

**Source repository:** https://github.com/AdamLJohnson/Amendment  
**Audience:** AI coding assistant and human maintainer  
**Status:** Proposed implementation blueprint; validate details against the current checkout before changing code.  
**Primary objective:** Preserve Amendment's existing operational behavior while removing always-on infrastructure and minimizing idle AWS cost.

---

## 0. Instructions for the AI Coding Agent (Read First)

This document describes the **target architecture and migration process**, not permission to replace the application in one pass.

1. **Inspect the current repository first.** Inventory actual features, routes, roles, handlers, validation, data models, real-time messages, tests, exports, imports, settings, deployment scripts, and current .NET versions. The source of truth for *existing* behavior is the repository, not assumptions in this plan.
2. Create a dedicated modernization branch. Do **not** delete the legacy implementation until parity tests and cutover gates pass.
3. Implement **one phase at a time**, in the order below. Each phase must produce a small, reviewable PR with tests, documentation, a demonstration, and a checkpoint report. **Stop after each phase and ask for approval before starting the next.**
4. **Target .NET 10 LTS (`net10.0`) across all maintained C# projects**, including Blazor WASM, domain/contracts/infrastructure, Lambda functions, tests, migration utilities, and **AWS CDK v2 in C#**. Use AppSync JavaScript resolvers only where they simplify straightforward data operations.
5. Preserve **Blazor WebAssembly**, existing markdown slide semantics, localization, translator editing behavior, projector experience, and import/export behavior unless explicitly approved otherwise.
6. Prefer managed, pay-per-use AWS services. Avoid EC2, ECS/Fargate, RDS, Redis, VPCs and NAT gateways for the target architecture unless a verified requirement demands them.
7. **Never rely on client validation or UI hiding for security.** Authenticate/authorize on the server, enforce persistence invariants with conditional/transactional writes, and test anonymous access explicitly.
8. **No new approval gate for translators.** If a body is currently projected, a trusted translator's successful save must appear on the corresponding projector promptly and automatically.
9. Avoid speculative refactoring. Preserve existing semantics unless a change is explicitly listed, justified and covered by tests. Record discoveries, questions and decisions in `docs/modernization/decisions.md`.
10. Verify current AWS service behavior, pricing, CDK package compatibility and Lambda runtime configuration before implementing each integration. AWS Lambda supports the managed **`dotnet10`** runtime; use it for deployed managed C# functions.
11. Leave notes for future AI coding assistant sessions in docs/ai_notes/*.md so that they can pick up where you leave off for seamless future work.

### Required output at the end of every phase

- Files changed and architectural decisions made.
- Commands executed and pass/fail results (build, tests, lint, `cdk synth`, smoke tests).
- Deployed AWS resources and estimated recurring/usage costs, if any.
- Screenshots or a short walkthrough for UI-visible work.
- Open risks, known regressions, migration impact, rollback instructions.
- Explicit **PASS / FAIL** against that phase's exit criteria.
- A proposed scope for the *next* phase; **do not automatically continue**.

---

## 1. Goals, Non-Goals and Fundamental Decisions

### Goals

- Reduce **idle cost toward zero** for compute/data APIs (domain, DNS, storage and other fixed charges may remain).
- Eliminate the persistent ASP.NET Core host, container hosting, PostgreSQL/EF Core infrastructure, Redis and server-side SignalR from the final deployment.
- Maintain real-time synchronized multilingual projection and operator controls.
- Keep the existing Blazor WebAssembly user interface, improving its API/auth/event adapters in controlled increments.
- Enable safe concurrent translator editing, durable presentation state, and page/timer recovery after disconnects.
- Deliver fully reproducible **CDK C# infrastructure**, repeatable CI/CD, observable failures, and rollback capability.
- Build a low-cost developer/test environment with seeded example data and meaningful automated tests.
- Upgrade all maintained C# projects and new C# infrastructure code to **.NET 10 LTS**, using a consistent SDK/toolchain and compatible NuGet dependencies.

### Non-goals (initial release)

- Rewriting the UI in React or introducing a new frontend framework.
- Microservices for their own sake; adding Step Functions, EventBridge, SNS/SQS, or Lambda to every CRUD request.
- Automatically implementing an operator approval/publish step after translators edit live content.
- Redesigning the existing `**NEWSLIDE**` slide separator unless required by proven behavior or tests.
- Adding a general history/event-sourcing platform, per-slide entity model, or elaborate workflow engine.
- Migrating everything in one PR.

### Confirmed decisions from design discussions

| Concern | Decision |
|---|---|
| Frontend | Keep Blazor WebAssembly, serve static assets from S3 via CloudFront. |
| Data | DynamoDB on-demand with access-pattern-driven keys; no automatic SQL-to-NoSQL table translation. |
| Queries and commands | AppSync GraphQL; direct DynamoDB resolvers for safe, simple operations; C# Lambda for complex use cases. |
| Real time | AppSync Events; DynamoDB is the authoritative source, events notify clients to refresh. |
| Editor updates | **Trusted translators' saves to currently live body content display immediately**, without additional operator approval. |
| Conference approval workflow | **Preserve the existing `UpdateAmendmentCommandHandler` approval transition (`isBeingApproved`) and parent-amendment cleanup/markup behavior** as a business-critical workflow; this is distinct from translator live-edit publishing. |
| Projection | Anonymous **read-only** presentation access; private amendments/archived drafts must not become publicly enumerable. |
| Identity | Cognito for authenticated users and role/group claims. |
| Timer | Store clock state/timestamps, not per-second server ticks; browsers calculate countdown locally. |
| IaC | AWS CDK v2 in **.NET 10 C#**, deployed using `cdk synth`, `cdk diff`, `cdk deploy`. |
| .NET baseline | **`net10.0` for all actively maintained C# projects**; Lambda managed runtime **`dotnet10`**; C# 14 where appropriate. |
| Deployment | No VPC/NAT gateway by default. |

---

## 2. Current-State Discovery and Parity Inventory

The repository contains a Blazor client, ASP.NET server, layered handlers/services/repositories, PostgreSQL/EF, SignalR hubs, and timer-related server state. The README also documents Redis and traditional Linux hosting. **Do not assume that all README instructions are current**; inspect running code and current CI configuration.

Before design finalization, build a checklist of *all observable behavior*, including:

- **.NET upgrade inventory:** list all `.csproj`/`.fsproj`, `global.json`, `Directory.Build.props/targets`, central NuGet package versions, SDK/container dependencies, test SDKs, workloads, publish settings, code generators and CI SDK pins; identify package/API incompatibilities with .NET 10.

- Amendment lifecycle: create, edit, clone, parent/child association, approval/status, archive/unarchive, deletion/restore, lists, search/sorting. **Specifically trace `UpdateAmendmentCommandHandler`, its `isBeingApproved` condition, parent-update behavior, and the cleanup service that applies strikethrough/underline semantics.** Capture exact trigger, inputs, outputs, persisted changes, edge cases, and current test coverage.
- Translation lifecycle: languages, create/update/delete, content status, permissions, concurrent saves, markdown and `**NEWSLIDE**` behavior.
- Projection: selection of live amendment, independent language visibility, pages, multi-projector synchronization, blank/cleared screen, refresh/reconnect.
- Timer: set/start/pause/resume/reset/show/hide, end-of-timer behavior, synchronization across clients.
- Identity: current roles and actual authorization boundaries, anonymous behaviors, login/logout/password administration.
- User interface: operator routes, translator routes, public routes, settings, localization, notifications.
- Imports/exports: PDF/Excel layout, filtering, file naming and browser download behavior.
- Data and deployment: row counts/data size, migrations, existing data backup, dependencies, secrets, tests, pipelines and hosted configurations.

**Deliverables:** `docs/modernization/current-state.md`, feature-parity matrix and prioritized gap/decision log. Tag unclear behavior as `VERIFY`, not as an assumed requirement.

---

## 2B. Business-Critical Conference Approval / Parent Cleanup Workflow (SOURCE VERIFIED; MUST PRESERVE)

**Priority: High — proven valuable during a live conference.** This is the conference's **formal approval of a child amendment**, not an approval gate for trusted translators' live edits. The following was verified directly against:

- [`UpdateAmendmentCommandHandler.cs`](https://github.com/AdamLJohnson/Amendment/blob/main/src/Amendment.Server.Handlers/Handlers/AmendmentHandlers/UpdateAmendmentCommandHandler.cs)
- [`AmendmentCleanupService.cs`](https://github.com/AdamLJohnson/Amendment/blob/main/src/Amendment.Service/AmendmentCleanupService.cs)

### Verified current workflow (source-of-truth for characterization tests)

1. The handler loads an existing amendment; if missing, it returns **404**.
2. It captures the *prior* `IsApproved`, maps the incoming request onto the amendment using Mapster, and computes **`isBeingApproved = !wasApproved && amendment.IsApproved`**. Only the **false → true** transition triggers cleanup/propagation. Re-saving an already approved amendment does not trigger this branch.
3. It loads **all language bodies** of the newly approved amendment. If `request.UpdateCurrentAmendmentBodies` is true, each child body is cleaned with `CleanupAmendmentText`. **Only changed child bodies** are persisted through `IAmendmentBodyService.UpdateAsync`, with updated author/timestamp; this path triggers the existing body notifications. If the option is false, the child's marked-up bodies are left unchanged.
4. **Regardless of the `UpdateCurrentAmendmentBodies` flag**, if the approved amendment has `ParentAmendmentId`, the handler attempts to update that parent's bodies. It fetches the parent and its bodies, **matches each child body to a parent body by `LanguageId`**, applies cleanup to the approved child body's text, and **replaces the matching parent's entire `AmendBody`** with the cleaned result if different. It does **not** incrementally patch the parent's old text. Missing parent or unmatched language is skipped.
5. Each changed parent body is saved through `IAmendmentBodyService.UpdateAsync`, with author/timestamp. **If `parentAmendment.IsLive`**, the handler additionally calls `IScreenControlService.UpdateBodyAsync(parentBody, forceSend: true)` to force a projector refresh. The new architecture must preserve the user-visible refresh, but use the authorized live-body event pipeline instead of this legacy SignalR call.
6. Only **after** these body updates, the handler calls `IAmendmentService.UpdateAsync` to save the approved child amendment metadata. This ordering and the surrounding error handling allow **partial updates** in the current implementation; they are not desirable properties to reproduce.
7. The parent-update helper **catches/logs all exceptions and does not rethrow**; therefore formal approval can succeed even when a parent update fails. The modernized operation must make this failure detectable and recoverable rather than silently accepting inconsistent child/parent state. The option to fix this behavior should be reviewed/approved as a deliberate reliability improvement.

### Exact cleanup semantics — not 'retain tracked-change markup'

`CleanupAmendmentText` uses two regular expressions, in this order:

- `~~(.*?)~~` with `RegexOptions.Singleline` → **remove the entire struck-through span**, including the text (deletion accepted).
- `<u>(.*?)</u>` with `RegexOptions.Singleline | RegexOptions.IgnoreCase` → **keep the underlined text but remove the `<u>` tags** (addition accepted).

Thus `The ~~old~~ <u>new</u> wording` becomes `The  new wording` (the double space is preserved; there is **no whitespace normalization**). The cleanup runs on arbitrary Markdown/text and is not a structured diff engine. It supports multiline matches, and `<U>...</U>` casing, but the regex may not safely handle nested markup or every possible underline-tag variation. `ContainsAmendmentFormatting` only detects the marker patterns; it does not transform text.

**Important distinction:** Markup expresses **proposed edits before approval**. The updated parent receives **clean final text without deletion/underline markers**. Do not implement the opposite behavior or assert that the parent retains underlines/strikethroughs.

### Required characterization work (Phase 0; do not skip)

- Read the two source files and trace `UpdateCurrentAmendmentBodies`, `IAmendmentBodyService.UpdateAsync`, parent lookup, language matching, screen force-send, and the final amendment save. Check service behavior and existing tests instead of guessing about additional side effects.
- Write golden tests for: false→true approval only; optional child cleanup **both true and false**; parent cleanup **regardless of that option**; exact deleted/added text output; whitespace; multiline matches; case-insensitive `<u>`; matching and missing languages; unchanged parent (no write); no-parent/missing-parent behavior; and parent-live force-send.
- Characterize `IsApproved` reversal and subsequent re-approval, multiple approved siblings, parent archived state, partial failures, and any side effects inside underlying services. Treat anything not established by the source as `VERIFY`.
- Preserve `**NEWSLIDE**` delimiters unless they occur *inside* accepted deletion markup; record actual before/after fixtures. Inspect how rendered HTML is generated and sanitize untrusted HTML appropriately.

### Target .NET 10 implementation (Phase 1 and Phase 4)

- Expose a clear **conference approval domain operation** (`ApproveAmendment` or verified equivalent) distinct from translator editing. Preserve the ability to choose whether to clean **the approved child's own bodies**, while **always propagating clean text to matching parent-language bodies** on the approval transition.
- Move the deterministic cleanup transformation into a **.NET 10 domain service** with golden tests. Preserve the *resulting plain/Markdown text*, including legacy spacing unless explicitly changed; do not preserve accepted change markup in the parent.
- A focused **.NET 10 Lambda-backed AppSync mutation** is appropriate for version-checked approval, multi-language reads, transformation, conditionally updated parent bodies, and coordinated persistence. Model and test a true false→true state transition.
- Use optimistic `Version` checks and atomic `TransactWriteItems` for the approval plus affected body writes where limits allow. If transaction limits are exceeded, implement a durable, idempotent recovery workflow. This intentionally fixes the legacy partial-update/swallowed-exception risk, subject to maintainer approval.
- Avoid stale sibling overwrites: if another approved amendment has changed the parent between fetch and save, return a conflict or retry using fresh versions instead of overwriting blindly. Support safe retries of the same approval request.
- Send notifications **after durable success** via the existing private update/public-live feed split. When the changed parent body is currently projected, refresh that projector automatically. Authorize and audit approver, timestamp, parent ID, and body-language changes.

### Dedicated acceptance checkpoint — Conference Approval Parity

A golden fixture must demonstrate:

```text
Approved child's proposed body:  "We ~~shall~~ <u>will</u> proceed."
Parent body AFTER approval:     "We  will proceed."
```

Test all language matches and the optional child-body cleanup flag. Confirm parent content is **replaced by the child's cleaned full body** (not patched), repeated saves do not rerun approval, no-parent behavior is safe, and failures/conflicts cannot silently produce partial approval. Verify that currently projected parent content refreshes without another operator action. Compare rendered before/after output with the legacy app and obtain human sign-off. **Do not retire the old handler/cleanup service until this checkpoint passes.**

## 2A. .NET 10 Upgrade Standard (Applies Throughout)

**Framework policy:** All **actively built and maintained C# code** in the final solution targets `net10.0`: Blazor WebAssembly client, domain/contracts/shared models, infrastructure/data libraries, Lambda handlers, import/export and migration tools, CDK application, and automated test projects. Do not create new .NET 8/9 projects. The deprecated legacy host may temporarily remain on its original framework if changing it would jeopardize a safe migration; document this as a time-boxed exception, retain a separately verified build, and remove it at cutover. If practical, upgrade the legacy host too before retirement rather than running a split-framework production solution.

**Toolchain policy:** Pin an approved .NET 10 SDK using `global.json` with an intentional roll-forward policy, and pin compatible NuGet packages using central package management (`Directory.Packages.props`) where useful. Use the supported .NET 10 Blazor WebAssembly packages/runtime and update workload/hosting/build tools. Check breaking changes, serialization, globalization, trimming and linker behavior, authentication libraries, Markdig/PDF/Excel libraries, GraphQL client generation, test adapters, and any source generators before upgrading each project. Avoid indiscriminate major-version dependency updates unrelated to .NET 10 compatibility.

**Lambda runtime:** AWS supports the managed **`.NET 10` Lambda runtime (`dotnet10`)**. Target `net10.0`, use an appropriate `Amazon.Lambda.*` set and publish a ZIP deployment by default (avoid container images). In C# CDK configure the runtime using the supported `Runtime.DOTNET_10` binding (verify the installed package exposes it), not `DOTNET_8` or a custom runtime. If a pinned CDK package lacks the enum, upgrade that package to a compatible version before implementation. Consider trimming/Native AOT only as **optional, measured later optimizations**, not required for the initial migration.

**Build and CI policy:** All new and changed solution projects build and test with the pinned SDK in local development and GitHub Actions. Validate Blazor WASM publishing and browser runtime, Lambda packaging/handler loading in AWS, CDK synth/deploy, and regression/parity tests. Record warnings and breaking changes with explicit fixes; do not silence compiler warnings en masse or set `TreatWarningsAsErrors=false` simply to pass the upgrade.

**Release policy:** Upgrade incrementally behind checkpoints. A green `dotnet build` alone does **not** prove runtime compatibility: run user-journey smoke tests, browser tests, Lambda integration tests, and CDK assertions. Keep legacy rollback options intact until cutover.

---

## 3. Target Architecture

```text
Browser: Blazor WebAssembly (editor / operator / public projector)
            |
            +---- CloudFront + private S3 origin (static assets)
            |
            +---- AppSync GraphQL API ---------------- Cognito User Pool
            |         |                     (authenticated user groups)
            |         +---- DynamoDB direct resolvers
            |         +---- C# Lambda use-case handlers ---- S3 temporary exports
            |
            +---- AppSync Events
                      ^
                      | (backend-authorized publications)
             DynamoDB Streams -> Publisher Lambda
                      ^
                      |
                   DynamoDB

CloudWatch: structured logs, errors, alarms and metrics
Route 53 (existing `adamlj.com` hosted zone) + ACM: stage-specific HTTPS domains
GitHub Actions: build, test, CDK synth/diff/deploy, static upload/invalidation
```

### Boundary ownership

- **DynamoDB:** durable content, presentation state, timer state, settings and version numbers.
- **GraphQL:** strongly typed application reads/mutations. Auth at schema plus resolver level, especially role and resource-level checks.
- **Lambda:** validated business operations requiring multi-item orchestration, complex domain logic, exports/imports and stream/event publication.
- **AppSync Events:** near-real-time notifications, **not a message database** or replay system.
- **Blazor:** rendering, local countdown calculations, realtime reconnect logic and responsive editor UX.
- **Cognito:** login, token issuance, user/group administration. Never ship administrator credentials to a public client.

### Data consistency rule

**Commit first; notify second.** A successful write must become authoritative in DynamoDB before clients are told it changed. Use a DynamoDB Stream-driven publisher (or an explicitly designed transactional outbox) to reliably publish notifications after committed state changes. Avoid treating a best-effort `UpdateItem` followed by synchronous publish as an atomic operation.

Treat DynamoDB Streams and event publication as at-least-once in practice: duplicates, retries and reordering must be safe. Track the correct **item-specific** versions in events, and implement monitoring plus DLQ/on-failure handling for publisher failures. Clients can always recover by re-fetching state.

---

## 4. DynamoDB Data Model and Access Patterns

Start with **one application table** and a minimal set of GSIs, then revise based on the verified access-pattern list. Example table name: `AmendmentData-{stage}`. Partition key `PK` (string), sort key `SK` (string). On-demand capacity. Point-in-time recovery enabled for production, deletion protection/`RETAIN` production data, server-side encryption, alarms.

### Proposed item shapes (conceptual)

| PK | SK | Purpose |
|---|---|---|
| `AMENDMENT#<id>` | `META` | Amendment metadata, flags/status, parent, version, audit data |
| `AMENDMENT#<id>` | `BODY#en` | English markdown/status/version |
| `AMENDMENT#<id>` | `BODY#es` | Spanish markdown/status/version |
| `AMENDMENT#<id>` | `BODY#fr` | French markdown/status/version |
| `SYSTEM` | `LANGUAGE#<code>` | Supported language configuration |
| `SYSTEM` | `SETTING#<key>` | Nonsecret application settings |
| `PRESENTATION#MAIN` | `STATE` | Active amendment; language visibility/page and version |
| `TIMER#MAIN` | `STATE` | Timer clock state and version |

Store each body separately, keyed by language. **Do not embed all languages in one metadata item**, since translators edit independently. Persist markdown as the source of truth; derive HTML/client slides using a tested sanitizer/rendering pipeline. Keep `**NEWSLIDE**` compatible.

Use `BODY#<normalized language code>` (lowercase normalized BCP-47 identifier) so duplicate languages cannot exist as separate keys. Enforce body creation with `attribute_not_exists(PK)` (conditional put; PK+SK are the complete item key). Enforce version-aware updates with conditional expressions and report conflicts clearly.

### Suggested presentation item

```json
{
  "PK": "PRESENTATION#MAIN",
  "SK": "STATE",
  "activeAmendmentId": "01ABC...",
  "languages": {
    "en": { "visible": true, "page": 0 },
    "es": { "visible": true, "page": 1 },
    "fr": { "visible": false, "page": 0 }
  },
  "version": 42,
  "updatedAt": "2026-10-17T00:00:00Z",
  "updatedBy": "cognito-sub"
}
```

Presentation state is distinct from amendment editorial state. **Live content is not an approved snapshot.** If its body changes, its projector must update automatically. Do not duplicate `IsLive` on each body unless existing behavior requires a separately defined concept.

### Suggested timer item

```json
{
  "PK": "TIMER#MAIN",
  "SK": "STATE",
  "remainingAtLastChangeSeconds": 300,
  "startedAt": "2026-10-17T00:00:00Z",
  "running": true,
  "visible": true,
  "version": 19
}
```

When running, calculate `remaining = max(0, remainingAtLastChangeSeconds - elapsedSecondsSinceStartedAt)`. When paused, persist computed remaining seconds and set `running=false`. Define clock skew tolerance, precision, zero-time semantics, and desired behavior when all clients are disconnected. Use server-issued timestamps/reconciliation to limit device-clock drift. Do not launch Lambda each second.

### Index candidates: add only for verified queries

- **GSI1:** list amendments by archive/status and chosen sort order; e.g., `GSI1PK=AMENDMENTS#ACTIVE|ARCHIVED`, `GSI1SK=<sortTimestamp>#<id>` on **metadata items only**. Paginate with continuation tokens.
- **GSI2:** parent/child listing *if needed*: `GSI2PK=PARENT#<parentId>`, `GSI2SK=<createdAt>#<id>`.
- **Legislative identifier:** optional GSI for lookup. If uniqueness is a business rule, **GSI alone does not enforce uniqueness**; use a conditionally created uniqueness/claim item within a DynamoDB transaction.

**Define and test first:** Get by id, get bodies by amendment, list active/archived, search requirements, find children, read current presentation/timer, update body, create/clone, archive, move live presentation, imports/exports. No full-table Scan for standard user flows; use scans only for bounded administrative migrations/tools.

### Concurrency and transaction policy

- Use **`version` consistently** on Amendment, AmendmentBody, PresentationState, and TimerState. Every successful state mutation increments its own item version atomically. Use conditional writes with `expectedVersion` where concurrency-sensitive; for all state types, versions also let clients discard stale notifications and detect gaps.
- DynamoDB stores version as a 64-bit integer-compatible Number; use C# `long`. GraphQL `Int` is limited to signed 32-bit: expose `version` and `expectedVersion` as AppSync `AWSBigInt` (or a deliberately designed string scalar), and ensure Blazor JSON handling preserves full 64-bit values. Never use `AWSTimestamp` for versions.
- Versions are **per item**, not a globally ordered event sequence. Comparing different item versions is meaningless; a body update does not increment the presentation state version. Public notifications about body edits must identify the *body* version separately from presentation changes.
- Conditional writes prevent lost updates and duplicate bodies.
- TransactWriteItems for cross-item invariants (e.g., amendment existence, unique claims, presentation transitions) when needed. Observe transaction item/size limits.
- Decide deletion policy: when a currently presented amendment is archived/deleted, atomically select a safe display state or block the operation. Tests must cover race conditions.
- Define max markdown length, supported language list, page validation, archive semantics, and edit permissions in the domain contract.

---

## 5. GraphQL Contract and Validation Strategy

Use Cognito as the **default** GraphQL authorization mode. Add a separate public mode only to the minimal public query fields/types. A public API key embedded in WASM is **not secret** and grants internet-wide access to the explicitly allowed operations; consider anonymous IAM with Cognito Identity Pools if more suitable. Apply query complexity/throttling/WAF or other safeguards as appropriate. Avoid exposing general `amendment(id)` through public auth.

### Contract sketch (not final generated schema)

```graphql
# Read-side operations
query GetAmendment($id: ID!) { amendment(id: $id) { id title bodies { language markdown version } } }
query PublicPresentation { publicPresentation { version activeAmendmentId languages { language markdown page visible } } }

# Write-side domain operations
# createAmendment / updateAmendment / archiveAmendment / cloneAmendment
# createAmendmentBody / updateAmendmentBody / deleteAmendmentBody
# presentAmendment / clearPresentation / setLanguageVisible / setLanguagePage
# timerStart / timerPause / timerReset / timerSet / timerShow
# requestPdfExport / requestExcelExport
```

Use this as **operation inventory**, not copy-paste GraphQL SDL. In the implementation, define actual schema types, inputs, nullability, enums, resolver mappings and role constraints. Add cursors/pagination and carefully sized public payloads.

### Validation and invariants

| Invariant | Enforcement |
|---|---|
| Body markdown cannot be blank/whitespace | Blazor feedback **and server-side resolver/domain validation** |
| One language body per amendment | DynamoDB `PK=AMENDMENT#id`, `SK=BODY#language` and conditional create |
| Amendment exists before adding a body | Transactional/conditional validation; no time-of-check/time-of-use gap |
| Only configured languages | Server-side validation of normalized language code |
| No lost concurrent translation changes | Version compare-and-swap conditional update |
| Archived/deleted amendment edit policy | Server-side domain/authorization rule |
| Only operators can change screen/timer | Server-side Cognito role checks |
| Current page valid for markdown slide count | Domain validation and defined clamping/reset behavior |
| Only active live content is public | Public presentation resolver and event publisher **check authoritative presentation state** |

**Important:** Even if AppSync GraphQL resolves directly to DynamoDB, rules still belong in resolver logic plus DynamoDB conditions. Put reusable complex C# rules in a domain project; use focused Lambda handlers when multi-read or multi-write behavior exceeds a simple resolver. Do **not** require Lambda for every `PutItem`.

### Error contract

Return consistent client-understandable errors: `VALIDATION_ERROR`, `NOT_FOUND`, `FORBIDDEN`, `CONFLICT`, `PRECONDITION_FAILED`, `RATE_LIMITED`, `INTERNAL_ERROR`. Do not leak server stack traces, restricted body text, IDs from unauthorized contexts or token contents. Resolve validation/conflict failures into useful UI notifications.

---

## 6. Real-Time Design and Anonymous Projector Security

### Separate event audiences

1. **Authenticated editorial events:** `/amendments/<id>/<language>` for editor/admin clients. Body changed → subscribed authenticated clients refresh corresponding body.
2. **Public live-presentation events:** `/presentation/main` for all projectors. Changes to *currently public* state or bodies trigger a small event with version and identifiers appropriate for public consumption.
3. **Public timer events:** `/timer/main` (updates only; no anonymous publishing).

We originally discussed letting projectors subscribe directly to arbitrary body channels. **Do not make those channels anonymously subscribable**: knowing an amendment ID must not let anonymous clients discover drafts or archived/private translations. Instead, projector clients subscribe to a stable public feed. This preserves immediate live updates without exposing the private content graph.

### How a live translator edit reaches a projector

```text
Translator saves Spanish markdown
    -> GraphQL updateAmendmentBody (auth + validation + conditional write)
    -> DynamoDB body version 12 -> 13 (committed)
    -> DynamoDB Stream -> Publisher Lambda
    -> check authoritative active amendment + visible language
    -> if live: publish /presentation/main {type:"liveBody.updated", amendmentId, language, bodyVersion: 13}
    -> Spanish projector re-fetches publicPresentation (authorized, filtered)
    -> renders revised slide immediately; NO operator approval

Authenticated editors also receive their private /amendments/... notification.
```

Event publisher must **re-evaluate public eligibility at publish time**, not trust the edited record alone. On event lag or races (e.g., switching slides while a stream record is processed), consumers must re-fetch the current public presentation and never render content from the event payload. The event carries **no private markdown**. It is okay to over-notify and re-fetch; it is **not** okay to publish draft content.

### Public projection bootstrap/reconnect

1. Open public event subscription and buffer notifications.
2. Query `publicPresentation` and current public timer state (snapshot).
3. Apply buffered newer-version notifications and re-query if needed (avoids a subscription/snapshot race).
4. On reconnect, resubscribe and re-query authoritative snapshots; handle duplicates, missing versions and out-of-order events.
5. On body change, re-render visible/current language and clamp page to safe range if slide count decreased; persist any operator state normalization in the proper write flow if required.
6. If fetch fails, show a clear connection/reconnecting state and never substitute private cached content that should no longer be public.

**Definition of public:** Anyone with the public web app URL can read the currently projected content and timer. If even that is too public for a deployment, replace anonymous mode with device/operator login or scoped temporary projector credentials.

### Security tests (must fail closed)

- Anonymous user **can** read only the current public presentation/timer and receive current live updates.
- Anonymous user **cannot** list all amendments, fetch arbitrary body by ID, enumerate archives, request exports, change presentation/timer, publish events, or access admin/user settings.
- Direct HTTP requests and direct WebSocket/Event API calls are covered, not only Blazor navigation.
- Cognito token claims are checked for role on every protected mutation; translators cannot operate screens unless explicitly authorized.
- Private/draft body edits never leak through public event payloads or public resolver paths.

---

## 7. Blazor Client Plan

- Retain existing components/routes where practical; introduce typed `IAmendmentApi`, `IPresentationApi`, `ITimerApi` and `IRealtimeClient` adapters behind current UI.
- Prefer a generated typed GraphQL client or a small tested client abstraction. Store no secrets in browser configuration. Implement token refresh/relogin, unauthorized redirects, and user-friendly conflict resolution.
- Render markdown through the existing compatible rendering pipeline with explicit sanitization strategy; regression-test HTML and `**NEWSLIDE**` splitting against legacy examples.
- Treat projector as **read-only**, role-free, reconnecting view. It should not be able to invoke authenticated mutations.
- On timer messages, update timestamp-based local model; tick UI locally at ~1 second; reconcile on reconnect. Client clock discrepancy must not accumulate unnoticed.
- Enforce bounded retries/backoff and handle disconnected states gracefully. Avoid polling per second and avoid refetching entire amendment lists for a single live-body change.
- Cache snapshots carefully; clear stale sensitive data on logout and prevent accidental rendering of old content after a public presentation change.

---

## 8. CDK v2 Infrastructure-as-Code Guidance (C#)

### Toolchain and conventions

- Use **AWS CDK v2** in the top-level **`infra/Amendment.Cdk/`** directory as a dedicated **`net10.0`** C# project (`Amazon.CDK.Lib`, `Constructs`). Use the pinned .NET 10 SDK, current compatible CDK bindings/CLI, and configure C# Lambda functions with the managed **`dotnet10`** runtime.
- Require AWS CLI profiles/credential chain; **no access keys in git or CDK context**. Bootstrap each target account/region with `cdk bootstrap` once.
- Pin package/tool versions (NuGet lockfiles where appropriate and CLI version) and store environment parameters as typed CDK context/config (not secrets).
- CDK names/logical IDs should be deterministic. Use unique stage suffixes (e.g., `dev`, `prod`); avoid embedding mutable developer-specific paths.
- Build functions as assets with reproducible bundling/publish steps. Do not depend on developer-machine-only executables for release builds.
- Keep least-privilege IAM grants narrowly scoped per resolver/function/table/key operation, S3 prefix and AppSync publish authority.
- **No default VPC**, NAT gateway, ALB or Redis. Review every generated CloudFormation resource for unintended recurring-cost services.
- Reuse the **existing Route 53 public hosted zone for `adamlj.com`**; do not create another hosted zone. Stage hostname and hosted-zone selection must be configuration-driven, not hardcoded in resource constructs.

### Existing hosted zone, environment URLs and future portability

The AWS account already contains a **public Route 53 hosted zone named `adamlj.com`**. Look it up/import it in CDK rather than provisioning a new zone or changing its name servers. The expected initial environment URLs are:

| Stage | Public frontend hostname | Hosted zone to reuse |
|---|---|---|
| `dev` | `dev-amendments.adamlj.com` | `adamlj.com` |
| `prod` | `amendments.adamlj.com` | `adamlj.com` |

**Keep this configurable:** use a typed stage configuration object (e.g. `StageDomainConfig`) with distinct `hostedZoneName`, `siteDomainName`, and optionally `hostedZoneId` values. Place nonsecret defaults under `infra/Amendment.Cdk/Config/` and allow stage-specific overrides through CDK context/environment configuration. **Do not derive the domain from the stage using hardcoded `adamlj.com` strings inside WebStack.** A future domain move should primarily require changing configuration plus DNS/certificate deployment, not rewriting constructs. Document the necessary DNS cutover and any Cognito callback/logout URL updates.

For example, an illustrative nonsecret configuration (not a required exact filename or schema):

```json
{
  "dev": {
    "hostedZoneName": "adamlj.com",
    "siteDomainName": "dev-amendments.adamlj.com"
  },
  "prod": {
    "hostedZoneName": "adamlj.com",
    "siteDomainName": "amendments.adamlj.com"
  }
}
```

- CDK must **reference the existing hosted zone** (`HostedZone.FromLookup` or `FromHostedZoneAttributes` with a configured zone ID); it must not use `new HostedZone` for this domain. If multiple zones have the same name, resolve with an explicit configured hosted zone ID and verify account ownership.
- Attach each hostname to its stage-specific CloudFront distribution and create the corresponding Route 53 **A/AAAA alias records** in the existing zone; do not create unnecessary records at the zone apex. Verify that DEV and PROD distributions and records remain isolated.
- CloudFront custom-domain HTTPS requires an **ACM certificate in `us-east-1`**, even if the main application stacks deploy in another region. CDK must account for certificate creation/validation in that region (or import a suitable existing certificate), DNS validation against the reused hosted zone, and deployment ordering/cross-region references. Do not assume an ACM certificate in the application region works for CloudFront.
- Keep AppSync API/Events endpoints separately configurable for the Blazor client; the above hostnames describe the **website**, not automatically a custom domain for AppSync.
- Treat the hosted zone as a **shared existing resource**: deleting a dev stack must not delete it, its unrelated records, or the PROD records. CDK tests should assert this explicitly. Before first deploy, verify `adamlj.com` is the correct **public** hosted zone in the target AWS account and that its nameservers are delegated appropriately.

### Suggested repository layout

Keep application code under `src/`, but place the CDK C# application under the **top-level `infra/Amendment.Cdk/`** directory. Do not create `src/Amendment.Cdk/`.

```text
src/
  Amendment.Client/                 # .NET 10 Blazor WASM, adapted incrementally
  Amendment.Domain/                 # domain models, validation, slide rules
  Amendment.Contracts/              # GraphQL/event contracts, DTOs
  Amendment.Infrastructure/         # DynamoDB and AWS clients
  Amendment.Functions/              # focused Lambda handlers
  # Keep legacy Server/Repository/etc. during strangler migration
infra/
  Amendment.Cdk/                    # .NET 10 CDK v2 C# app and stacks
    Stacks/
      DataStack.cs
      IdentityStack.cs
      ApiStack.cs
      WebStack.cs
      ObservabilityStack.cs
    Constructs/
    Config/                           # stage-specific domain/zone settings (nonsecret)
schema/
  amendment.graphql
  events/                            # public/private event contract files
  resolvers/                         # AppSync JS resolvers if used
scripts/
  export-legacy-data/
  import-dynamodb/
  verify-migration/
tests/
  Amendment.Domain.Tests/
  Amendment.Api.Tests/
  Amendment.Security.Tests/
  Amendment.Integration.Tests/
docs/modernization/
.github/workflows/
```

### Stack responsibilities

- **DataStack:** DynamoDB tables/GSIs, PITR/retention, streams, encryption, optional backup policies.
- **IdentityStack:** Cognito pool/client, groups and configuration; no implicit destructive user-pool replacement.
- **ApiStack:** AppSync GraphQL schema, authorization modes, data sources, resolvers, Event API channels/namespace auth, stream publisher Lambda, scoped grants and failure destinations.
- **WebStack:** private S3 site bucket, CloudFront with origin access control, HTTPS, cache behavior, existing Route 53 zone lookup, configurable dev/prod hostnames and us-east-1 ACM certificate, SPA routing (handle 403/404 appropriately), response/security headers.
- **ObservabilityStack:** CloudWatch log retention, dashboards/metrics/alarms, billing guardrails and publisher failures.

Keep separate stacks where ownership or replacement lifecycle justifies it; start with a small number if cross-stack dependencies create more complexity than value. Avoid cyclical stack references. Use environment-specific resource removal policies: destroyable temporary dev data only by explicit choice, `RETAIN` and recovery settings for prod.

### CDK implementation sequence and commands

```bash
# From the repository root, once:
mkdir -p infra/Amendment.Cdk
cd infra/Amendment.Cdk
cdk init app --language csharp

# For ongoing CDK work, from the repository root:
cd infra/Amendment.Cdk
# From this directory (where cdk.json resides):
dotnet --version  # must resolve to the pinned .NET 10 SDK
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
cdk synth --context stage=dev
cdk diff --context stage=dev
cdk deploy --all --context stage=dev

# For production: require human approval of the diff
cdk diff --context stage=prod
cdk deploy --all --context stage=prod
```

Keep `cdk.json` under `infra/Amendment.Cdk/` and run the CDK commands from that directory, or use a documented equivalent `--app` invocation. Reference Lambda, schema and resolver assets using paths resolved relative to the CDK app location, not assumptions that CDK runs from the repository root. Include `infra/Amendment.Cdk/` in solution/build configuration and CI path filters; document exact verified invocations in the repo README. Add automated assertions (CDK assertions library and/or snapshot tests) for:

- No `AWS::EC2::NatGateway` / unexpected ECS/RDS/ALB.
- Public GraphQL fields explicitly scoped and private-by-default.
- Nonpublic S3 bucket and CloudFront OAC; HTTPS enforced.
- DEV hostname `dev-amendments.adamlj.com`, PROD hostname `amendments.adamlj.com`; both alias the correct stage distribution through the reused `adamlj.com` zone, with no new hosted zone created.
- CloudFront TLS certificate belongs to `us-east-1`; stage/domain selection is configuration-driven and zone/DNS changes do not delete shared zone resources.
- Production DynamoDB removal policy/point-in-time recovery.
- Least-privilege IAM grants, no wildcard publishing grants by default.
- Log retention and failure alarms.
- C# Lambda functions use **`dotnet10`** managed runtime; CDK app targets **`net10.0`**; no unexpected Lambda container packaging.

### CI/CD recommendations

Use GitHub Actions with **GitHub OIDC federation** to AWS IAM roles, not long-lived AWS secrets. Pipeline:

1. PR: restore/build/unit tests, client build, schema validation, `cdk synth`, CDK assertions, static/security scans.
2. Dev promotion: integration/security tests and dev deploy, seeded fixture smoke tests.
3. Production: protected environment + approval; immutable build artifacts; reviewed `cdk diff`; deploy infra; upload Blazor assets; CloudFront cache invalidation or content-hashed asset rollout; end-to-end smoke tests.
4. Rollback: redeploy previous client build and API/Lambda versions; **do not assume a destructive DynamoDB schema rollback is safe**. Every data migration must have a separate rollback/recovery strategy.

CDK can model infrastructure, but deploy scripts should manage static web artifact publication in a repeatable manner (or a verified CDK deployment construct). Avoid committing generated app assets or AWS secrets.

---

## 9. Cost and Operability Controls

- DynamoDB on-demand; intentionally review table GSIs and stream read/write costs.
- Static hosting S3 + CloudFront, compressed/cacheable immutable files; avoid always-on compute.
- AppSync GraphQL/Events priced by use; **measure both calls and subscription connection minutes**, public traffic/bots and data transfer.
- Keep Lambda outside VPC; short timeouts, reasonable memory, controlled concurrency; use minimal logging at normal operation.
- Limit CloudWatch retention (e.g., short retention in dev and a justified longer prod policy); no verbose full-body event logging.
- Route 53 hosted zones/domains, AWS WAF, Cognito usage and backups may add fixed or usage-dependent charges. Do not promise a specific monthly bill without usage assumptions.
- AWS Budgets: low monthly budget alerts; basic CloudWatch alarms for error rates, throttling, dead-letter/failure messages, concurrency and export failures.
- Before production, prepare a simple AWS Pricing Calculator estimate for **idle**, **typical conference**, and **peak presentation** scenarios.

---

## 10. Phased Delivery Plan (One Reviewable PR Per Phase)

Each checkbox represents a deliverable/acceptance gate. **Do not proceed to the next phase until the checkpoint is explicitly accepted.**

### Phase 0 — Baseline, feature audit and test harness

**Objective:** Understand and protect current behavior before rewriting.

- [x] Check out the latest main branch and produce a current-state architecture/feature and data-flow map.
- [x] Identify all clients, SignalR hubs/events, controllers, roles, validation rules, exports and data access paths.
- [x] Capture representative test fixtures: multiple amendments, multilingual content, archived state, markdown with varying slide counts, live presentation, timer states. *(Phase 0: multi-language approval and `**NEWSLIDE**` cleanup fixtures. Completed in Phase 1: `test/Amendment.Domain.Tests/Fixtures.cs`.)*
- [x] Establish reproducible local build/tests; document missing prerequisites and baseline failures.
- [x] Add characterization tests around legacy behavior that must not regress.
- [x] **Critical:** Trace the source-verified `isBeingApproved` false→true transition, optional approved-child cleanup, unconditional matching-parent-body replacement using accepted changes, live parent notifications, and failure behavior; capture golden before/after fixtures and edge cases.
- [x] Record assumptions and unresolved questions (including import/export exact expectations).

**Checkpoint 0:** Current code can be built/tested reproducibly; baseline parity matrix and migration risk register reviewed. **No production changes.**

### Phase 1 — Target contracts and domain extraction

**Objective:** Lock critical semantics and validation without AWS deployment.

- [x] Define domain entities/DTOs, versioning, valid states and language normalization (design against **.NET 10** APIs; production project retargeting occurs in Phase 1A).
- [x] Preserve `**NEWSLIDE**` and test slide parsing, empty-body rejection, pages after edits, edge cases.
- [x] Define create vs update semantics and duplicate-body handling.
- [x] Define GraphQL schema draft, public presentation DTO, private/public event schema, version behavior.
- [x] Define role-to-operation matrix, including translator live edits and anonymous read-only projector.
- [x] Create `Amendment.Domain.Tests` with unit/property-style edge tests.
- [x] Extract/specify conference approval as a business-critical domain workflow with deterministic cleanup/parent-update golden tests; distinguish it from live translator updates.

**Checkpoint 1:** Unit tests demonstrate all agreed invariants and desired live-edit behavior; schema/auth decisions reviewed. **No cloud dependency required.**

### Phase 1A — Upgrade maintained C# projects to .NET 10 (required gate before Phase 2)

**Objective:** Establish .NET 10 as the single target for the modernized application before provisioning new AWS infrastructure.

- [x] Install and pin an approved .NET 10 SDK in `global.json`; update local setup instructions and GitHub Actions `setup-dotnet` usage.
- [x] Inventory every C# project and shared build file; record current framework, incompatible dependencies, and any deliberately temporary legacy-host exception.
- [x] Upgrade the existing **Blazor WebAssembly client** and the C# projects that are being retained to `net10.0`; align `Microsoft.AspNetCore.Components.WebAssembly*` and other framework-dependent packages to compatible .NET 10 versions.
- [x] Create or retarget **domain, contracts, infrastructure and test** projects to `net10.0`; preserve feature behavior and maintain backwards-compatible interop with the legacy backend during migration.
- [ ] Update obsolete APIs, serializers, dependency injection registration, analyzers, code generators and test SDKs only where required. Validate Markdown rendering, localized strings, authentication and all existing major UI routes.
- [ ] Establish a reproducible **Release** build/test pipeline and a Blazor WASM publish smoke test; verify browser console/network behavior and capture size/performance baseline.
- [x] Document any legacy projects excluded from the upgrade, why, who owns removal, and the sunset checkpoint (Phase 11).

**Checkpoint 1A:** All retained C# projects build/test/publish as `net10.0` with the pinned SDK; Blazor smoke and parity tests pass; any temporary legacy exception is documented. **Stop for approval before Phase 2.**

### Phase 2 — CDK foundation and disposable dev environment

**Objective:** Reproducible, low-cost AWS baseline.

- [ ] Create **`infra/Amendment.Cdk/` targeting `net10.0`** (not under `src/`) using AWS CDK v2 C# and typed stage configuration; use compatible `Amazon.CDK.Lib`/`Constructs` package versions.
- [ ] Provision dev DynamoDB table, Cognito pool and groups, basic AppSync API, S3+CloudFront, minimal logs/budget alerts.
- [ ] Reuse the existing Route 53 `adamlj.com` public hosted zone and configure `dev-amendments.adamlj.com` for DEV and `amendments.adamlj.com` for PROD using typed, overrideable stage-domain configuration; do **not** create a new hosted zone.
- [ ] Configure CloudFront certificates in `us-east-1`, Route 53 A/AAAA alias records, and stage-isolated domains; validate DNS delegation and protect unrelated zone records.
- [ ] Add CDK assertions for security defaults, no NAT/VPC, resource removal/retention, and every managed C# Lambda using **`dotnet10`** (when functions are introduced).
- [ ] Add `cdk synth`, `cdk diff`, deploy instructions and a GitHub OIDC plan (initially least-privilege dev).
- [ ] Use stage-specific names, outputs and nonsecret frontend config generation.

**Checkpoint 2:** A fresh dev stack can be deployed/destroyed intentionally from a clean checkout using the pinned **.NET 10** SDK; synth assertions pass; no unexpected standing services/costs. `https://dev-amendments.adamlj.com` serves DEV over valid TLS, and deleting DEV cannot delete the shared `adamlj.com` zone or impact `amendments.adamlj.com`. PROD domain is defined in configuration but not deployed without production approval.

### Phase 3 — DynamoDB persistence and data migration prototype

**Objective:** Confirm access patterns and safe persistence semantics.

- [ ] Implement amendment/body metadata keys, conditional create/update, optimistic concurrency and query pagination.
- [ ] Add only proven GSIs; integration tests against real dev DynamoDB (or compatible local emulator **plus** AWS tests for behavior).
- [ ] Build a **read-only** PostgreSQL export/conversion tool. Preserve original IDs or create an explicit ID mapping.
- [ ] Prototype import with a small scrubbed fixture; verify counts, content checksums, language uniqueness, relationships and markdown rendering.
- [ ] Document backup/rollback and dual-system consistency strategy during transition.

**Checkpoint 3:** Create/read/update/duplicate/conflict tests pass; fixture round-trip matches source; migration is repeatable and doesn't modify production source data.

### Phase 4 — Private authenticated GraphQL amendment API

**Objective:** Replace core server API behavior without touching projector operation.

- [ ] Implement authenticated amendment lists/details, create/update/archive and translation-body operations.
- [ ] Use direct DynamoDB resolvers for straightforward operations; use targeted Lambda for complex transactional use cases.
- [ ] Enforce server-side validation and per-operation group/resource authorization.
- [ ] Add consistent GraphQL errors and integration tests for roles, concurrent updates, invalid data.
- [ ] Introduce typed Blazor API client behind feature flags/adapters; keep old backend working for other routes.
- [ ] Implement conference approval transition using focused .NET 10 C# domain cleanup logic and a transactional/conditionally guarded Lambda-backed operation where required; preserve parent updates and output markup exactly, with safe retries and private/live notifications.

**Checkpoint 4:** Existing edit workflows function against new API in dev. Unauthorized direct requests fail; duplicate body and blank body writes are rejected. **Conference approval parity checkpoint passes: approving a child correctly updates its parent with verified legacy cleanup semantics (remove deleted text; keep added text without underline tags), verified using rendered before/after fixtures, concurrency/retry tests, and authorization tests.** Regression suite passes.

### Phase 5 — Presentation state and anonymous public API

**Objective:** Make public reads safe before enabling anonymous real-time access.

- [ ] Build single authoritative presentation state and operator mutations (select/clear amendment, visibility/page controls).
- [ ] Implement `publicPresentation` resolver that returns **only** currently projected content; no arbitrary anonymous get-by-ID.
- [ ] Define and implement behavior when a live body is deleted/archived or slide count changes.
- [ ] Verify public vs authenticated schema modes at nested field level.
- [ ] Test anonymous access via raw GraphQL requests, including enumeration/alias attempts and mutation denial.

**Checkpoint 5:** Anonymous clients read current projection only; cannot read drafts, browse archives, mutate state or enumerate private entities. Presentation state survives reloads.

### Phase 6 — AppSync Events and instant translator-to-projector updates

**Objective:** Remove dependence on the SignalR server for content and screen synchronization.

- [ ] Provision private editorial channels, public presentation channel, and server-side publish authorization.
- [ ] Implement DynamoDB Stream publisher for **committed** body and presentation changes; add retry/error alarms and failure destination.
- [ ] Restrict public events to identifiers/version notifications; include **no markdown body payloads**.
- [ ] On body updates, publish a public notification when its amendment/language is currently visible; authenticated editorial notifications remain independent.
- [ ] Implement projector snapshot/subscription ordering, reconnect/resync, duplicate/out-of-order handling.
- [ ] Test simultaneous operators/translators and multiple language-specific projectors under Wi-Fi disconnect/reconnect.

**Checkpoint 6 — critical demo:** Spanish translator saves a **currently projected Spanish body** → Spanish projector refreshes promptly **without operator action**. Editing an unprojected body emits no private content publicly. Switching amendment/page works across screens; recovery after lost events works.

### Phase 7 — Durable timestamp-based timer

**Objective:** Eliminate server timer loop and per-second network calls.

- [ ] Implement timer state operations with conditional/version writes and authorization.
- [ ] Emit timer change notifications and add local client countdown behavior.
- [ ] Validate start, pause, resume, reset, show/hide, zero, browser refresh and device clock differences.
- [ ] Confirm timer continuity during API/client network disconnection and recovery.

**Checkpoint 7:** Two+ projectors display acceptably synchronized timers; countdown runs with **zero per-second AWS writes/events**. All timer controls and authorization pass.

### Phase 8 — Authentication/authorization migration

**Objective:** Replace legacy user/password management safely.

- [ ] Complete Cognito hosted/managed login and token handling for Blazor.
- [ ] Map existing users/roles to Cognito groups (document account migration/reset path and invitation method).
- [ ] Make all admin/editor/operator mutations protected on backend; audit direct AppSync and Event API permissions.
- [ ] Implement logout, session expiry and permission-changed behavior; prevent leaking sensitive cached content.
- [ ] Keep anonymous projection available as agreed; verify no role escalation.

**Checkpoint 8:** Role matrix proven with integration tests and representative user accounts; legacy identity can be retired without locking out operators/translators.

### Phase 9 — Exports, imports, settings and remaining parity

**Objective:** Close non-core gaps before cutover.

- [ ] Port PDF/Excel generation to Lambda and private temporary S3 with short-lived presigned downloads; expiration/lifecycle cleanup.
- [ ] Handle any existing imports, settings, UI notifications, localization, audit requirements and other parity-matrix features.
- [ ] Test correctness against legacy output fixtures and realistic file sizes; manage Lambda timeout/memory limits.
- [ ] Add sensible error handling, authorization and protection for downloadable artifacts.

**Checkpoint 9:** Feature parity matrix is complete or remaining exceptions are explicitly accepted; outputs are accurate and access-controlled.

### Phase 10 — Full migration rehearsal and production readiness

**Objective:** Prove performance, security, data correctness, recoverability and cost.

- [ ] Export real legacy data from a consistent snapshot and validate import mapping/counts/checksums.
- [ ] Exercise the full session: login, create/edit translations, present/clear, live edits, page/visibility, timer, export, reconnect.
- [ ] Run anonymous auth negative tests, load/soak simulations, failure injection for event publisher and CloudFront errors.
- [ ] Verify alarm alerts, backups/PITR, operational runbook, billing alerts and a documented rollback plan.
- [ ] Reconcile changes made to legacy data after export (migration freeze or carefully planned final delta import).

**Checkpoint 10:** Signed-off rehearsal and rollback drill; migration reconciliation fully green; go/no-go checklist approved.

### Phase 11 — Cutover, validation and decommission

**Objective:** Switch traffic safely and remove legacy operating cost.

- [ ] Back up PostgreSQL, capture final change freeze, perform final import and checksums.
- [ ] Deploy production CDK/API/website at `https://amendments.adamlj.com` through the existing `adamlj.com` hosted zone; verify certificate, DNS alias, anonymous/public access and role tests without changing the DEV hostname.
- [ ] Run live projector and translator smoke test before a real session.
- [ ] Keep legacy infrastructure available but **read-only** for a defined rollback window; avoid divergent writes.
- [ ] Monitor user reports, errors, throttles, event lag, streaming publisher failures and actual spend.
- [ ] After acceptance, delete old server/EF/Redis/SignalR deployment configuration and decommission host/db carefully; update developer docs.

**Checkpoint 11:** New serverless stack is the sole writer; user/session smoke tests succeed; rollback window complete; decommission approved.

### Phase 12 — Post-migration optimization (optional)

- [ ] Evaluate simplifying GSI usage and expensive/high-volume resolver calls from real metrics.
- [ ] Improve operational dashboards, mutation tracing and failure recovery.
- [ ] Decide whether to split admin and projector static bundles later (not required initially).
- [ ] Evaluate export enhancements, content history, audit/version history, and more sophisticated deployment strategies only if demonstrated value.

**Checkpoint 12:** Optimization documented with before/after measurements; no architecture changes solely for novelty.

---

## 11. Critical End-to-End Acceptance Scenarios

| ID | Scenario | Expected behavior |
|---|---|---|
| A01 | Translator saves whitespace-only body | Rejected server-side; no version increment/event |
| A02 | Two simultaneous Spanish-body creations | Exactly one succeeds; duplicate returns conflict |
| A03 | Two editors save version 5 | One wins; other receives clear version conflict |
| A04 | Translator edits visible Spanish body | Spanish projector refreshes automatically without approval |
| A05 | Translator edits unrelated draft | No draft content appears in public API or events |
| A06 | Operator switches live amendment | Every projector shows correct current amendment/language |
| A07 | Projector disconnects; multiple changes occur; reconnects | Snapshot refresh restores authoritative current state |
| A08 | Live body loses slides while on last page | Deterministic validated/clamped page behavior, no blank crash |
| A09 | Anonymous client guesses amendment IDs | Cannot fetch arbitrary amendment/body/archive |
| A10 | Anonymous client calls mutation or publishes event | Rejected consistently |
| A11 | Translator token attempts timer/presentation control | Denied unless assigned the required operator role |
| A12 | Timer runs for 10 minutes | No per-second backend activity; countdown persists/reconstructs |
| A13 | Stream event delivered twice or out of order | Version-aware client converges to latest state |
| A14 | Publisher temporarily fails | Alarms/retry path visible; projector eventually re-syncs |
| A15 | Full migration rehearsal | Source/target counts, content, relationships and fixtures reconcile |
| A16 | CDK production diff | No unexpected database replacement, public bucket or NAT/RDS cost |
| A17 | Conference approves child amendment | `isBeingApproved` transition and cleanup produce the exact expected parent amendment content, with struck-through text removed and underlined text retained without tags |
| A18 | Approval saved/retried twice | No accidental repeated cleanup or duplicate modifications; documented idempotency/conflict behavior |
| A19 | Approval fails during parent modification | No inconsistent partially-approved child/parent state; conflicts surfaced and retry/recovery tested |
| A20 | Approved child updates currently projected parent body | Authorized projector refreshes updated parent text through the live-body pipeline, without a separate operator publish action |

---

## 12. Open Questions to Resolve in Phase 0–1

The coding agent should **discover or ask** rather than invent answers:

1. Can multiple amendments be live simultaneously or is there exactly one per session? Is `MAIN` the only presentation or do independent venues/screens require keyed presentation sessions?
2. Do projector screens display **different** languages simultaneously, and do page controls act globally or per language?
3. Must live translations update on *every keystroke* or after a successful translator **Save**? This plan assumes **successful save**.
4. Beyond the verified false→true `isBeingApproved` transition and full parent-body replacement, what are the underlying service side effects, rollback expectations, behavior for multiple approved siblings, reversals/re-approvals, and any markup corner cases? (Keep this separate from translator live-edit behavior.)
5. What should happen when a currently projected amendment is archived/deleted or hidden?
6. What are the complete search/sort filters for amendment lists (determines GSI needs)?
7. What are the exact role names, user migration needs and authentication UI requirements?
8. What import/export formats, templates and audit expectations exist in production?
9. Which AWS region, DNS name(s), environment separation, retention/backup, conference traffic and budget thresholds should be used?
10. How quickly should a successful live save appear (target SLO)? Decide and measure; avoid promising instant delivery with an asynchronous stream.

---

## 12A. .NET 10-specific Acceptance Checks

- [ ] `global.json` pins a supported .NET 10 SDK; local and CI `dotnet --version` match the intended policy.
- [ ] All maintained `.csproj` files target `net10.0`; any time-boxed legacy-host exceptions are recorded and eliminated by Phase 11.
- [ ] Blazor WASM publishes and starts in a real browser with .NET 10 runtime assets and no critical JS/interop or authentication regression.
- [ ] Domain/API/client/test code compiles without bypassing existing validation or weakening security.
- [ ] CDK C# project targets `net10.0`; `cdk synth`/assertions pass consistently in CI.
- [ ] Every new managed .NET Lambda runs on **`dotnet10`** in CDK and is smoke-tested after deployment; ZIP deployment remains the default.
- [ ] No .NET 8/9 targets or older Lambda runtimes remain in the **final active deployment**; tests confirm feature parity.

---

## 13. Suggested First Prompt to the AI Coding Tool

> You are modernizing https://github.com/AdamLJohnson/Amendment according to `AMENDMENT_MODERNIZATION_PLAN.md`. Read the entire plan first. **Do only Phase 0**. The agreed runtime target is **.NET 10 (`net10.0`) across all maintained C# projects, including Blazor WASM, tests, Lambdas and C# CDK; execute the actual retargeting at Phase 1A**. Inspect the actual codebase carefully and produce the current-state architecture, feature-parity checklist, security/role matrix, real-time event inventory, data/access-pattern inventory, legacy test baseline, open questions, and migration risk register. Add only safe characterization tests/documentation as needed. Do not introduce AWS infrastructure or delete legacy code yet. Run available builds and tests, report the commands and results, and stop for my approval at Checkpoint 0. Distinguish verified behavior from assumptions.

For later sessions:

> Continue Amendment modernization from the last **approved checkpoint**. Review the blueprint and `docs/modernization/decisions.md`, inspect code/PR status, implement **only the next phase**, run the phase's tests and checkpoint criteria, report costs/risks/rollback, and stop for review. Do not skip phases or change confirmed decisions silently.

---

## 14. AWS Implementation References

- AWS CDK v2 with C#: https://docs.aws.amazon.com/cdk/v2/guide/work-with-cdk-csharp.html
- CDK application development: https://docs.aws.amazon.com/cdk/v2/guide/develop.html
- AppSync GraphQL auth modes: https://docs.aws.amazon.com/appsync/latest/devguide/security-authz.html
- AppSync Events: https://docs.aws.amazon.com/appsync/latest/eventapi/event-api-welcome.html
- DynamoDB transactions and conditional expressions: https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/transaction-apis.html
- DynamoDB Streams: https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Streams.html
- AWS Lambda with .NET: https://docs.aws.amazon.com/lambda/latest/dg/lambda-csharp.html
- AWS .NET 10 Lambda announcement: https://aws.amazon.com/about-aws/whats-new/2026/01/aws-lambda-dot-net-10/
- AWS .NET 10 Lambda upgrade guide: https://aws.amazon.com/blogs/compute/net-10-runtime-now-available-in-aws-lambda/
- S3/CloudFront static website security: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/private-content-restricting-access-to-s3.html

**Design principle:** Keep DynamoDB as truth, make permissions explicit, maintain instant live-content visibility for trusted translators, use AWS managed primitives sparingly, and make each checkpoint independently reviewable.
