# Implementation Plan: Attendance Planning

**Branch**: `feat/003-attendance` | **Date**: 2026-06-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-attendance/spec.md`

## Summary

Employees reserve a place in a specific room for a single bookable day; the system guarantees
the place and never overbooks a (room, day), and holds each employee to one reservation per
day. This is the **Core** context and the **first event-sourced** one: the consistency boundary
is the `AttendanceDay` aggregate (`CompanyId + Date`, ADR-0026), persisted as an append-only
stream on the existing hand-rolled event store (ADR-0012). Both invariants are enforced inside
one aggregate with a single optimistic-concurrency check per company-day; the last-place race
(scenario 12) is resolved by a bounded retry in the application handler. Room **capacity** and
the acting user's **employee identity** come from organization via integration events into two
local read models — never a cross-service join (ADR-0014). See [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md).

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: EF Core (Npgsql) on PostgreSQL; the owned event store
(`backend/libs/infrastructure-persistence/EventStore`); Wolverine outbox/inbox over RabbitMQ
(ADR-0005/0015); the owned `ICommandHandler`/`Result`/`Ensure` abstractions (no MediatR, no
framework in core)
**Storage**: PostgreSQL — append-only events table (write model) + `Rooms`/`Employees`
state-based read models, attendance's **own** database (ADR-0014)
**Testing**: xUnit v3 + Shouldly (domain/unit, integration against real Postgres via the sibling
Aspire test host), WebApplicationFactory (API/contract), NetArchTest (architecture)
**Target Platform**: Linux server (containerised), composed locally by Aspire
**Project Type**: Backend bounded-context service (`backend/apps/attendance-api`) + `backend/libs/attendance/*`
**Performance Goals**: v1 single-tenant; small booking volume per company-day (ADR-0026) — no
throughput target; correctness/invariants first
**Constraints**: no cross-service join/DB access; domain & application reference no framework;
no tokens in the SPA (BFF, ADR-0013); Europe/Berlin calendar; warnings-as-errors; no suppressions
**Scale/Scope**: 2 use cases (reserve, cancel) + 1 read; 1 event-sourced aggregate; 3 consumers;
2 read models; 13 acceptance scenarios + edge cases

## Constitution Check

*GATE: must pass before Phase 0 and re-checked after Phase 1.*

| Principle | Status | How this plan satisfies it |
|---|---|---|
| I. Spec-Driven & Test-First | ✅ | Every scenario (1–13) + edges becomes a failing test first (quickstart §1–4); Red→Green→Refactor. |
| II. Clean Architecture & DDD | ✅ | `domain`(no deps)→`application`→`infrastructure`→`apps`; behaviour in the `AttendanceDay` aggregate; value objects over primitives; aggregate = consistency boundary (ADR-0026). New context projects added to `Roomy.ArchitectureTests` so the rules aren't vacuous. |
| III. Context Isolation — IDs & Events | ✅ | Capacity/employee learned via `OfficeOpened`/`RoomAdded`/`EmployeeHired`; wire event → internal command at the edge; attendance owns its DB; consumes only `backend/libs/organization/contracts`. |
| IV. No Framework in Core | ✅ | Domain/application use owned `ICommandHandler`/`Result`/`Ensure`/`TimeProvider`-at-edge only; event store & Wolverine wired at the composition root. |
| V. Decisions Recorded (ADR-before-code) | ⚠️→✅ | **ADR-0039 (event-sourced write model: aggregate base + repository + optimistic-retry)** MUST be authored before the write-model code — it is task #1. Reuses ADR-0012/0026 otherwise. |
| VI. Green Before Done — No Suppressions | ✅ | Full gate suite in quickstart §DoD; no analyzer/test suppression. |
| VII. Small, Single-Purpose Changes | ✅ | One story on `feat/003-attendance`; atomic Conventional Commits; per-task or per-logical-group. |

**Gate result:** PASS, conditional on ADR-0039 landing before the write-model implementation
(tracked as the first task). No unjustified complexity — see no Complexity Tracking entries.

## Project Structure

### Documentation (this feature)

```text
specs/003-attendance/
├── plan.md          # this file
├── research.md      # Phase 0 — decisions R1–R6
├── data-model.md    # Phase 1 — AttendanceDay aggregate, events, read models, VOs
├── quickstart.md    # Phase 1 — layered validation guide
├── contracts/       # Phase 1 — attendance-api.md, integration-events.md
└── tasks.md         # Phase 2 — /speckit-tasks (NOT created here)
```

### Source Code (repository root)

```text
backend/libs/
  shared-kernel/src/
    EventSourcedAggregate.cs            # NEW (ADR-0039) — replay/Apply/Raise/Version base
  attendance/
    domain/                             # Roomy.Attendance.Domain  (type:domain, context:attendance)
      AttendanceDays/
        AttendanceDay.cs                # event-sourced aggregate root
        Reservation.cs                  # entity (replay-only)
        ReservationPlaced.cs            # stream event
        ReservationCancelled.cs         # stream event
        BookingWindow.cs                # bookable-day policy VO
        BookingDate.cs RoomCapacity.cs RoomReference.cs
        CompanyIdentifier.cs EmployeeIdentifier.cs OfficeIdentifier.cs
        RoomIdentifier.cs ReservationIdentifier.cs
        IAttendanceDayRepository.cs
    application/                        # Roomy.Attendance.Application (type:application)
      UseCases/
        ReservePlace.cs ReservePlaceHandler.cs       # incl. optimistic-retry loop (R2)
        CancelReservation.cs CancelReservationHandler.cs
        ViewDayReservations.cs ...Handler.cs          # replay read (R6)
      Ports/ IRoomDirectory.cs IEmployeeDirectory.cs
    infrastructure/                     # Roomy.Attendance.Infrastructure (type:infrastructure)
      Persistence/
        AttendanceDbContext.cs          # : EventStoreDbContext
        AttendanceDayRepository.cs      # IEventStore bridge + AttendanceDayStreamId
        ReadModels/ RoomsConfiguration.cs RoomDirectory.cs EmployeesConfiguration.cs EmployeeDirectory.cs
        Migrations/
        AttendanceEventTypeRegistry.cs  # registers the 2 stream events
      Messaging/
        RoomAddedConsumer.cs OfficeOpenedConsumer.cs EmployeeHiredConsumer.cs
      AttendanceInfrastructureServiceCollectionExtensions.cs
  organization/contracts/               # NEW events (organization's published language)
    OfficeOpened.cs RoomAdded.cs        # + publish wired in 002 infra (PR #113) — see Dependency
backend/apps/
  attendance-api/                       # Roomy.Attendance.Api (type:app, context:attendance)
    Program.cs Endpoints/ ...           # mirrors backend/apps/identity-api
  gateway/appsettings.json              # NEW /attendance route (cluster attendance)
  apphost/                              # register attendance-api + its DB (Aspire)
backend/tests/
  architecture/Roomy.ArchitectureTests/ # ADD ProjectReferences to the 3 attendance projects
  attendance/                           # domain/integration/api tests (mirrors backend/tests/identity)
```

**Structure Decision**: Mirror the proven `identity` context layout — `domain`/`application`/
`infrastructure` libs under `backend/libs/attendance/` + an `backend/apps/attendance-api` host — tagged
`type:* / context:attendance`. The single new cross-cutting primitive
(`EventSourcedAggregate`) lives in `shared-kernel` beside `Aggregate`/`IAggregate`. The two new
organization contracts live in `backend/libs/organization/contracts` (organization's published language),
consumed by attendance.

## Dependency & sequencing note (important)

"Full feed now" (chosen) means the capacity invariant is fed by **real** organization events.
The producer side (`OfficeOpened`/`RoomAdded` emitted by organization's create handlers) lives
in the organization context, whose Office/Room domain is in **PR #113** (green, not yet on
`main`). Therefore:

1. **Merge PR #113 first** (your call), then rebase `feat/003-attendance` on `main`.
2. Add `OfficeOpened`/`RoomAdded` to `backend/libs/organization/contracts` and the publish in
   organization's create-office / add-room handlers (a small 002 addition).
3. Build the attendance consumers + read models against those contracts.

The attendance **write model** (aggregate, event store, reserve/cancel invariants) does **not**
depend on organization and can be built test-first in parallel with steps 1–2, since the domain
tests pass `capacity` explicitly (research R3). This is the natural backend split if work is
parallelised.

## Complexity Tracking

*No entries — no constitution violations to justify. The one new shared primitive
(`EventSourcedAggregate`) and the optimistic-retry are required by event sourcing (ADR-0012) and
the last-place race (FR-007/scenario 12), not optional generality.*
