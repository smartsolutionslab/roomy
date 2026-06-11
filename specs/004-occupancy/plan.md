# Implementation Plan: Occupancy Views

**Branch**: `feat/004-occupancy` | **Date**: 2026-06-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-occupancy/spec.md`

## Summary

Employees view how full rooms and offices are so they can plan when and where to come in. This is
the **read side** of the attendance context (US6 lists, US7 calendar, US9 my-reservations): per-room
occupancy (e.g. 3/8) and an office rollup that sums its rooms (e.g. 12/30), as day/week/month lists
and as a calendar, plus an employee's own reservations across all time. For **today and the next
day** the view additionally shows the names of who is booked in each room; every other day shows
counts only (data minimisation). All occupancy views are read-only; past days are viewable as
history; the calendar highlights the viewer's own bookings.

The reservation facts already exist as `ReservationPlaced`/`ReservationCancelled` events in the
event-sourced `AttendanceDay` aggregate (ADR-0026). 004 introduces **materialised read models kept
current by an inline, synchronous projection** committed in the same transaction as the events
(**ADR-0038**) — so a view reflects the latest data the moment it is opened (FR-010) with no async
lag. A single `Reservations` read model serves every query shape; occupancy figures are an indexed
`GROUP BY` joined to the `Rooms` (capacity) and `Offices` (name) read models. No new integration-event
contracts: attendance additionally consumes organization's existing `OfficeOpened` (office name) and
persists the `DisplayName` already carried by `EmployeeHired` (today/tomorrow names). The frontend is
the **first attendance Angular slice** (`libs/attendance/{data-access,ui,feature}`), consuming the
OpenAPI-generated client (ADR-0036). See [research.md](./research.md), [data-model.md](./data-model.md),
[contracts/](./contracts/), [quickstart.md](./quickstart.md).

## Technical Context

**Language/Version**: C# / .NET 10 (backend read side) + TypeScript / Angular 22 (frontend views)
**Primary Dependencies**: EF Core (Npgsql) on PostgreSQL for the read models in attendance's own
`AttendanceDbContext`; the owned event store (`backend/libs/infrastructure-persistence/EventStore`) for the
inline projection seam (ADR-0012/0038); the owned `IQueryHandler`/`Result`/`Ensure` abstractions (no
MediatR, no framework in core); Wolverine inbox for the `OfficeOpened` consumer (ADR-0005/0031);
Angular standalone signal components + NgRx SignalStore facade + `ng-openapi-gen` client (ADR-0016/0019/0036);
Transloco DE+EN + Angular CDK (ADR-0024)
**Storage**: PostgreSQL — three state-based read models in attendance's database (ADR-0014):
`Reservations` (new, per live reservation), `Offices` (new, name), `Employees` (extended with
`DisplayName`); the append-only events table is the source of truth, unchanged
**Testing**: xUnit v3 + Shouldly (projection/query unit + integration against real Postgres via the
sibling Aspire test host); WebApplicationFactory (API/contract); NetArchTest (architecture, projects
already referenced from 003); `vitest-analog` + `@testing-library/angular` (frontend libs),
`@angular/build:unit-test` (app); Playwright e2e deferred to the suite in ADR-0022
**Target Platform**: Linux server (containerised) composed by Aspire; the SPA served via the YARP BFF
**Project Type**: Backend read side (`backend/apps/attendance-api`, `backend/libs/attendance/*`) + frontend feature
slice (`libs/attendance/{data-access,ui,feature}`, routed in `apps/web`)
**Performance Goals**: v1 single-tenant, small per-company-day volume (ADR-0026) — correctness and
read-your-writes consistency (FR-010) first; occupancy figures by indexed `GROUP BY`, no throughput
target; the per-(room,day) counter optimisation is deferred (ADR-0038 Option D)
**Constraints**: read-your-writes consistency (FR-010) — projection commits in the event-append
transaction; data minimisation — names only today + tomorrow (FR-007); read-only views (FR-006); any
authenticated user may view any office/room (FR-005); no cross-service join/DB access (ADR-0014); no
framework in domain/application (ADR-0005); BFF — no tokens in the SPA (ADR-0013); no hardcoded
strings, WCAG 2.2 AA (ADR-0024); Europe/Berlin calendar for "today/tomorrow"; warnings-as-errors; no
suppressions
**Scale/Scope**: 2 read use cases (view occupancy, view my reservations) + 1 new consumer
(`OfficeOpened`) + 1 extended consumer (`EmployeeHired` display name); 1 inline projection over 2
events; 3 read models (1 new + 1 new + 1 extended); 2 read endpoints; 3 frontend pages (list,
calendar, my-reservations) across 3 new Angular libs; 9 acceptance scenarios + 2 edge cases

## Constitution Check

*GATE: must pass before Phase 0 and re-checked after Phase 1.*

| Principle | Status | How this plan satisfies it |
|---|---|---|
| I. Spec-Driven & Test-First | ✅ | Every scenario (1–9) + edges becomes a failing test first (quickstart §1–5); Red→Green→Refactor. Projection tested at unit + integration before wiring; queries before endpoints; FE components before pages. |
| II. Clean Architecture & DDD | ✅ | Read models & projection are **infrastructure**; query use cases & ports are **application** (own `IQueryHandler`); `domain` untouched (read side carries no invariants, ADR-0026/0038). Attendance projects already in `Roomy.ArchitectureTests` (003) so rules stay enforced. FE layers `feature→ui→data-access` (ADR-0035). |
| III. Context Isolation — IDs & Events | ✅ | No cross-service join: office name via `OfficeOpened`, capacity via `Rooms` (already), name via `EmployeeHired`. Wire event → read-model row at the infra edge; attendance consumes only `backend/libs/organization/contracts`. No new published contracts. |
| IV. No Framework in Core | ✅ | Query handlers use owned `IQueryHandler`/`Result`/`TimeProvider`-at-edge only; the inline projector and EF read models are wired at the composition root; `domain`/`application` reference no EF/Wolverine type. |
| V. Decisions Recorded (ADR-before-code) | ✅ | **ADR-0038** (occupancy read side: inline synchronous projection into materialised read models, consistency + retry-safety) authored **before** the projection code. Reuses ADR-0012/0026/0031/0035/0036 otherwise. |
| VI. Green Before Done — No Suppressions | ✅ | Full gate suite in quickstart §DoD (dotnet + nx affected lint/test/build, format, OpenAPI drift gate); no analyzer/test suppression. |
| VII. Small, Single-Purpose Changes | ✅ | One story on `feat/004-occupancy`; atomic Conventional Commits grouped by US (US6 → US9 → US7) and by layer. The deliberately minimal read side — one `Reservations` model + `GROUP BY`, counter table deferred (ADR-0038 Option D) — is the simplest design that serves all four query shapes. |

**Gate result:** PASS. ADR-0038 is authored (Proposed) ahead of the implementing code. No Complexity
Tracking entries — the new state (one read model + two feed extensions) is required by the spec, not
speculative generality.

## Project Structure

### Documentation (this feature)

```text
specs/004-occupancy/
├── plan.md          # this file
├── research.md      # Phase 0 — decisions R1–R7
├── data-model.md    # Phase 1 — read models, projection, query DTOs
├── quickstart.md    # Phase 1 — layered validation guide
├── contracts/       # Phase 1 — attendance-api.md (occupancy + my-reservations)
└── tasks.md         # Phase 2 — /speckit-tasks (NOT created here)
```

### Source Code (repository root)

```text
backend/libs/
  attendance/
    application/                          # Roomy.Attendance.Application (type:application, context:attendance)
      UseCases/
        ViewOccupancy.cs ...Handler.cs    # NEW — per-room + rollup, range, today/tomorrow names policy
        OccupancyView.cs                  # NEW — query DTOs (office rollup, room figures, optional names)
        ViewMyReservations.cs ...Handler.cs  # NEW — actor's reservations across all time
        MyReservationView.cs              # NEW — office, room, day (+ id for cancel link)
      Ports/
        IOccupancyReadModel.cs            # NEW — range query over Reservations joined to Rooms/Offices
        IMyReservationsReadModel.cs       # NEW — by-employee query over Reservations
    infrastructure/                       # Roomy.Attendance.Infrastructure (type:infrastructure)
      ReadModels/
        Reservations/ Reservation.cs ReservationConfiguration.cs   # NEW read model (per live reservation)
        Offices/      Office.cs OfficeConfiguration.cs             # NEW read model (office name)
        Employees/    Employee.cs (+ DisplayName) EmployeeConfiguration.cs  # EXTEND
        OccupancyReadModel.cs MyReservationsReadModel.cs           # NEW — IOccupancy/IMyReservations adapters
      Projections/
        ReservationProjection.cs          # NEW — ReservationPlaced→insert / ReservationCancelled→delete
        IReservationProjection.cs         # NEW — applied by the repository in the save transaction
      Persistence/
        AttendanceDayRepository.cs        # EXTEND — apply projection in SaveAsync; reset tracker on conflict
        AttendanceDbContext.cs            # EXTEND — add Reservations/Offices DbSets + configs
        Migrations/                       # NEW — Reservations, Offices, Employees.DisplayName
      Messaging/
        OfficeOpenedConsumer.cs           # NEW — OfficeOpened → Offices read model
        EmployeeHiredConsumer.cs          # EXTEND — persist DisplayName
      AttendanceInfrastructureServiceCollectionExtensions.cs  # EXTEND — register projection, queries, consumer
    data-access/                          # NEW Roomy.Attendance.DataAccess (type:data-access, context:attendance)
      generated/                          # ng-openapi-gen client from the emitted spec (ADR-0036)
      occupancy.store.ts my-reservations.store.ts  # SignalStore facades (ADR-0019)
    ui/                                   # NEW Roomy.Attendance.Ui (type:ui)
      occupancy-figure/ full-badge/ calendar-cell/ …            # presentational signal components
    feature/                              # NEW Roomy.Attendance.Feature (type:feature)
      occupancy-list/ occupancy-calendar/ my-reservations/      # routed pages (lazy)
backend/apps/
  attendance-api/                         # Roomy.Attendance.Api (type:app)
    Endpoints/OccupancyEndpoints.cs       # NEW — GET /occupancy
    Endpoints/ReservationEndpoints.cs     # EXTEND — GET /reservations/mine
    Program.cs                            # map new endpoints; register OfficeOpened consumer
  gateway/appsettings.json                # ensure /occupancy + /reservations route to the attendance cluster
  web/                                    # register attendance routes (occupancy, calendar, my-reservations)
backend/tests/
  attendance/                             # domain N/A; projection + query unit, integration, API/contract
  attendance-integration/                 # projection-in-transaction, reserve-after-conflict, rebuild (real Postgres)
```

**Structure Decision**: Keep the read side inside the **attendance** context (ADR-0026/0038):
read models + projection in `infrastructure`, query use cases + ports in `application`, `domain`
untouched. Reuse the proven inline-projection seam of the event store (ADR-0012) rather than build
async-projection infrastructure. The frontend follows the `identity` precedent and ADR-0035 layering
(`feature → ui → data-access`), introducing the first attendance Angular libs, tagged
`type:* / context:attendance`, composed by the single `context:web` app.

## Dependency & sequencing note (important)

The read side splits cleanly into independently testable layers; recommended order:

1. **Feeds first (small, unblock display data).** Extend the `Employees` read model + `EmployeeHired`
   consumer to persist `DisplayName`; add the `OfficeOpened` consumer + `Offices` read model. Both are
   in-context consumer changes against organization's **existing** contracts — no producer change.
2. **Projection + `Reservations` read model.** Build `ReservationProjection` and wire it into
   `AttendanceDayRepository.SaveAsync` in the event-append transaction, with change-tracker reset on
   conflict (ADR-0038). Cover with integration tests (reserve, cancel, reserve-after-conflict, rebuild)
   against real Postgres — this is the correctness-critical piece.
3. **Query use cases + endpoints (US6 occupancy, US9 my-reservations).** `ViewOccupancy` (range +
   today/tomorrow name policy) and `ViewMyReservations`; map to `GET /occupancy` and
   `GET /reservations/mine`; emit/extend the OpenAPI spec (ADR-0036 drift gate).
4. **Frontend (US6 list → US9 my-reservations → US7 calendar).** Generate the client into
   `attendance/data-access`, build SignalStore facades, `ui` presentational components, then the three
   routed `feature` pages; Transloco DE+EN, CDK a11y, OnPush signal components. US7 calendar renders
   over the same `/occupancy` figures plus `/reservations/mine` for the own-day highlight (FR-003) — no
   extra endpoint.

Steps 1–3 are backend and gate-checkable without the SPA; step 4 depends on the spec emitted in step 3.

## Complexity Tracking

*No entries — no constitution violations to justify. The read side adds exactly the state the spec
requires: one `Reservations` read model (serves counts, rollup, my-reservations, calendar, names), one
`Offices` read model (rollup naming), and one new column on `Employees` (names). The per-(room,day)
counter table and asynchronous projection are explicitly deferred, not built (ADR-0038 Options D/C).*
