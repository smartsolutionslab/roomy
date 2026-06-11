# Feature Specification: Offices page component extraction (behaviour-preserving)

**Feature Branch:** `refactor/frontend-cleanup`
**Status:** Draft
**Created:** 2026-06-11
**Updated:** 2026-06-11
**Realizes:** A structural refactor of the existing offices administration UI
(`002-office-management` / `006-organization-web`). No functional requirement changes; no new
endpoint, contract, translation key, or icon. It splits the single 235-line `OfficesPage` smart
component into the page plus focused presentational children, establishing the pattern recorded in
**ADR-0060**.

## Summary

`OfficesPage` (`organization/feature/src/lib/offices/offices-page.ts`, 235 lines) is one smart
component that does three things at once: orchestrates loading and gateway calls and maps HTTP errors
to feedback signals; owns the page-level form/editor state (`createForm`, a shared `textForm`, a shared
`roomForm`, and a single `activeEditor` so **only one inline editor is open at a time**); and renders a
large per-office template fragment that is repeated with `@for` (~110 of its ~160 template lines render
*one* office).

This feature extracts the presentational pieces into co-located child components while the page stays the
**single smart orchestrator** (ADR-0060, option D). The page keeps the gateway, all HTTP calls, the form
groups, the `activeEditor`, and the `result`/conflict/failed/not-found feedback signals; the children are
`standalone`, `OnPush`, signal `input()`/`output()` only, inject nothing, and do no I/O. Because the forms
and `activeEditor` remain on the page and are passed down, the single-editor-at-a-time behaviour is
preserved exactly. The extracted children are private to the page (not exported from the lib barrel); none
is promoted to a `ui` library, because none is reused beyond this page (ADR-0060 promotion rule).

This is a pure structural refactor: the rendered output, the strings (DE + EN via Transloco, ADR-0024),
the accessibility, and every observable behaviour are identical to today. The 17 existing `OfficesPage`
tests are the contract and stay green unchanged; each new child additionally gets its own focused unit
test. Standalone / signal-based / zoneless / OnPush throughout (ADR-0016/0035).

> **Not here:** no change to what the page does, the gateway, the endpoints, the routes, the strings, the
> icons, or the layout. No new Nx library or tag. No change to the single-editor-at-a-time interaction. The
> only thing that changes is how the page's template and rendering are decomposed into components.

## User Scenarios & Testing

### Primary User Story

As an administrator managing offices and rooms, I want the offices page to look and behave exactly as it
does today — so that this internal refactor is invisible to me.

### Acceptance Scenarios

These restate the existing, passing `OfficesPage` behaviours; after the refactor they must still hold,
now satisfied through the extracted components.

1. **Lists each office with its location, derived capacity and rooms**
   - GIVEN offices are loaded
   - WHEN the page renders
   - THEN each office shows its name, location, derived capacity, and its rooms — rendered by `OfficeCard`

2. **Empty and load-error states are unchanged**
   - GIVEN no offices / a failing load
   - WHEN the page renders
   - THEN the existing empty message / load-error message is shown

3. **Create office (success, name conflict, generic failure) is unchanged**
   - GIVEN the create form, rendered by `CreateOfficeForm`
   - WHEN it is submitted
   - THEN a success prepends the office and announces the result; a 409 shows the field-level name
     conflict and adds nothing; another error shows the generic create error — all identical to today

4. **Rename / relocate an office is unchanged**
   - GIVEN an `OfficeCard`
   - WHEN its rename or relocate editor is opened, edited and saved
   - THEN the office reflects the change with the announced result; a 409 shows the name conflict and keeps
     the editor open; a 404 announces the office no longer exists and reloads

5. **Add room and rename room are unchanged**
   - GIVEN an `OfficeCard` / `RoomRow`
   - WHEN a room is added (with the existing name-required and capacity-min validation and name-conflict
     handling) or renamed
   - THEN the rooms list and derived office capacity update with the announced result, exactly as today

6. **Only one inline editor is open at a time**
   - GIVEN one office's editor (or one room's rename) is open
   - WHEN another office's or room's editor is opened
   - THEN the first closes — the shared `activeEditor` still governs all cards (behaviour preserved)

7. **Each child renders and emits in isolation**
   - GIVEN a child component rendered with stub inputs
   - WHEN the user interacts (submit, open editor, save, cancel)
   - THEN it renders from its inputs and emits the corresponding intent output — with no gateway and no
     shared state of its own

### Edge Cases

- **A child that hosts an inline editor receives a shared `FormGroup` input.** It binds and reads that
  group but never constructs or resets it (the page owns its lifecycle). Validation messages render from
  the inputs the page already exposes (`roomAttempted`, control validity, conflict/failed flags).
- **Result vs. error placement.** The single page-level `result` banner and the top-level
  `editNotFound`/`loadFailed` messages stay on the page (they are page-scoped, not per-card); only the
  per-office/per-room feedback that already lives inside the repeated fragment moves into the children.
- **No double source of truth.** Children compute "is *my* editor open" purely from inputs; they do not
  keep a private copy of `activeEditor`.

## Requirements

### Functional Requirements

- **FR-1** The offices page MUST render and behave identically to before the refactor; all 17 existing
  `OfficesPage` acceptance scenarios MUST still pass, unchanged.
- **FR-2** The per-office fragment MUST become a presentational child component (`OfficeCard`): it receives
  the office and the editor/feedback state it displays via `input()` and reports user intent via `output()`.
  It MUST NOT inject the gateway, perform HTTP, or own shared page state.
- **FR-3** A single room's row MUST become a presentational child (`RoomRow`), used by `OfficeCard`, with
  the same presentational-only contract (read view vs. inline rename, emitting intent).
- **FR-4** The create-office form MUST become a presentational child (`CreateOfficeForm`) bound to the
  page's `createForm`, showing the existing conflict/failed messages and emitting a submit intent.
- **FR-5** The page MUST remain the single smart orchestrator: the injected `OfficesGateway` and all HTTP
  calls, the `createForm`/`textForm`/`roomForm` groups, the `activeEditor` signal, and the
  `result`/conflict/failed/not-found feedback signals all stay on the page.
- **FR-6** Every extracted child MUST be `standalone`, `OnPush`, use signal `input()`/`output()` only, and
  declare no `NgModule` (ADR-0016/0035). Children are private to the page (not exported from the
  `@roomy/organization-feature` barrel).
- **FR-7** The single-editor-at-a-time behaviour MUST be preserved: the shared `activeEditor` on the page
  governs all cards and rooms.
- **FR-8** All user-facing text MUST continue to come from Transloco; no string may be hardcoded; no new or
  renamed translation keys; DE + EN render exactly as today (ADR-0024).
- **FR-9** No Nx module boundary or context boundary may be crossed; the children are co-located in
  `organization/feature` and no new library or tag is introduced (ADR-0060). No backend, gateway, contract,
  OpenAPI, or generated-client change.
- **FR-10** Each new child component MUST have its own unit test, written before its implementation, that
  renders it from stub inputs and asserts it emits the right intent on interaction (Shouldly-equivalent
  Testing-Library assertions; ADR-0035 `vitest-analog` + `@testing-library/angular`).

### Non-Functional / Constraints

- Pure refactor: no change to bundle behaviour, routing, or data flow; no new HTTP calls.
- The children sit at `type:feature`/`context:organization`; they import only `@roomy/shared-ui`,
  `@roomy/organization-api` (for the `Office`/`Room` view types and branded ids), and Angular/Transloco —
  the same imports the page already has. No cross-context import.
- Accessibility unchanged: the existing labels, `aria-label`s (e.g. "Rename room {name}"), and roles move
  with the markup into the children.

## Key Entities

Component contracts (presentational; the page wires them):

- **CreateOfficeForm** — `roomy-create-office-form`. Inputs: the `createForm` group, `conflict`, `failed`.
  Output: `submit`. Renders the create heading, name/location fields, conflict/failed messages, submit button.
- **OfficeCard** — `roomy-office-card`. Inputs: `office`, the current `activeEditor` (to derive which inline
  editor, if any, is open for this office), the shared `textForm` and `roomForm`, and the editor feedback
  flags (`editConflict`, `editFailed`, `roomAttempted`). Outputs: `renameOffice`, `relocateOffice`,
  `addRoom`, `cancelEdit`, plus the room-level intents bubbled from `RoomRow` (`renameRoom`, save). Renders
  one office: name + rename editor, location + relocate editor, the rooms list (via `RoomRow`), and the
  add-room editor.
- **RoomRow** — `roomy-room-row`. Inputs: `room`, whether this room's rename editor is open, the shared
  `textForm`, the editor feedback flags. Outputs: `openRename`, `saveRename`, `cancelEdit`. Renders one room
  as either its read view (name, capacity, rename button) or its inline rename form.

> The exact input/output split is finalised in the plan; the contract above is the seam the spec fixes:
> data and editor/feedback state flow *down*, user intent flows *up*, and no child does I/O.

## Review & Acceptance Checklist

- [ ] Every acceptance scenario has a test; the 17 existing page tests pass unchanged, and each new child
      has its own unit test written before its implementation.
- [ ] `OfficesPage` keeps the gateway, the forms, `activeEditor`, and the feedback signals; no child injects
      the gateway or owns shared state.
- [ ] `CreateOfficeForm`, `OfficeCard`, and `RoomRow` are standalone / OnPush / signal `input()`/`output()`;
      none is exported from the lib barrel.
- [ ] Single-editor-at-a-time behaviour is preserved (a covering test opens a second editor and asserts the
      first closes).
- [ ] Rendered output, strings (DE + EN), `aria-label`s, and layout match the pre-refactor page exactly.
- [ ] No new Nx library/tag; no boundary crossed; no backend/gateway/contract/client change.
- [ ] ADR-0060 recorded; all quality gates green; no suppressions or skipped tests.
