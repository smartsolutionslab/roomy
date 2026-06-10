---
description: "Task list for Attendance On-Behalf — administrator acts for an employee (009)"
---

# Tasks: Attendance On-Behalf (Administrator acts for an employee)

**Input**: Design documents in `specs/009-attendance-on-behalf/` (plan.md, spec.md)

**Tests**: REQUIRED (RED→GREEN). xUnit + Shouldly for the host endpoints + read model;
`@testing-library/angular` on `vitest-analog` for the SPA.

**Organization**: Extends `007`'s `@roomy/attendance-data-access` and `@roomy/attendance-feature`. No new
libs; no gateway change; no backend write change (reserve `onBehalfOf` + admin cancel already exist).

## Story label map
| Label | Story | Scenarios | Priority |
|---|---|---|---|
| US1 | Reserve on behalf of an employee | 1, 4 | P1 (MVP) |
| US2 | View + cancel the employee's reservations on behalf | 2, 3, 5 | P1 (MVP) |
| US3 | Admin gate + nav + localization + a11y | 6, 7, 8 | P1/P2 |

---

## Phase 1: Backend reads (admin-gated)
- [x] T001 [US1] `ViewEmployees` query + `EmployeeView` (employeeId, name) + `IEmployeeCatalog` port +
  `ViewEmployeesHandler`; handler unit test over a faked catalog (returns rows; empty when none).
- [x] T002 [US1] `EmployeeCatalog` adapter over the `Employees` read model (employeeId + displayName),
  ordered by name; register in DI. Read-model integration test (real Postgres).
- [x] T003 [US1/US2] `GET /reservations/employees` (admin → `[{employeeId,name}]`, 403 for non-admin) and
  `GET /reservations/by-employee/{employeeId:guid}` (admin → `[MyReservationResponse]`, reusing
  `ViewMyReservations`); `.WithName/.Produces/.ProducesProblem`. Host tests: admin gets the list / an
  employee's reservations; a non-admin gets 403; no session → 401. Re-emit the OpenAPI spec.

## Phase 2: Data-access
- [x] T004 [US1] `booking.ts` — add `EmployeeId` brand + `employeeId()`. `employee.ts` — `Employee`
  view model + `toEmployee` (+spec).
- [x] T005 [US1/US2] `AttendanceGateway`: `listEmployees()` (GET /reservations/employees),
  `reservationsFor(employee: EmployeeId)` (GET /reservations/by-employee/{id}), and an optional
  `onBehalfOf` arg on `reserve(office, room, date, onBehalfOf?)`; export `Employee`/`EmployeeId`. Spec
  extension (HttpTestingController): URLs/params and mapping; reserve sends `onBehalfOf` in the body.
  Regenerate the client (no spec drift).

## Phase 3: Reserve input (reuse the 007 flow)
- [x] T006 [US1] `ReservePage`: add `onBehalfOf = input<string | null>(null)`; `reserve()` passes it to
  the gateway (null ⇒ self-service, unchanged). Spec: when `onBehalfOf` is set, the reserve call carries
  it; default behaviour and existing specs stay green.

## Phase 4: On-behalf page (US1, US2)
- [x] T007 [US1/US2] RED: `on-behalf/on-behalf-page.spec.ts` — employee picker lists employees; before a
  pick, no reserve form/list (prompt); after a pick, the embedded reserve flow targets that employee
  (onBehalfOf), and the employee's reservations render with cancel on upcoming only; cancel removes the
  row + announces; empty directory + empty reservations states.
- [x] T008 [US1/US2] GREEN: `on-behalf/on-behalf-page.ts/.html/.css` — employee `<select>`
  (`listEmployees`), `<roomy-reserve-page [onBehalfOf]>`, the employee's reservations (`reservationsFor`)
  with cancel (admin cancel authorised); `aria-live` results; `past_immutable` surfaced (FR-004).

## Phase 5: Wiring, localization, accessibility (US3)
- [x] T009 [US3] Add the `on-behalf` route (guarded by `authGuard` + `adminGuard`) to
  `attendance.routes.ts`. Guard behaviour is covered by `shared-feature`; add a smoke test if useful.
- [x] T010 [US3] Add the admin-only `On behalf` nav entry to `apps/web/src/app/app.html` (inside the
  `administrator` block).
- [x] T011 [US3] `attendance.onBehalf.*` i18n in `apps/web/public/i18n/{en,de}.json` + the feature test
  transloco helper; assert DE/EN key parity.
- [x] T012 [US3] WCAG 2.2 AA pass: labelled picker, announced reserve/cancel, keyboard operability,
  visible focus.

## Verify (Definition of Done)
- [x] `dotnet build -warnaserror` + `dotnet test` (host + read model) + `dotnet format --verify-no-changes`.
- [x] `pnpm nx run-many -t test lint -p attendance-data-access attendance-feature web` + `nx build web`.
- [x] OpenAPI spec + generated-client drift gates green.
- [x] EN/DE i18n key parity holds.
- [x] Reconcile this tasks.md on merge.
