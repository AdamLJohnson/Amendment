# Feature-Parity Checklist (Phase 0 Baseline)

Status column: **[V]** verified in source/tests, **[VERIFY]** needs runtime or maintainer confirmation. "Test" lists the existing automated coverage. Target behavior is decided in later phases. This is the legacy reference.

## Amendments

| # | Feature | Legacy behavior | Status | Test |
|---|---|---|---|---|
| F1 | List amendments | `GET api/Amendment`, all rows; client shows LIVE/ARCHIVED/CLONE badges | [V] | `AmendmentControllerTests` |
| F2 | Create/Edit amendment | Title, Author, PrimaryLanguageId required (validators) | [V] | `AmendmentControllerTests` |
| F3 | Delete amendment | Admin-only in UI; any authenticated user via API | [V] | — |
| F4 | Archive / Unarchive | PUT with `IsArchived` toggled (non-live rows only in UI) | [V] | — |
| F5 | Clone | Copies metadata (overridable) + all bodies incl. `Page`; sets `ParentAmendmentId` | [V] | `CloneAmendmentTests` |
| F6 | Approve & Clean Up | See §Conference approval | [V] | `AmendmentApprovalTests`, `AmendmentCleanupServiceTests` |
| F7 | Go Live | UI: PUT `IsLive=true`; button hidden while any amendment is live; server does not enforce a single live amendment | [V] / [VERIFY] uniqueness intent | — |
| F8 | Export Excel / PDF | Selected IDs → `Amendments.xlsx` / `Amendments.pdf` | [V] layout [VERIFY] | — |

## Amendment bodies (languages / translations)

| # | Feature | Legacy behavior | Status | Test |
|---|---|---|---|---|
| B1 | Create/Edit body | One body per language per amendment; `AmendBody` required, `LanguageId != 0` | [V] | `AmendmentBodyControllerTests` (1 baseline failure) |
| B2 | Markdown rendering | Markdig advanced extensions + soft→hard line breaks; raw HTML allowed (no sanitizer) | [V] | — |
| B3 | Slides | `**NEWSLIDE**` delimiter; `Pages` = split count; out-of-range page → 0 | [V] | — |
| B4 | Live save propagation | After REST save, events go to amendmentHub; screenHub only if the body belongs to the live amendment | [V] | `ClientNotifierTests` |

## Live presentation

| # | Feature | Legacy behavior | Status | Test |
|---|---|---|---|---|
| P1 | Screen Control page | Live amendment; per-language preview; All Go Live / Take all down; Reset All / All Prev / All Next | [V] | — |
| P2 | Per-body live/page | `ScreenControlItem` → hub `AmendmentBodyLive` / `AmendmentBodyPage` | [V] | — |
| P3 | Clear screens | `ClearScreensCommand` → both hubs `ClearScreens`; sets all live flags false | [V] | — |
| P4 | Projector `/View/{Language}` | Anonymous; fullscreen via `js/ViewFullScreen.js`; offline/online CSS; `inverted` system setting; reconnect banner | [V] | — |
| P5 | Home page previews | Three small live previews + View buttons | [V] | — |
| P6 | Languages | English/Spanish/French hard-coded in `NavMenu`; `View` matches by `LanguageName` | [V] | — |
| P7 | Parent refresh after child approval | Intended force-send; anonymous screens likely not refreshed (dead `AmendmentBodyChange`) | [VERIFY] | `ClientNotifierTests.Characterization_*` |

## Timer

| # | Feature | Legacy behavior | Status | Test |
|---|---|---|---|---|
| T1 | Control | Reset, ResetWithTime, Set, Pause, Start, Show (AdminScreenController) | [V] | — |
| T2 | Display | `CurrentState` + `SecondsChanged`/`StateChanged`/`ShowChanged`; small control on `/ScreenControl` | [V] | — |
| T3 | State | In-memory singleton; lost on restart; not shared across instances | [V] | — |
| T4 | End-of-timer behavior | Counts down 1/s; at 0 sets `IsRunning=false`. `Start` ignored at 0 or while running; `Set` keeps running state (`TimerService`) | [V] | `TimerStateTests` (target model) |

## Identity and administration

| # | Feature | Legacy behavior | Status | Test |
|---|---|---|---|---|
| I1 | Login / refresh / logout | JWT + refresh token; client `RefreshTokenService`, local storage | [V] | `AccountControllerTests` |
| I2 | Change password / forced change | `PasswordChangeRedirect` wrapper in `MainLayout` | [V] mechanism [VERIFY] trigger | — |
| I3 | Users / roles admin | Admin-only API and UI | [V] | — |
| I4 | System settings | Admin UI; GET anonymous; mutate any authenticated (API) | [V] | — |
| I5 | Client error logging | Anonymous `POST api/ClientError/log`; `AmendmentErrorBoundary` | [V] | — |
| I6 | Version check / update notification | `GET api/Version`; `VersionCheckManager`, `UpdateNotification` | [V] | `VersionControllerTests` |

## Conference approval (plan §2B) — characterized

| Rule | Legacy behavior | Test (`AmendmentApprovalTests`) |
|---|---|---|
| Only false→true triggers | Re-save of approved and un-approve do not touch bodies | `Characterization_AlreadyApproved_*`, `Characterization_Unapprove_*` |
| Optional child cleanup | `UpdateCurrentAmendmentBodies` true → changed child bodies saved; false → untouched | `Characterization_GoldenFixture_*` (true/false), existing tests |
| Parent replacement | Always (if parent exists): matching-language parent `AmendBody` replaced by cleaned child text | `Characterization_GoldenFixture_*` |
| Golden | `We ~~shall~~ <u>will</u> proceed.` → `We  will proceed.` | both handler and cleanup goldens |
| Language matching | Unmatched languages skipped; unchanged parent not written / not force-sent | `Characterization_MultiLanguage_*` |
| Missing parent | Logged, approval succeeds | `Characterization_MissingParent_*` |
| Failure handling | Parent exceptions swallowed; approval saved anyway | `Characterization_ParentUpdateThrows_*` |
| Ordering / partial update | Child bodies → parent bodies → amendment save; amendment-save failure returns error after bodies are persisted | `Characterization_BodiesSavedBeforeAmendment_*` |
| Live parent force-send | `UpdateBodyAsync(parentBody, true)` per changed parent body | existing + multi-language test |
| Cleanup edge cases | Multiline, `<U>`, unclosed markers left as-is, `<u attr>` not matched, `**NEWSLIDE**` inside deletion removed | `AmendmentCleanupServiceTests.Characterization_CleanupAmendmentText_Golden` |
| Not yet characterized | Multiple approved siblings on one parent (last write wins), archived parent, real-DB side effects | [VERIFY] |
