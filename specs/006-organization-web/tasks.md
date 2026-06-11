---
description: "Task list for Organization Web — Office & Room Admin UI (006)"
---

# Tasks: Organization Web (Office & Room Admin UI)

**Input**: Design documents in `specs/006-organization-web/` (plan.md, spec.md)

**Tests**: REQUIRED. Each acceptance criterion becomes a failing `@testing-library/angular` spec
(on `vitest-analog`, ADR-0035) before the component/guard exists.

**Organization**: Grouped by phase / user story; each story is independently testable.

## Story label map

| Label | Story | Scenarios | Priority |
|---|---|---|---|
| US1 | Offices list + admin guard | 1, 8, 9, edge: empty | P1 (MVP) |
| US2 | Create office | 2, 3 | P1 (MVP) |
| US3 | Edit office (rename + relocate) | 4 | P2 |
| US4 | Rooms (add + rename) | 5, 6, 7 | P2 |

## Format: `[ID] [P?] [Story] Description with file path`

- **[P]**: parallelizable (different files, no incomplete dependency)
- All backend calls are relative URLs through the gateway; no tokens in the SPA (ADR-0013/0030).

---

## Phase 1: Backend enablement — organization OpenAPI emit (ADR-0036)

- [x] T001 Stand up the OpenAPI spec emit on `organization-api`, mirroring identity: add
  `Microsoft.AspNetCore.OpenApi` + `Microsoft.Extensions.ApiDescription.Server` package refs and the
  `OpenApiDocumentsDirectory` / `OpenApiGenerateDocumentsOnBuild=false` props to
  `backend/apps/organization-api/Roomy.Organization.Api.csproj`; add `AddOpenApi()` + `MapOpenApi()` and the
  `OpenApi:EmitDocument` skip-guards (skip messaging + company seeder during emit) to `Program.cs`;
  commit the emitted `backend/apps/organization-api/Roomy.Organization.Api.json`. Verify: a clean build with
  `-p:OpenApiGenerateDocumentsOnBuild=true` re-emits an identical spec.
- [x] T002 Add the CI drift gates to `.github/workflows/ci.yml`: a "Verify the OpenAPI spec is
  current" step for `organization-api` (build with emit, `git diff --exit-code` the `.json`) and the
  organization client to the "Verify the generated API client is current" step
  (`nx run organization-data-access:generate-client` + diff `generated`).

---

## Phase 2: Data-access lib — `@roomy/organization-data-access`

- [x] T003 Generate the lib `libs/organization/data-access` (`type:data-access`,
  `context:organization`) with `--unitTestRunner=vitest-analog`; add `ng-openapi-gen.json` (input
  `backend/apps/organization-api/Roomy.Organization.Api.json`, output `src/lib/generated`) and the
  `generate-client` target (mirror identity's `project.json`); run it and commit `generated/`.
- [x] T004 [US1] `office.ts` — `Office`/`Room` view models, `OfficeId`/`RoomId` branded ids, and
  `toOffice` mapping the generated DTO at the boundary (ADR-0020). Spec: `office.spec.ts` maps a DTO
  to the branded model (capacity derived from rooms is read straight from the response).
- [x] T005 [US1] `offices-gateway.ts` — `OfficesGateway` facade over the generated client:
  `listOffices`, `createOffice`, `renameOffice`, `relocateOffice`, `addRoom`, `renameRoom`, each a
  relative-URL call mapped to the branded model. Spec `offices-gateway.spec.ts`: each method calls
  the expected generated fn / URL and maps the response (HttpTestingController).

---

## Phase 3: Shared route guards (ADR-0040)

- [x] T006 Generate `libs/shared/feature` (`@roomy/shared-feature`, `type:feature`,
  `context:shared`, `vitest-analog`). **Move** `auth.guard.ts(+spec)`, `admin.guard.ts(+spec)`, and
  `not-authorized.ts/.html/.css(+spec)` from `identity-feature` into it and re-export from
  `src/index.ts`. Rewire `identity.routes.ts` + the `/not-authorized` route + any imports in
  `identity-feature` to consume `@roomy/shared-feature`. All moved specs stay green; `nx affected
  -t lint test` green (no `organization → identity`, no boundary violation).

---

## Phase 4: US1 — Offices list + admin guard (P1, MVP)

**Independent Test**: an administrator on `/offices` sees the office list (name, location, derived
capacity, rooms); an employee is sent to not-authorized and the Offices nav entry is absent; an empty
list shows the empty state; an unauthenticated visitor is redirected to `/bff/login`.

- [x] T007 [US1] `offices-page.spec.ts` (RED) — renders offices from a stubbed `OfficesGateway`
  (name, location, capacity, rooms with name+capacity); empty list → empty-state message; loading +
  error states. `organization.routes.ts` guarded by `authGuard` + `adminGuard` (from
  `@roomy/shared-feature`). `@testing-library/angular`.
- [x] T008 [US1] `offices-page.ts/.html/.css` (standalone, OnPush, signal-based;
  `TranslocoDirective`), the `/offices` route, `organization.*` i18n keys in `en.json` + `de.json`,
  lazy-load `organizationRoutes` in `apps/web/src/app/app.routes.ts`, and the **Offices** admin nav
  entry in the shell shown only to administrators. T007 green.

---

## Phase 5: US2 — Create office (P1, MVP)

**Independent Test**: an admin submits a name + location → the office appears; a duplicate name shows
a field-level "name already taken" message and adds nothing.

- [x] T009 [US2] Extend `offices-page.spec.ts` (RED) — submitting the create-office form calls
  `OfficesGateway.createOffice` and prepends the returned office (201); a `409` shows a localized
  field-level conflict message and leaves the list unchanged; a `5xx` shows a non-blocking error; the
  form has accessible labels and an announced result.
- [x] T010 [US2] Wire the create-office form in `offices-page.ts` (+ `aria-live` result region,
  field-level conflict, FR-007) and the i18n keys. T009 green.

---

## Phase 6: US3 — Edit office: rename + relocate (P2)

**Independent Test**: an admin changes an office's name or location → the office reflects it; a
duplicate name is rejected with a field-level conflict.

- [x] T011 [US3] Extend `offices-page.spec.ts` (RED) — editing an office's name
  (`PATCH …/name`) and location (`PATCH …/location`) calls the gateway and updates the row from the
  returned office; `409` on rename → field-level conflict; `404` → "no longer exists" + refresh.
- [x] T012 [US3] Wire the inline edit-office affordance in `offices-page.ts` and the i18n keys.
  T011 green.

---

## Phase 7: US4 — Rooms: add + rename (P2)

**Independent Test**: an admin adds a room (capacity ≥ 1) → it appears under the office and the
office capacity grows; capacity `< 1` or a blank name is rejected client-side; renaming a room
updates it; a duplicate room name shows a field-level conflict.

- [x] T013 [US4] Extend `offices-page.spec.ts` (RED) — adding a room calls
  `OfficesGateway.addRoom` and appends the room + bumps the office's derived capacity (201); capacity
  `< 1` or blank name → form invalid, **no request sent**, localized validation message (FR-006);
  `409` → field-level conflict. Renaming a room calls `renameRoom` and updates it from the returned
  office; `409` → field-level conflict.
- [x] T014 [US4] Wire the add-room + rename-room affordances in `offices-page.ts` (client-side
  capacity/name validation, `aria-live` result, field-level conflict) and the i18n keys. T013 green.

---

## Phase 8: Polish

- [x] T015 [P] Accessibility pass (WCAG 2.2 AA): keyboard operability, focus visibility, roles/names,
  labelled form controls, announced mutation results, `lang` correctness across both languages;
  verify with the testing-library role queries already in the specs. Confirm `de.json` parity with
  `en.json` for the `organization.*` namespace (no missing keys).

---

## Dependencies & Execution Order

- **Phase 1 (emit)** → **Phase 2 (data-access)**: the generated client needs the committed spec.
- **Phase 3 (shared guards)** is independent of 1–2 and unblocks the guard import in Phase 4; it can
  run in parallel with Phase 1–2 but must land before T008.
- **US1** is the MVP entry. **US2** depends on US1's page. **US3/US4** extend the same page.
- Within each story: **spec first (must FAIL)** → component → i18n. Commit per task or logical group.

## Notes

- Tests fail before implementation (verify RED).
- No new gateway route (the `/offices/{**}` YARP route already exists).
- ADR-0040 (shared guards) is the only new ADR; ADR-0036 (codegen) is applied, not re-decided.
- Token-free throughout (ADR-0013); same-origin relative URLs (ADR-0030); generated, drift-gated
  client (ADR-0036).

## Status reconciliation

All tasks (T001–T015) are complete. The offices page (`libs/organization/feature/`) uses a single
**active-editor** model — one inline editor (rename office / change location / add room / rename
room) open at a time — which keeps the form state correct across multiple offices. Mutations refresh
the affected office from the mutation response (add-room appends the returned room and recomputes the
office's derived capacity); a `409` keeps the editor open with a field-level conflict so the name can
be fixed (the rename-conflict test cancels, then asserts the office is unchanged), a `404` closes the
editor, shows a "no longer exists" notice and reloads the list, and other failures show a
non-blocking error. The success `aria-live` region announces every mutation. The route guards come
from `@roomy/shared-feature` (ADR-0040); no `organization → identity` import. DE/EN key parity is
verified (73/73). The web app builds under strict template checking.
