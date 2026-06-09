---
description: "Task list for Attendance Web — Reserve, View, Cancel (self-service) (007)"
---

# Tasks: Attendance Web (Reserve, View, Cancel — self-service)

**Input**: Design documents in `specs/007-attendance-web/` (plan.md, spec.md)

**Tests**: REQUIRED. Each acceptance criterion becomes a failing test before the code exists —
`@testing-library/angular` on `vitest-analog` (ADR-0035) for the SPA; xUnit + Shouldly for the
backend host endpoint and the catalogue read model (RED→GREEN).

**Organization**: Grouped by phase / user story; each story is independently testable.

## Story label map

| Label | Story | Scenarios | Priority |
|---|---|---|---|
| US1 | Reserve a place (office → room → day → confirm) | 1, 2, 3, 4, 5, 10 | P1 (MVP) |
| US2 | See my reservations | 6 | P1 (MVP) |
| US3 | Cancel an upcoming reservation | 7, 8 | P1 (MVP) |
| US4 | Change a reservation (cancel + re-reserve) | 9 | P2 |
| US5 | Auth gate + nav + localization + a11y polish | 11, 12, 13 | P1/P2 |

## Format: `[ID] [P?] [Story] Description with file path`

- **[P]**: parallelizable (different files, no incomplete dependency)
- All backend calls are relative URLs through the gateway; no tokens in the SPA (ADR-0013/0030).

---

## Phase 1: Backend enablement — attendance OpenAPI emit (ADR codegen, #0036)

- [x] T001 Stand up the OpenAPI spec emit on `attendance-api`, mirroring identity/organization: add
  `Microsoft.AspNetCore.OpenApi` + `Microsoft.Extensions.ApiDescription.Server` package refs and the
  `OpenApiDocumentsDirectory` / `OpenApiGenerateDocumentsOnBuild=false` props to
  `apps/attendance-api/Roomy.Attendance.Api.csproj`; add `AddOpenApi()` + `MapOpenApi()` and the
  `OpenApi:EmitDocument` skip-guards (skip messaging/inbox + any seeder during emit) to `Program.cs`.
  Verify: a clean build with `-p:OpenApiGenerateDocumentsOnBuild=true` boots through `getdocument`
  with no broker/DB.
- [x] T002 Annotate the existing endpoints for the typed client: add `.WithName/.Produces/
  .ProducesProblem` to `Endpoints/ReservationEndpoints.cs` (POST/DELETE/GET /reservations,
  GET /reservations/mine) and `Endpoints/OccupancyEndpoints.cs` (GET /occupancy), covering their
  documented status codes (201/204/200/400/403/404/409/422). Commit the emitted
  `apps/attendance-api/Roomy.Attendance.Api.json`.
- [ ] T003 Add the CI drift gates to `.github/workflows/ci.yml`: a "Verify the OpenAPI spec is
  current" step for `attendance-api` (build with emit, `git diff --exit-code` the `.json`) and the
  attendance client to the "Verify the generated API client is current" step
  (`nx run attendance-data-access:generate-client` + diff `generated`).

---

## Phase 2: Bookable catalogue — `GET /rooms` (D-AW2)

- [ ] T004 [US1] RED: `IBookableRoomsReadModel` port +
  `libs/attendance/application/UseCases/ViewBookableRooms.cs` (query) + `BookableRoomView.cs`
  (officeId, officeName, roomId, roomName, capacity) + `ViewBookableRoomsHandler` returning the
  company's bookable rooms. Handler unit test (Shouldly) over a faked read model: groups/returns
  rooms with their office names; empty when none.
- [ ] T005 [US1] GREEN: `libs/attendance/infrastructure/ReadModels/Rooms/BookableRoomsReadModel.cs`
  adapter joining the `Offices` + `Rooms` read models (no cross-service join, ADR-0014); register it
  in the infrastructure DI. Integration/read-model test that it returns offices with their rooms.
- [ ] T006 [US1] `Endpoints/RoomCatalogueEndpoints.cs` — `GET /rooms` mapping the query to
  `[{ officeId, officeName, roomId, roomName, capacity }]`, `RequireAuthorization()`, with
  `.WithName/.Produces`; wire `MapRoomCatalogueEndpoints` in `Program.cs`. Host test: an authenticated
  GET returns the catalogue; `401` without a session. Re-emit the OpenAPI spec (T002 gate).
- [ ] T007 [US1] Add the `attendance-rooms` route to `apps/gateway/appsettings.json`
  (`/rooms/{**catch-all}` → `attendance` cluster, `AuthorizationPolicy: default`), mirroring
  `attendance-reservations`.

---

## Phase 3: Data-access lib — `@roomy/attendance-data-access`

- [ ] T008 Generate the lib `libs/attendance/data-access` (`type:data-access`, `context:attendance`)
  with `--unitTestRunner=vitest-analog`; add `ng-openapi-gen.json` (input
  `apps/attendance-api/Roomy.Attendance.Api.json`, output `src/lib/generated`) and the
  `generate-client` target (mirror organization's `project.json`); run it and commit `generated/`.
- [ ] T009 [P] [US1] `bookable-day.ts` — Europe/Berlin working-day + today..today+14 window helper:
  `isBookable(date, today)`, `bookableDaysFrom(today)`. Spec `bookable-day.spec.ts`: rejects past,
  weekend, and >14-day-ahead; accepts in-window weekdays incl. today (scenario 5).
- [ ] T010 [P] [US1/US2] `booking.ts` — `BookableOffice`/`BookableRoom`/`MyReservation` view models,
  `OfficeId`/`RoomId`/`ReservationId` branded ids, and mappings from the generated DTOs (group the
  flat `/rooms` list into offices; mark `MyReservation` upcoming vs past against today). Spec
  `booking.spec.ts`.
- [ ] T011 [US1..US3] `attendance-gateway.ts` — `AttendanceGateway` facade over the generated client:
  `listBookableOffices`, `occupancyForOffice(officeId, day)`, `reserve({officeId, roomId, date})`,
  `myReservations`, `cancel(reservationId, date)`. Spec `attendance-gateway.spec.ts`: each method
  calls the expected generated fn/URL and maps the response/error code (HttpTestingController).

---

## Phase 4: Reserve flow — `reserve-page` (US1, FR-001..FR-004)

- [ ] T012 [US1] RED: `reserve-page.spec.ts` — office step lists offices from the catalogue
  (empty-state when none, scenario edge); room step lists the office's rooms with remaining places for
  the chosen day from occupancy and disables a full room (scenario 3); day step offers only bookable
  days (scenario 5); confirm calls `reserve` and announces success (scenario 1, 2).
- [ ] T013 [US1] GREEN: `reserve/reserve-page.ts/.html/.css` — the office→room→day→confirm flow,
  signal-based + OnPush, reactive forms, `aria-live` success; remaining places via
  `occupancyForOffice`.
- [ ] T014 [US1] Error surfacing (FR-004): map `room_full`, `already_reserved_today`, `not_bookable`,
  `unknown_room` (refresh catalogue), `concurrency_retry_exhausted` (retryable) to localized,
  non-blocking messages; no reservation on rejection (scenarios 3, 4, 5, 10 + edge cases). Extend
  `reserve-page.spec.ts`.

---

## Phase 5: My reservations — `my-reservations-page` (US2, US3, US4)

- [ ] T015 [US2] RED+GREEN: `my-reservations/my-reservations-page.ts/.html/.css` +
  `my-reservations-page.spec.ts` — list mine from `myReservations`, ordered by day, upcoming vs past
  distinguished; empty-state with a link to reserve (scenario 6, edge).
- [ ] T016 [US3] Cancel an upcoming reservation: a cancel action only on upcoming rows; on success
  remove it and announce (scenario 7). No cancel offered for past rows; a `past_immutable` rejection
  surfaces a localized message (scenario 8). Extend the spec.
- [ ] T017 [US4] "Change" affordance on an upcoming row → cancels then routes into `reserve-page`
  (no combined edit step, scenario 9). Spec asserts the navigation + that no single-step edit exists.

---

## Phase 6: Wiring, localization, accessibility (US5, FR-009..FR-011)

- [ ] T018 [US5] `attendance.routes.ts` (guarded by `authGuard` from `@roomy/shared-feature`, NOT
  `adminGuard`) + `src/index.ts` exporting `attendanceRoutes`; lazy-load in
  `apps/web/src/app/app.routes.ts`. Guard spec: unauthenticated → `/bff/login?returnUrl` (scenario 11);
  any signed-in employee is admitted (scenario 12).
- [ ] T019 [US5] Add the nav entry (Attendance / My reservations) in the web shell, shown to any
  signed-in user (not admin-gated); spec the shell shows it for a non-admin session.
- [ ] T020 [US5] `attendance.*` i18n namespace in `apps/web/public/i18n/{en,de}.json` (labels,
  headings, actions, day names, validation + error messages); assert DE/EN key parity (scenario 13,
  FR-010).
- [ ] T021 [US5] WCAG 2.2 AA pass (FR-011): keyboard operability across the reserve flow and the
  my-reservations list, roles/names, visible focus, labelled controls, announced reserve/cancel
  results. Extend specs as needed.

---

## Verify (Definition of Done)

- [ ] `pnpm nx run-many -t test lint -p attendance-data-access attendance-feature web` green.
- [ ] `pnpm nx build web` green.
- [ ] `dotnet build -warnaserror` + `dotnet test` (host endpoint + catalogue read model) +
  `dotnet format --verify-no-changes` green.
- [ ] OpenAPI spec + generated client drift gates green (no diff).
- [ ] Reconcile this `tasks.md` to reality on merge (heed the tasks.md-lag convention).
