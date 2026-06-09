---
description: "Task list for Occupancy Views (004)"
---

# Tasks: Occupancy Views

**Input**: Design documents in `specs/004-occupancy/` (plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md)

**Tests**: REQUIRED. The constitution (Principle I) and `CLAUDE.md` golden rule 1 mandate test-first — every story phase writes failing tests before implementation (Red → Green → Refactor).

**Organization**: Grouped by backlog user story so each is independently implementable and testable. This is the **read side** of the attendance context — the first **inline projection** (ADR-0038) and the first **attendance Angular** slice (ADR-0035).

## Story label map (priority order)

| Label | Story | Spec scenarios | Priority |
|---|---|---|---|
| US6 | Occupancy as lists (per-room + office rollup, day/week/month) | 1–4, 7–9 | **P1 (MVP)** |
| US9 | View own reservations (past/today/future) | 6 | P2 |
| US7 | Occupancy as a calendar (own days highlighted) | 5 | P3 |

## Format: `[ID] [P?] [Story] Description with file path`

- **[P]**: parallelizable (different files, no incomplete-task dependency).
- Paths follow plan.md: `libs/attendance/{application,infrastructure,data-access,ui,feature}/`, `apps/attendance-api/`, `apps/web/`, `apps/gateway/`, `tests/attendance*/`.

> **No external/producer dependency.** Every feed 004 needs is *already published* by organization (`OfficeOpened`, `RoomAdded`, `EmployeeHired` with `DisplayName`); 004 only consumes more of the existing published language (research R6). The whole read side builds against 003 on `main`.

> **Read-your-writes is the load-bearing invariant (ADR-0038/FR-010).** The Foundational projection (Phase 2) is the correctness-critical piece — it commits in the event-append transaction and must reset the change tracker on conflict. Get Phase 2 green before any query/UI work.

---

## Phase 1: Setup

- [x] T001 **ADR-0038 authored** (`docs/adr/0038-occupancy-read-side-inline-projection.md`, Proposed) — occupancy read side: inline synchronous projection into materialised read models, read-your-writes consistency, retry-safety, counter-table deferred. **Architectural prerequisite — MUST precede the projection code** (golden rule 4). *Done in planning; verify it is on the branch and linked from the ADR index.*
- [ ] T002 [P] Scaffold the three attendance frontend libs — `libs/attendance/data-access` (`@roomy/attendance-data-access`, `type:data-access`), `libs/attendance/ui` (`@roomy/attendance-ui`, `type:ui`), `libs/attendance/feature` (`@roomy/attendance-feature`, `type:feature`) — each tagged `context:attendance`, standalone/zoneless/OnPush, `vitest-analog` + `@testing-library/angular` configured (ADR-0016/0035). Add lazy placeholder routes in `apps/web`. Confirm `pnpm nx affected -t lint` passes the module-boundary rules for the new tags.

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: No user-story work can begin until this phase is complete. This builds the shared read-side spine: the display feeds, the `Reservations` read model, and the inline projection that keeps it current.

### Display feeds (no producer change — consume existing contracts)

- [x] T003 [P] RED integration test in `tests/attendance-integration/` — the extended `EmployeeHiredConsumer` persists `DisplayName` onto the `Employees` read model from a published `EmployeeHired` (real Postgres via the sibling Aspire test host, [[aspire-postgres-integration-tests]]).
- [x] T004 Extend the `Employees` read model with `DisplayName` (`libs/attendance/infrastructure/ReadModels/Employees/Employee.cs` + `EmployeeConfiguration.cs`) and persist it in `EmployeeHiredConsumer.Handle` (set on insert **and** update). T003 green.
- [x] T005 [P] RED integration test — a new `OfficeOpenedConsumer` upserts an `Offices` read-model row `(OfficeId, CompanyId, Name)` from a published `OfficeOpened`; redelivery is idempotent.
- [x] T006 Add the `Offices` read model (`libs/attendance/infrastructure/ReadModels/Offices/Office.cs` + `OfficeConfiguration.cs`), the `OfficeOpenedConsumer` (`libs/attendance/infrastructure/Messaging/`), register the `DbSet` + config in `AttendanceDbContext`, and register the consumer in `apps/attendance-api/Program.cs`. Regenerate the Wolverine static codegen for `attendance-api` and add it to the CI codegen-verify list ([[wolverine-codegen-platform-sensitive]]). T005 green.

### Reservations read model + inline projection

- [x] T007 RED tests in `tests/attendance-integration/ReservationProjectionTests.cs` (Shouldly) — `ReservationPlaced` ⇒ a `Reservations` row for `(ReservationId, CompanyId, EmployeeId, OfficeId, RoomId, Date)`; `ReservationCancelled` ⇒ the row removed; N placed in one room/day ⇒ `COUNT(*) == N`; a cancel decrements; mapping is total over both event types (data-model.md).
- [x] T008 Add the `Reservations` read model (`libs/attendance/infrastructure/ReadModels/Reservations/Reservation.cs` + `ReservationConfiguration.cs` with indexes `(RoomId,Date)`, `(OfficeId,Date)`, `(EmployeeId,Date)`), the `IReservationProjection`/`ReservationProjection` (`libs/attendance/infrastructure/Projections/`), and the `DbSet` + config in `AttendanceDbContext`. T007 green.
- [x] T009 Create the EF Core **migration** in `libs/attendance/infrastructure/Persistence/Migrations/` — create `Reservations` (+ indexes), create `Offices`, add `Employees.DisplayName`. Verify the db-migrator applies it cleanly (ADR-0033).
- [x] T010 Wire the projection into the write transaction — `AttendanceDayRepository.SaveAsync` applies `ReservationProjection` over `attendanceDay.UncommittedEvents` (staged on the shared `AttendanceDbContext`) so the event append's `SaveChanges` commits events **and** read-model rows atomically; on `EventStoreConcurrencyException` it calls `context.ChangeTracker.Clear()` before returning `Error.Conflict` (research R4). Register `IReservationProjection` in `AttendanceInfrastructureServiceCollectionExtensions`.
- [x] T011 **Integration tests** (real Postgres) for the correctness-critical paths (ADR-0038, quickstart §2): **(a) inline atomicity** — after `ReservePlace` succeeds the `Reservations` row is visible in a fresh scope (FR-010), and a forced append failure leaves **no** row; **(b) reserve-after-conflict** (scenario 12) — force a save conflict, let the bounded retry succeed, assert the rows match the committed stream exactly (no leaked losing-attempt row); **(c) cancel** removes the row in the same transaction.
- [ ] T012 [P] *(deferred — ADR-0038 follow-up; the read model functions without it)* Add an offline **rebuild** routine (research R5) — truncate `Reservations` and replay every company-day stream to repopulate it; integration test asserts the rebuilt model is identical to the forward-projected one.
- [x] T013 [P] Confirm the architecture suite stays green — the projection and read models live in `Roomy.Attendance.Infrastructure`; `domain` is untouched; no EF/Wolverine type leaks into `application` (`tests/architecture`).

**Checkpoint**: the `Reservations` read model is materialised and read-your-writes consistent; office/employee display data flows in. Query and UI work can begin.

---

## Phase 3: User Story US6 — Occupancy as lists (Priority: P1) 🎯 MVP

**Goal:** any authenticated user views per-room occupancy (e.g. 3/8) and the office rollup (Σ/Σ, e.g. 12/30) for a day/week/month; today and tomorrow also show who is booked (names), every other day counts only; full rooms are distinguishable; past days are read-only history (FR-001/002/005/006/007/008/009/010).

**Independent test:** drive `ViewOccupancy` over a seeded read model with a fake `TimeProvider` for scenarios 1–4, 8–9; `GET /occupancy` returns the documented shape and error codes; the SPA list page renders the figures.

### Backend query + endpoint

- [ ] T014 [P] [US6] RED application tests in `tests/attendance/Application/ViewOccupancyTests.cs` — room capacity 8 + 3 rows ⇒ `3/8` (scenario 1); rooms Σcap 30 + 12 rows ⇒ rollup `12/30`, a 0-reservation room still counted (scenario 2, edge); each day in `[from,to]` returns its own figure incl. past dates (scenarios 3, 8); `occupied==capacity` ⇒ `isFull` (scenario 9); **names present only for today + tomorrow**, counts only otherwise, pinned via a fake `TimeProvider` Europe/Berlin (scenario 4, FR-007).
- [ ] T015 [US6] Implement `ViewOccupancy` query + `OccupancyView`/`OfficeOccupancy`/`RoomOccupancy` DTOs (`libs/attendance/application/UseCases/`), the `IOccupancyReadModel` port (`.../Ports/`), and the today/tomorrow name policy in the handler (`TimeProvider`, research R7). T014 green.
- [ ] T016 [US6] Implement the `OccupancyReadModel` adapter (`libs/attendance/infrastructure/ReadModels/OccupancyReadModel.cs`) — `GROUP BY` over `Reservations` joined to `Rooms` (capacity/name) and `Offices` (name); include occupant `(employeeId, DisplayName)` rows only for today/tomorrow. Register in DI. Integration test asserts counts/rollup against seeded Postgres.
- [ ] T017 [US6] RED API/contract tests (`tests/attendance/Api/`, WebApplicationFactory) for `GET /occupancy` — `officeId` and `roomId` shapes; `occupants` present only today/tomorrow; `unknown_scope`/`range_too_large` ⇒ 422, `unknown_office`/`unknown_room` ⇒ 404; any authenticated user may view (FR-005), only `GET` exists (FR-006) — per `contracts/attendance-api.md`.
- [ ] T018 [US6] Implement `OccupancyEndpoints.MapOccupancyEndpoints` (`GET /occupancy`) in `apps/attendance-api/Endpoints/`, map `Result`→HTTP, register in `Program.cs`; add the `/occupancy` route to the `attendance` cluster in `apps/gateway/appsettings.json`. Re-emit the OpenAPI spec; the drift gate (ADR-0036) is green. T017 green.

### Frontend (list page)

- [ ] T019 [US6] Generate the typed client from the emitted spec into `libs/attendance/data-access/generated` (`ng-openapi-gen`, ADR-0036) and add an `occupancy` NgRx SignalStore facade exposing the per-day figures as signals (ADR-0019). Store unit test (vitest).
- [ ] T020 [P] [US6] RED + impl presentational `ui` components — `occupancy-figure` (renders `3/8`) and `full-badge` (shown when `isFull`) in `libs/attendance/ui/` (signal `input()`, OnPush); `@testing-library/angular` tests assert the rendered figure and the full state (scenario 9).
- [ ] T021 [US6] RED + impl the `occupancy-list` routed page in `libs/attendance/feature/` — office/room + range picker, one row per day, per-room figures + rollup, occupant names shown only for today/tomorrow; Transloco DE+EN keys (no hardcoded strings), CDK a11y (roles, keyboard); lazy route registered in `apps/web`. Component test covers the list and the name-visibility rule.

**Checkpoint**: MVP — occupancy lists work end-to-end (figures, rollup, full state, names today/tomorrow, past history), read-your-writes consistent.

---

## Phase 4: User Story US9 — View own reservations (Priority: P2)

**Goal:** an employee sees all of their reservations — past, today, and future — each with office, room, and day; future ones can be cancelled, past ones cannot (FR-004, scenario 6).

**Independent test:** `ViewMyReservations` over a seeded read model returns past+future rows with names; `GET /reservations/mine` returns the caller's list; the SPA page lists them with cancel only on future rows.

- [ ] T022 [P] [US9] RED application tests in `tests/attendance/Application/ViewMyReservationsTests.cs` — an employee with past **and** future rows ⇒ all returned, each with `OfficeId/OfficeName/RoomId/RoomName/Date`; another employee's rows excluded; empty list when none.
- [ ] T023 [US9] Implement `ViewMyReservations` query + `MyReservationView` DTO + `IMyReservationsReadModel` port (`libs/attendance/application/UseCases/` + `Ports/`) and the `MyReservationsReadModel` adapter over `Reservations` joined to `Offices`/`Rooms` (`libs/attendance/infrastructure/ReadModels/`); register in DI. T022 green.
- [ ] T024 [US9] RED API/contract tests for `GET /reservations/mine` — returns the caller's reservations (actor resolved from token `sub` via `Employees`); `unknown_employee` ⇒ 404; past rows included as history. Per `contracts/attendance-api.md`.
- [ ] T025 [US9] Add `GET /reservations/mine` to `apps/attendance-api/Endpoints/ReservationEndpoints.cs` (resolve actor like reserve/cancel), map `Result`→HTTP; re-emit the spec (drift gate green). T024 green.
- [ ] T026 [US9] RED + impl the `my-reservations` page — `data-access` store over the generated client; `feature` routed page in `libs/attendance/feature/` listing past+future with office/room/day, **cancel only on future** rows wired to the existing `DELETE /reservations/{id}?date=` (past ⇒ disabled; server returns `past_immutable`); Transloco DE+EN, CDK a11y; route in `apps/web`. Component test covers the past-vs-future cancel rule.

**Checkpoint**: US6 + US9 both work independently — occupancy lists and a personal reservations overview.

---

## Phase 5: User Story US7 — Occupancy as a calendar (Priority: P3)

**Goal:** an employee opens an occupancy calendar where each day shows its occupancy and the days on which they hold a reservation are highlighted (FR-003, scenario 5). No new backend — renders over `GET /occupancy` (figures) + `GET /reservations/mine` (own days).

**Independent test:** the calendar page renders per-day figures from `/occupancy` and highlights the viewer's days by intersecting with `/reservations/mine`; keyboard navigation works.

- [ ] T027 [P] [US7] RED + impl the `calendar-cell` `ui` component in `libs/attendance/ui/` — shows a day's figure (reusing `occupancy-figure`/`full-badge`) and an `isOwnDay`/highlight state (signal `input()`, OnPush, ARIA); `@testing-library/angular` tests assert the figure and the highlight.
- [ ] T028 [US7] RED + impl the `occupancy-calendar` routed page in `libs/attendance/feature/` — month grid over the `occupancy` store for the visible range, own-day highlight by intersecting the `my-reservations` store (research R7), month navigation; Transloco DE+EN, CDK keyboard a11y (grid roles, arrow-key navigation), WCAG 2.2 AA; lazy route in `apps/web`. Component test covers per-day figures + the own-day highlight (scenario 5).

**Checkpoint**: all three views — list, my-reservations, calendar — work independently and share the occupancy/my-reservations stores.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T029 [P] Localization audit — every new string in `ui`/`feature` has DE + EN Transloco keys; no hardcoded text; runtime language switch verified (ADR-0024).
- [ ] T030 [P] Accessibility pass — WCAG 2.2 AA on the list, calendar, and my-reservations pages (CDK roles, focus order, keyboard, colour-independent full/highlight cues); fix gaps.
- [ ] T031 [P] Confirm the OpenAPI drift gate and the `attendance-api` Wolverine codegen-verify are green in CI (new `OfficeOpened` consumer + endpoints), and the full gate suite passes: `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `pnpm nx affected -t lint test build`.
- [ ] T032 Run the `quickstart.md` manual smoke through the gateway (reserve today + next week → occupancy names today only → fill a room → `isFull` → my-reservations → cancel future → count drops immediately) and record the result.
- [ ] T033 [P] Docs — move ADR-0038 toward `Accepted` on merge; update `docs/architecture.md` if the inline-projection pattern is worth surfacing; confirm `CLAUDE.md` active-plan pointer and the build-out roadmap reflect 004.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: ADR confirmed + FE libs scaffolded — no blockers.
- **Foundational (Phase 2)**: depends on Setup; **BLOCKS all stories**. The projection (T007–T011) is the spine; the feeds (T003–T006) unblock names + office rollup display.
- **US6 (Phase 3)**: depends on Phase 2 — the MVP.
- **US9 (Phase 4)**: depends on Phase 2; independent of US6 (shares only the `Reservations` read model and the FE data-access lib).
- **US7 (Phase 5)**: depends on Phase 2 **and** consumes the US6 `occupancy` store + US9 `my-reservations` store on the frontend (renders over both) — schedule after US6 + US9.
- **Polish (Phase 6)**: after the desired stories are complete.

### Within each story

- Tests are written and **fail** before implementation (Red → Green → Refactor).
- Backend: query/port → read-model adapter → endpoint → OpenAPI emit; then frontend: generated client → store → ui → feature page.
- Commit per task or logical group (atomic Conventional Commits).

### Parallel opportunities

- T002 runs alongside the Phase 2 feed tests.
- Within Phase 2: the feed tasks (T003–T006) and the projection tasks (T007–T011) touch different files and can progress in parallel; T012/T013 are `[P]`.
- US6 and US9 backends can be built in parallel by different developers once Phase 2 is green; their FE pages share the data-access lib but live in different files.
- `[P]`-marked UI components (T020, T027) are independent of the routed pages until wired.

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Feeds and projection progress in parallel (different files):
Task: "RED EmployeeHired DisplayName integration test (T003)"
Task: "RED OfficeOpened consumer integration test (T005)"
Task: "RED ReservationProjection unit tests (T007)"
# then their implementations, converging on the migration (T009) and the repository wiring (T010).
```

## Parallel Example: User Story US6

```bash
# After Phase 2 is green:
Task: "RED ViewOccupancy application tests (T014)"
Task: "RED GET /occupancy contract tests (T017)"
# UI components independent of the page:
Task: "occupancy-figure + full-badge ui components (T020)"
```

---

## Implementation Strategy

### MVP first (US6 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (CRITICAL — the projection spine) → 3. Phase 3 US6 →
**STOP & VALIDATE**: occupancy lists work end-to-end, read-your-writes consistent → demo.

### Incremental delivery

1. Setup + Foundational → read model live.
2. US6 → occupancy lists (MVP) → demo.
3. US9 → my reservations → demo.
4. US7 → calendar (over US6 + US9 data) → demo.
5. Polish → localization, a11y, gates, quickstart.

---

## Notes

- [P] = different files, no incomplete-task dependency. [Story] maps a task to a backlog story for traceability.
- The projection (Phase 2) is correctness-critical — verify the reserve-after-conflict integration test (T011b) before relying on the read model.
- No new integration-event contracts: 004 consumes organization's existing `OfficeOpened`/`EmployeeHired` (research R6).
- Verify tests fail before implementing; run the gate suite before "done"; no analyzer/test suppressions.
