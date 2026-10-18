# Amendment — Current State (Phase 0 Baseline)

Branch: `modernization/phase-0-baseline`. Tags: **[V]** = verified from source and/or tests; **[VERIFY]** = inferred, needs runtime/maintainer confirmation.

## 1. Architecture

| Project | TFM | Role |
|---|---|---|
| `Amendment.Client` | net9.0 (BlazorWebAssembly) | Blazor WASM UI (Blazorise, Markdig, SignalR client) |
| `Amendment.Client.Repository` | net9.0 | Client-side HTTP repositories |
| `Amendment.Server` | net9.0 (Web) | ASP.NET Core host: controllers, SignalR hubs, JWT, static WASM hosting |
| `Amendment.Server.Mediator` (`src/Amendment.Server.Handlers`) | net9.0 | MediatR commands/queries/handlers, FluentValidation validators |
| `Amendment.Service` | net9.0 | Business services, cleanup, exports (ClosedXML 0.105.0, QuestPDF 2025.5.0) |
| `Amendment.Repository` | net9.0 | EF Core `AmendmentContext`, repositories, `UnitOfWork`, migrations (Npgsql 9.0.4) |
| `Amendment.Model` | net9.0 | EF entities; Markdown rendering/paging on `AmendmentBody` |
| `Amendment.Shared` | net9.0 | Requests/responses, validators, `RoleGroups`, SignalR command DTOs |
| Tests | net9.0 | `src/Amendment.Tests` (**not in `Amendment.sln`**), `test/Amendment.Server.Tests`, `test/Amendment.Client.Repository.Tests` |

- [V] No `global.json` and no `Directory.Packages.props`. Installed SDK `10.0.202` builds the net9.0 projects.
- [V] Host: Autofac DI, MediatR pipeline `ValidationBehavior` → `ClientNotificationBehavior`, plus a service-level `IClientNotifier` (`ClientNotifier`).
- [V] DB: `DbType=DB` → PostgreSQL + `MigrateAsync`; otherwise EF InMemory + `EnsureCreatedAsync`. `SeedDatabase` always runs at startup.
- [V] SignalR scale-out: Redis backplane if `ConnectionStrings:Redis` is set.
- [V] Deploy/CI: `src/Amendment.Server/Dockerfile`, `.github/workflows/docker-publish.yml`, `azure-pipelines.yml`, `.github/dependabot.yml`.

## 2. Data model and access patterns [V]

- `Amendment`: `Id, Title*, Author, Motion?, Source?, LegisId?, IsLive, IsArchived, IsApproved, ParentAmendmentId?, PrimaryLanguageId*`, audit fields (`EnteredBy/Date`, `LastUpdatedBy/LastUpdated`), `AmendmentBodies`.
- `AmendmentBody`: `Id, AmendId*, LanguageId*, AmendBody*, AmendStatus, IsLive, Page`, audit fields. Computed (not mapped): `Pages` = `AmendBody.Split("**NEWSLIDE**").Length`; `AmendBodyHtml`/`AmendBodyPagedHtml`/`AmendBodyPages` via Markdig `UseAdvancedExtensions().UseSoftlineBreakAsHardlineBreak()`. **No HTML sanitization** (raw HTML such as `<u>` passes through).
- Other entities: `Language`, `Role`, `User`, `SystemSetting`, refresh tokens (via account handlers).

| Access path | Implementation |
|---|---|
| List amendments | `AmendmentService.GetAllAsync` (full table, includes bodies via repository `Query`) |
| Get live amendment | `_repository.GetAsync(a => a.IsLive)` — first match; **not unique-enforced** |
| Bodies by amendment | `GetManyAsync(q => q.AmendId == id)` |
| Unique language per amendment | `ValidateLanguageId`: no other body with same `AmendId`+`LanguageId` |
| Bulk live / page | `SetIsLiveForManyAsync` / `SetPageForManyAsync` (`ExecuteUpdate` on DB, tracked update on InMemory) |
| Clear screens | Sets `IsLive=false` on all live amendments and bodies (`ScreenControlService.ClearAll`) |
| Clone | New amendment with `ParentAmendmentId = source.Id`, not live/archived/approved; bodies copied incl. `Page`; per-body failure is logged and skipped |
| Export | `POST api/Amendment/ExportExcel` → `Amendments.xlsx` (sheet "Amendments"; columns Title, Author, Motion, Source, Legislative ID, Is Archived, Language, Slide N…); `ExportPdf` → `Amendments.pdf` (QuestPDF) |

- [V] Paging: `ChangeBodyPage` resets out-of-range page to 0. Client `ChangeBodyPage` computes Prev/Next before calling the hub.
- [VERIFY] Production row counts/data size, existing backups, and the production `DbType`.

## 3. Real-time event inventory

Hubs: `/amendmentHub` (`[Authorize]`), `/screenHub` (`[AllowAnonymous]`, no methods), `/timerHub` (class anonymous; mutating methods role-gated). JWT is read from `access_token` query only for `/amendmentHub` and `/timerHub`.

[V] Connection choice (`MainLayout.razor`): **authenticated → `AmendmentHubClient` only; anonymous → `ScreenHubClient` only.** Both use WebSockets with `SkipNegotiation=true`, automatic reconnect, and a 5 s retry on start failure.

Client → server (amendmentHub): `AmendmentBodyLive(SetAmendmentBodyLiveCommands)`, `AmendmentBodyPage(SetAmendmentBodyPageCommands)`. **No role check; any authenticated user.**
Client → server (timerHub): `CurrentState` (anonymous), `Reset`, `ResetWithTime`, `Set`, `Pause`, `Start`, `Show` (`AdminScreenController`).

Server → client (client listens for: `AmendmentUpdate`, `AmendmentBodyUpdate`, `AmendmentBodyUpdateMany`, `SystemSettingUpdate`, `ClearScreens`, timer `SecondsChangedEvent`/`StateChangedEvent`/`ShowChangedEvent`):

| Trigger | Source | amendmentHub | screenHub |
|---|---|---|---|
| Create/Update amendment command | `ClientNotificationBehavior` | `AmendmentUpdate` | `AmendmentUpdate` if result `IsLive` |
| Delete amendment | behavior | `AmendmentUpdate` (Delete) | — |
| Create/Update body command | behavior | `AmendmentBodyUpdate` | `AmendmentBodyUpdate` if body's amendment is the live one |
| Delete body | behavior | `AmendmentBodyUpdate` (Delete) | — |
| Bulk live / page (hub) | behavior | `AmendmentBodyUpdateMany` | same if first body's amendment is live |
| `UpdateSystemSettingCommand` | behavior | `SystemSettingUpdate` | — |
| `ClearScreensCommand` | behavior | `ClearScreens` | `ClearScreens` |
| `AmendmentService.Create/Update/Delete` | `ClientNotifier` | `AmendmentUpdate` | `UpdateAmendmentAsync` → `AmendmentUpdate` if live |
| `AmendmentBodyService.Create/Update/Delete` | `ClientNotifier` | `AmendmentBodyUpdate` (always `OperationType.Update`, even on create/delete) | Update only: `UpdateBodyAsync(item,false)` → raw **`AmendmentBodyChange`** if body live |
| `ScreenControlService.UpdateBodyAsync(body, forceSend:true)` (approval of child with live parent) | `ClientNotifier.SendToLanguageScreenAsync` | — | raw **`AmendmentBodyChange`** (entity payload) |
| `SystemSettingService` | `ClientNotifier` | — | `SystemSettingUpdate` |
| `RefreshLanguage` | `ClientNotifier` | `LanguageUpdate` (no client listener) | — |
| Timer tick/state/show | `TimerService` (singleton) | timerHub `Clients.All` | |

- [V] **`AmendmentBodyChange` has no client subscriber.** The approval "force projector refresh" therefore never reaches the client as a handled event (characterized by `ClientNotifierTests.Characterization_SendToLanguageScreenAsync_*`).
- [VERIFY] Approving a child with a live parent: authenticated viewers get the parent `AmendmentBodyUpdate` through `AmendmentBodyService.UpdateAsync`. Source analysis says **anonymous projectors do not receive the parent body change** (screenHub gets only the dead `AmendmentBodyChange`). Needs runtime confirmation (plan acceptance A20).
- [V] Duplicate notifications: body/amendment updates via REST emit both behavior and service events (double `AmendmentBodyUpdate`/`AmendmentUpdate` on amendmentHub).
- [V] Public pages (`/`, `/View/{Language}`) handle only body events. `View.razor` resolves the language by `LanguageName` and has no handler for `AmendmentUpdate`/`ClearScreens`.
- [V] Timer state is in-memory in a singleton: it is not shared across instances (even with Redis) and resets on restart.

## 4. Security / role matrix [V]

Roles (`RoleGroups`): `System Administrator`, `Screen Controller`, `Amendment Editor`, `Translator`. All controllers inherit `Amendment.Server.ControllerBase` (`[Authorize]`), so they require authentication by default.

| Endpoint | Server rule | Client UI gate |
|---|---|---|
| `POST api/Account/Login`, `RefreshToken` | Anonymous | — |
| `POST api/Account/ChangePassword` | Authenticated | `[Authorize]` |
| `GET api/Amendment/Live` | **Anonymous** — returns full live amendment incl. **all bodies, live or not** | public |
| `GET/POST/PUT/DELETE api/Amendment[/{id}]` | **Any authenticated** (incl. approve/archive/go-live via PUT; delete) | Create/Edit: AdminAmendEditor; Delete: Admin; Go Live: AdminScreenController |
| `POST api/Amendment/{id}/Clone` | AdminAmendEditor | — |
| `POST api/Amendment/ExportExcel`, `ExportPdf` | Any authenticated | — |
| `api/Amendment/{id}/Body` CRUD | **Any authenticated** | AdminTranslatorAmendEditor |
| `GET api/Language`, `GET api/SystemSetting`, `GET api/Version`, `POST api/ClientError/log` | Anonymous | — |
| `api/SystemSetting` GET{id}/POST/PUT/DELETE | **Any authenticated** | Admin |
| `api/User` | Admin | Admin |
| `api/Role` | Admin | — |
| `api/Values` (template stub), `ScreenControlController` (empty) | Authenticated | — |
| amendmentHub methods | **Any authenticated** | ScreenControl page: AdminScreenController |
| timerHub mutators | AdminScreenController | TimerControl: AdminScreenController |

- [V] Server authorization is weaker than the UI implies: role separation is enforced mostly client-side.
- [VERIFY] Whether `/timer` and the timer component work for anonymous users (`TimerComponent` uses the auth-required `TimerControlHubCommandService`).

## 5. Client routes [V]

`/`, `/View/{Language}`, `/login`, `/logout`, `/error-test` (public); `/change-password`, `/Amendment` (authenticated); `/Amendment/Create|Edit/{id}` (AdminAmendEditor); `/Amendment/{AmendmentId}/Body/Create|Edit/...` (AdminTranslatorAmendEditor); `/ScreenControl`, timer control (AdminScreenController); `/Admin/User*`, `/Admin/SystemSetting` (Admin); `/timer`, `/timer/control` (page-level gate per component).

## 6. Legacy test baseline

See `decisions.md` §1 for commands and results.
