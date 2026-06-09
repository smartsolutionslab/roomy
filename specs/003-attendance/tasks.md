---
description: "Task list for Attendance Planning (003)"
---

# Tasks: Attendance Planning

**Input**: Design documents in `specs/003-attendance/` (plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md)

**Tests**: REQUIRED. The constitution (Principle I) and `CLAUDE.md` golden rule 1 mandate test-first — every story phase writes failing tests before implementation (Red → Green → Refactor).

**Organization**: Grouped by user story so each is independently implementable and testable. This is the **Core**, **first event-sourced** context (ADR-0012/0026).

## Story label map (priority order)

| Label | Story | Spec scenarios | Priority |
|---|---|---|---|
| US1 | Reserve a place (write model) | 1–7, 12, 13 | **P1 (MVP)** |
| US2 | Capacity feed from organization | (enables 3, 12 e2e) | P2 |
| US3 | Cancel a reservation | 8, 9, past-immutable edge | P2 |
| US4 | Authorization — admin on-behalf + owner-only | 10, 11 | P2 |
| US5 | View a day's reservations | 11 (view) | P3 |

## Format: `[ID] [P?] [Story] Description with file path`

- **[P]**: parallelizable (different files, no incomplete-task dependency).
- Paths follow plan.md: `libs/attendance/{domain,application,infrastructure}/`, `apps/attendance-api/`, `libs/shared-kernel/`, `libs/organization/contracts/`, `tests/attendance/`.

> **External dependency (US2 only):** the capacity feed needs organization to **emit** `OfficeOpened`/`RoomAdded`, which extends 002's Office/Room domain — still in **PR #113** (not on `main`). **US1 (MVP) does NOT depend on this**: its domain/handler tests pass `capacity` explicitly through the `IRoomDirectory` port (research R3), so the write model can be built in parallel while #113 merges. Recommended order: merge #113 → rebase `feat/003-attendance` → do US2.

---

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 **Author ADR-0036** in `docs/adr/0036-event-sourced-write-model.md` — the event-sourced write model: the `EventSourcedAggregate` base (replay/`Apply`/`Raise`/`Version`), the event-sourced repository pattern over `IEventStore`, and the **bounded optimistic-retry** policy for the last-place race (FR-007/scenario 12; ADR-0026 follow-up). **Architectural prerequisite — MUST land before any write-model code** (golden rule 4). Link from ADR-0012/0026.
- [ ] T002 Create the attendance context structure — `libs/attendance/{domain,application,infrastructure}`, host `apps/attendance-api`, test project `tests/attendance` — added to `Roomy.slnx`; per-project root namespaces `SmartSolutionsLab.Roomy.Attendance.*`; inherits `Directory.Build.props` (net10.0, nullable on, warnings-as-errors). **Add the three attendance `domain`/`application`/`infrastructure` projects as `ProjectReference`s to `tests/architecture/Roomy.ArchitectureTests`** (CLAUDE.md — otherwise the rules pass vacuously).
- [ ] T003 [P] Tag the new projects `context:attendance` + their `type:*` layer; confirm the .NET architecture suite now loads/inspects the `Roomy.Attendance.*` assemblies (the boundary enforcement for backend; Nx/eslint boundaries cover frontend only).

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: No user-story work can begin until this phase is complete. This builds the story-agnostic write-model machinery.

- [ ] T004 [P] Confirm the architecture rules actively enforce attendance: `LayerDependencyConventionTests` + `NoMediatRTests` inspect `Roomy.Attendance.{Domain,Application,Infrastructure}` — domain depends only on `shared-kernel`, application only on domain, no framework/MediatR in core.
- [ ] T005 [P] RED unit tests in `tests/attendance/Domain/ValueObjects/` (Shouldly) — `CompanyIdentifier`, `EmployeeIdentifier`, `OfficeIdentifier`, `RoomIdentifier`, `ReservationIdentifier` (GUIDv7 branded, non-empty, implicit `Guid`), `BookingDate`, `RoomCapacity` (≥ 1), `RoomReference`, and the `BookingWindow.IsBookable(candidate, today)` truth table (Mon–Fri ∧ `today ≤ candidate ≤ today+14`).
- [ ] T006 [P] Implement those value objects + `BookingWindow` in `libs/attendance/domain/AttendanceDays/` (invariants via `Ensure.That(...)`). T005 green.
- [ ] T007 [P] RED unit tests in `tests/shared-kernel/` for the `EventSourcedAggregate` base — `LoadFromHistory` replays events through `Apply` and advances `Version`; `Raise` applies **and** collects into `UncommittedEvents`; a fresh instance is at `StreamVersion.None`.
- [ ] T008 Implement `EventSourcedAggregate` in `libs/shared-kernel/src/EventSourcedAggregate.cs` (carries `IAggregate`; framework-free, ADR-0036). T007 green.
- [ ] T009 RED→green: the two stream events `ReservationPlaced`/`ReservationCancelled` in `libs/attendance/domain/AttendanceDays/`, and `AttendanceEventTypeRegistry` in `libs/attendance/infrastructure/Persistence/` registering them as `attendance.reservation-placed.v1` / `attendance.reservation-cancelled.v1`. Test: round-trips each event through the serializer by stable name (serialize → deserialize equals original).
- [ ] T010 `AttendanceDbContext : EventStoreDbContext` + the deterministic `AttendanceDayStreamId` (name-based `Guid` from `CompanyId`+`Date`, research R5) + the events-table **migration**, in `libs/attendance/infrastructure/Persistence/`. **Integration test** (real Postgres via a sibling Aspire test app host, per [[aspire-postgres-integration-tests]]): `IEventStore` append→read round-trips a stream and a conflicting `expectedVersion` surfaces `EventStoreConcurrencyException`.

**Checkpoint**: write-model machinery (VOs, aggregate base, event registry, event-store-backed context) ready — user stories can begin.

---

## Phase 3: User Story 1 — Reserve a place (Priority: P1) 🎯 MVP

**Goal:** an employee reserves a place in a room for a bookable day; the system guarantees it and never overbooks (FR-001..007), holding one reservation per employee per day, race-safe (scenario 12).

**Independent test:** drive `AttendanceDay.Reserve` with explicit `capacity`/`today` for scenarios 1–7; exercise the handler's retry against a fake event store for scenario 12; POST `/reservations` returns the documented status/`code` for each outcome — **all without organization** (capacity via a stubbed `IRoomDirectory`).

- [ ] T011 [US1] RED domain tests in `tests/attendance/Domain/AttendanceDayTests.cs` — `Reserve` raises `ReservationPlaced` on success (1–2); rejects `room_full` at capacity (3), `already_reserved_today` for a second same-day reservation (4), `not_bookable` for past/weekend/beyond-window (5–7). Capacity and `today` passed explicitly.
- [ ] T012 [US1] Implement the `AttendanceDay` aggregate in `libs/attendance/domain/AttendanceDays/AttendanceDay.cs` (`: EventSourcedAggregate`) + the `Reservation` entity + `Apply(ReservationPlaced)` + `Rehydrate`. `Reserve(employee, room, capacity, today)` enforces the rules in order (data-model.md). T011 green.
- [ ] T013 [US1] RED application tests in `tests/attendance/Application/ReservePlaceHandlerTests.cs` — the handler loads via `IAttendanceDayRepository`, reads capacity via `IRoomDirectory` (stubbed), saves, and on `EventStoreConcurrencyException` **retries** (reload → re-decide), correctly rejecting the loser as `room_full` (scenario 12) and returning `concurrency_retry_exhausted` after the bound. Define `ReservePlace` command + `IRoomDirectory` port.
- [ ] T014 [US1] Implement `ReservePlaceHandler` (bounded optimistic-retry, research R2) in `libs/attendance/application/UseCases/`, and `AttendanceDayRepository` in `libs/attendance/infrastructure/Persistence/` (load = `ReadStreamAsync`→`Rehydrate`; save = `AppendAsync(streamId, Version, uncommitted)`). T013 green. **Integration test** (real Postgres): two concurrent `Reserve` calls on a capacity-1 room → exactly one `ReservationPlaced`, the other `room_full`, `(stream_id, version)` never violated (FR-007/scenario 12).
- [ ] T015 [US1] `POST /reservations` endpoint + `apps/attendance-api/Program.cs` host (mirror `apps/identity-api`: JWT bearer against the realm, `AddRoomyMessaging`, event-store DI, `RunJasperFxCommands`) + the `/attendance/{**}` gateway route in `apps/gateway/appsettings.json` + register `attendance-api` and its DB in `apps/apphost`. API/contract tests assert `Error`→HTTP mapping (201/409 `room_full`/409 `already_reserved_today`/422 `not_bookable`) per `contracts/attendance-api.md`.

**Checkpoint**: MVP — reserving works end-to-end against a seeded/stubbed room directory; all reservation invariants green.

---

## Phase 4: User Story 2 — Capacity feed from organization (Priority: P2)

**Goal:** attendance learns real room capacity from organization via integration events into a local `Rooms` read model (research R3) — no cross-service join (ADR-0014).

**Independent test:** publish `OfficeOpened` + `RoomAdded` → the `Rooms` read model upserts → `IRoomDirectory.FindAsync` returns the capacity the reserve handler enforces.

> **Depends on PR #113** (organization Office/Room domain). Merge it and rebase before T016.

- [ ] T016 [US2] Add `OfficeOpened(OfficeId, CompanyId, Name, Location, OccurredAt)` and `RoomAdded(RoomId, OfficeId, CompanyId, Name, Capacity, OccurredAt)` to `libs/organization/contracts/` (namespace `SmartSolutionsLab.Roomy.Contracts.Organization`, IDs/primitives only, ADR-0031), and **publish** them from organization's create-office / add-room handlers over the Wolverine outbox. Test: creating an office/room enqueues the event.
- [ ] T017 [US2] RED tests in `tests/attendance/Infrastructure/` — `RoomAddedConsumer`/`OfficeOpenedConsumer` upsert the `Rooms` read model (wire event → internal command at the edge); `RoomDirectory.FindAsync` maps a row to `RoomCapacityView` and returns `unknown_room` (`NotFound`) for an unknown room.
- [ ] T018 [US2] Implement the `Rooms` read model (EF config + migration) + `RoomDirectory` + the two consumers in `libs/attendance/infrastructure/`. T017 green. **Integration test** (real Postgres + published event): the consumer materializes capacity that the reserve flow then enforces (scenario 3 e2e).

**Checkpoint**: real capacity feed live; US1's room-full rule holds against organization data end-to-end.

---

## Phase 5: User Story 3 — Cancel a reservation (Priority: P2)

**Goal:** an employee cancels a reservation (today/future), freeing the place (FR-008/009).

**Independent test:** `AttendanceDay.Cancel` frees a place so a previously full room is re-bookable (9); cancelling a past-day reservation is rejected (FR-009).

- [x] T019 [US3] RED domain tests — `Cancel` raises `ReservationCancelled` for a today/future reservation; the freed place makes a full room re-bookable (8–9); a past-day cancel is `past_immutable` (FR-009 edge).
- [x] T020 [US3] Add `Cancel(reservation, actor, actorIsAdmin, today)` + `Apply(ReservationCancelled)` to `AttendanceDay`. T019 green. (Owner/admin gate is wired in US4; here Cancel takes the flags.)
- [x] T021 [US3] RED application tests for `CancelReservationHandler` — loads, cancels, saves; a freed place is immediately re-bookable (re-run Reserve succeeds). Define `CancelReservation` command.
- [x] T022 [US3] Implement `CancelReservationHandler` + `DELETE /reservations/{reservationId}?date=` endpoint (date carried because the stream is keyed by company-day). T021 green. API/contract tests: 204 / 403 `not_owner` / 404.

**Checkpoint**: cancel works; freed places re-bookable (scenario 9).

---

## Phase 6: User Story 4 — Authorization: admin on-behalf + owner-only (Priority: P2)

**Goal:** an administrator reserves/cancels for anyone; an employee acts only on their own reservation and may otherwise only view (FR-011/012, scenarios 10–11).

**Independent test:** with a resolved actor, `onBehalfOf` is admin-only on reserve (403 for a non-admin targeting another); cancel is owner-or-admin (403 `not_owner` otherwise).

- [ ] T023 [US4] RED tests — `EmployeeHiredConsumer` upserts the `Employees` read model (`EmployeeId`↔`UserId`); `IEmployeeDirectory.FindByUserAsync(sub)` returns the `EmployeeId` (`unknown_employee` `NotFound` on miss).
- [ ] T024 [US4] Implement the `Employees` read model (EF config + migration) + `EmployeeDirectory` + `EmployeeHiredConsumer` in `libs/attendance/infrastructure/`. T023 green. (Consumes the **existing** `EmployeeHired` — no organization change needed.)
- [ ] T025 [US4] RED tests — the actor is resolved from the token `sub` via `IEmployeeDirectory`; admin status from the JWT `administrator` realm role (as `identity-api` flattens it). Reserve: non-admin with `onBehalfOf` ≠ self → 403 (FR-011); admin on-behalf → 201 (scenario 10). Cancel: non-owner non-admin → 403 `not_owner` (scenario 11); admin → 204.
- [ ] T026 [US4] Wire actor resolution + admin flag into `ReservePlaceHandler`/`CancelReservationHandler` and the endpoints (the API passes the resolved `EmployeeIdentifier` + `actorIsAdmin`). T025 green. API tests cover 10–11.

**Checkpoint**: admins act on behalf of anyone; employees are confined to their own reservations.

---

## Phase 7: User Story 5 — View a day's reservations (Priority: P3)

**Goal:** any authenticated employee can view a company-day's reservations (scenario 11, view part).

**Independent test:** `GET /reservations?date=…` returns the day's rows (or empty) by replaying the `AttendanceDay` stream (research R6).

- [ ] T027 [US5] RED API test — `GET /reservations?date=` returns the day's reservations for any authenticated user; empty array for a day with none.
- [ ] T028 [US5] Implement `ViewDayReservations` query + handler (replay via the repository) + the `GET /reservations` endpoint. T027 green.

**Checkpoint**: all user stories independently functional.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [ ] T029 [P] OpenAPI document for `attendance-api` + the typed Angular client generated from it (mirror organization 002 T024 / ADR-0018), exposed through the gateway.
- [ ] T030 [P] Full gate sweep on affected projects — `dotnet build -warnaserror`, `dotnet test` (unit + integration + architecture), `dotnet format --verify-no-changes`, `pnpm nx affected -t lint test build`. No suppressions/skips. Update the `CLAUDE.md` context table row for attendance if any naming drifted.
- [ ] T031 [P] Run the `quickstart.md` manual smoke through the gateway (reserve → fill → 409 → cancel → re-book → weekend 422).

---

## Dependencies & Execution Order

### Phase dependencies
- **Setup (P1)** → **Foundational (P2)** → user stories. T001 (ADR-0036) blocks the write-model code (T008+).
- **US1 (P1)**: after Foundational. **No organization dependency** — testable with a stubbed `IRoomDirectory`.
- **US2 (P2)**: after Foundational **and PR #113**. Independent of US1's aggregate code; implements the `IRoomDirectory` port US1 defines (T013).
- **US3 (P2)**: after US1 (reuses the aggregate + repository).
- **US4 (P2)**: after US1 (gates reserve) and US3 (gates cancel); consumes existing `EmployeeHired` (no #113 dependency).
- **US5 (P3)**: after US1 (replay via the repository).
- **Polish (P8)**: after the desired stories.

### Parallel opportunities
- **Across teams/sessions:** the **write model (US1)** and the **capacity feed (US2)** are the natural BE split — US1 proceeds immediately; US2 waits on #113. US4's `Employees` feed (T023–T024) is also #113-independent and can overlap US1.
- **Within a phase:** all `[P]` tasks (different files) — e.g. Foundational T005/T007 (tests) and the VO/base implementations.

## Implementation Strategy

### MVP first (US1 only)
1. Setup (incl. **ADR-0036**) → 2. Foundational → 3. US1 → **STOP & VALIDATE**: reservations + all invariants green against a stubbed room directory (scenarios 1–7, 12).

### Incremental delivery
US1 (MVP) → US2 (real capacity, after #113) → US3 (cancel) → US4 (authorization) → US5 (view) → Polish. Each story is independently testable at its checkpoint.
