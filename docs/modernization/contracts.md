# Phase 1 — Target Domain Contracts

Code: `src/Amendment.Domain` (net10.0, no external packages). Tests: `test/Amendment.Domain.Tests`. GraphQL draft: `schema.draft.graphql`. The deployed Phase 4 subset is `infra/Amendment.Cdk/Schema/amendment.graphql` (decisions.md §1E). The legacy app does not use any of this yet.

**[L]** = same as legacy; **[Δ]** = deliberate change from legacy (each one is covered by a test and listed in `decisions.md`).

## 1. Entities and keys

| Entity | Key (`PK` / `SK`) | Notes |
|---|---|---|
| `AmendmentRecord` | `AMENDMENT#<id>` / `META` | String ids (ULID planned). No `IsLive`. |
| `AmendmentBodyRecord` | `AMENDMENT#<id>` / `BODY#<lang>` | One item per language. `Status` keeps the legacy `AmendmentBodyStatus` values. No `IsLive` or `Page`. |
| `PresentationState` | `PRESENTATION#MAIN` / `STATE` | Active amendment, plus `visible`/`page` per language. |
| `TimerState` | `TIMER#MAIN` / `STATE` | Timestamps, not ticks. |

- **Languages.** Codes are normalized to lowercase BCP-47 (`en`, `es`, `fr`, `pt-br`). Legacy codes `ENG`/`SPA`/`FRA` and names `English`/`Spanish`/`French` map to the same key. The configured set defaults to en/es/fr. **[Δ]** Bodies are keyed by language code instead of an integer `LanguageId`.
- **Audit.** `AuditStamp(By, At)` is recorded on create and on every update.

## 2. Validation and create/update semantics

| Rule | Behavior |
|---|---|
| Amendment title, author, primary language | Required; whitespace counts as blank. The title `"not allowed"` is rejected **[L]**. Language must be configured. |
| Body markdown | Blank/whitespace rejected **[L]**. Max 100,000 chars **[Δ, VERIFY limit]**. |
| Create body | The amendment must exist. A duplicate language returns **`CONFLICT`** **[Δ: legacy returned 400 validation]**. Starts at version 1. |
| Update body | `expectedVersion` is required (CAS: compare-and-swap, so a stale version is rejected instead of overwriting). The language can't change; use delete + create **[Δ]**. |
| Update amendment | `expectedVersion` required. Cannot set `isApproved` false→true; use `approveAmendment` **[Δ]**. Un-approving is allowed with no side effects **[L]**. An amendment can't be its own parent. |
| Archive | Blocked while the amendment is presented. This matches the legacy UI gate; legacy had no server check **[Δ]**. Idempotent. |
| Delete amendment | Blocked while presented **[Δ]**. |
| Clone | Copies metadata (overridable) and all bodies; `parentAmendmentId = source`; not approved/archived **[L]**. All-or-nothing **[Δ: legacy skipped bodies that failed]**. |
| Slides | `**NEWSLIDE**` split; empty markdown = 0 pages **[L]**. A page outside `[0, count-1]` **resets to 0** (no clamping) **[L]**. |

## 3. Versions and errors

- Each item has a 64-bit `version`. It is 1 on create and +1 per effective change of that item only. A no-op (same visibility/page, clear when already empty, re-show) does not bump it.
- Body, amendment and presentation writes require `expectedVersion`. Timer operations are last-writer-wins: they come only from operators and are naturally ordered.
- Error codes: `VALIDATION_ERROR` (with `field`), `NOT_FOUND`, `FORBIDDEN`, `CONFLICT` (stale version or duplicate), `PRECONDITION_FAILED` (presented/archived/missing parent), `RATE_LIMITED`, `INTERNAL_ERROR`.

## 4. Role-to-operation matrix (decision D4, `RoleOperationMatrix`)

Legend: ✔ allowed, — denied. "Auth" = any signed-in user, even with no role.

| Operation | Anon | Auth | Translator | Amend. Editor | Screen Ctrl | Admin |
|---|---|---|---|---|---|---|
| publicPresentation, publicTimer, public settings, languages | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| List/read amendments, export, change own password | — | ✔ | ✔ | ✔ | ✔ | ✔ |
| Create/update/archive/clone/**approve** amendment | — | — | — | ✔ | — | ✔ |
| Approve with `BEST_EFFORT` | — | — | — | — | — | ✔ |
| Delete amendment | — | — | — | — | — | ✔ |
| Create/update/delete body (**translator live edits**) | — | — | ✔ | ✔ | — | ✔ |
| Present/clear/set languages, timer control | — | — | — | — | ✔ | ✔ |
| Settings, users, roles | — | — | — | — | — | ✔ |

- Role names are matched exactly (ordinal); unknown roles get nothing extra. An operation without a rule is denied.
- **[Δ]** Legacy enforced most of these only in the UI (see `current-state.md` §4).
- [VERIFY] Body delete uses the same roles as body edit; the legacy UI gate for deletion was not traced.

## 5. Conference approval (`ConferenceApproval.Plan`)

- Only a false→true change applies cleanup **[L]**. Approving an already-approved amendment is a successful no-op, even with a stale version, so retries are safe.
- `cleanChildBodies` (legacy `UpdateCurrentAmendmentBodies`): when true, only the child bodies that change are written **[L]**.
- **Parent update always runs when there is a parent** **[L]**: each parent body with a matching language is **replaced** by `Cleanup(source)`. `source` is the cleaned child text if the child was cleaned, otherwise the original child text. That is legacy's double-cleanup behavior **[L]**. Unchanged parent bodies are not written; unmatched languages are skipped **[L]**.
- Golden: `We ~~shall~~ <u>will</u> proceed.` becomes `We  will proceed.` (double space kept).
- The cleanup regexes are identical to legacy, and the domain tests reuse the same golden cases.
- **Failure mode (D5):**
  - `ALL_OR_NOTHING` (default): child body writes, parent body writes and the amendment write are committed in **one transaction**. A missing parent, a parent body that would become empty, or a version conflict fails the approval with nothing written **[Δ]**.
  - `BEST_EFFORT` (Admin only): child bodies plus the amendment are committed atomically. Parent writes are attempted afterwards, and problems become `warnings` (legacy semantics) **[L]**.
- A cleanup that would leave a **child** body empty is always rejected **[Δ]**.
- Every body write is conditional on the version that was read. This catches stale parent overwrites by concurrent sibling approvals; the result is a `CONFLICT` and the caller re-plans.
- Projector refresh: no special call. A committed parent-body change on a visible language produces a normal `liveBody.updated` public event. This fixes the dead legacy `AmendmentBodyChange` event **[Δ, fixes R3]**.

## 6. Presentation (decision D3: one `MAIN` presentation, one active amendment)

- `Present` replaces the active amendment, with every body hidden on page 0. **[Δ VERIFY]** Legacy kept each body's stored `Page`/`IsLive`. Archived amendments can't be presented.
- `SetLanguages` applies a batch of visibility/page changes in one version, like the legacy "All Prev/Next/Go Live" batches. Pages are normalized with the reset-to-0 rule.
- `ReconcileBody`/`RemoveBody` run after body writes on the presented amendment. They reset a page that is now out of range, and add a new language as hidden or remove a deleted one.
- `Clear` is legacy "Clear screens".

## 7. Public read model and events (decision D6)

- `publicPresentation` lists every language of the active amendment with `visible`, `page`, `pageCount`. `markdown`/`bodyVersion` are set **only when the language is visible**. No titles, drafts, archived content or other amendments. **[Δ fixes R5]**
- Channels:
  - `/presentation/main` (public): `presentation.changed {presentationVersion}`, `liveBody.updated {presentationVersion, language, bodyVersion}`.
  - `/timer/main` (public): `timer.changed` carries the timer state.
  - `/amendments/<id>/<lang>` and `/amendments/<id>/meta` (authenticated): `body.changed|body.deleted|amendment.changed|amendment.deleted {itemVersion}`.
- Events never carry markdown; clients re-fetch. Public eligibility is decided by `EventPolicy.PublicForBody` against the **current** presentation state when the event is published.
- Each changed item emits exactly one event, with a typed operation. **[Δ fixes R7]** Legacy sent duplicate events and labelled create/delete as `Update`.

## 8. Timer

- **[L]** Reset()→0 and stopped. Reset(s) and Set(s) keep their legacy meanings (Set keeps a running timer running). Start is ignored at 0 or while running. Reaching 0 stops the timer. Show is independent.
- Remaining time = `max(0, remainingAtLastChange − floor(now − startedAt))`. `publicTimer.serverTime` lets clients correct their clock offset.
- **[Δ]** `Set(0)` on a running timer stops it. Legacy left `IsRunning=true` at 0.
- State is durable and shared, unlike the legacy in-memory singleton.
