# Phase 1 Data Model: Occupancy Views

The 004 read side adds **no domain types** — the `AttendanceDay` aggregate and its
`ReservationPlaced`/`ReservationCancelled` events (003) are the source of truth and are unchanged
(ADR-0026/0038). What follows is the read-side model: the materialised read models, the inline
projection that maintains them, the application query ports/DTOs, and the validation rules each
requirement imposes.

## Read models (infrastructure, `AttendanceDbContext`)

State-based EF Core rows in attendance's own database (ADR-0014). All are a rebuildable cache derived
from the event log and the integration-event feeds (research R5); none carries an invariant.

### `Reservations` — NEW (the projection target)

One row per **live** reservation. Inserted on `ReservationPlaced`, deleted on `ReservationCancelled`
(research R2). Serves occupancy counts, the office rollup, "my reservations", the calendar highlight,
and the today/tomorrow names.

| Column | Type | Notes |
|---|---|---|
| `ReservationId` | `Guid` (PK) | aggregate's `ReservationIdentifier` |
| `CompanyId` | `Guid` | tenant scope (single-tenant v1) |
| `EmployeeId` | `Guid` | who holds it — "my reservations", names, highlight |
| `OfficeId` | `Guid` | rollup grouping |
| `RoomId` | `Guid` | per-room count |
| `Date` | `DateOnly` | the booked day |

Indexes: `(RoomId, Date)` for per-room counts/range; `(OfficeId, Date)` for the rollup;
`(EmployeeId, Date)` for "my reservations" and the highlight. No row exists for a cancelled
reservation, so counts are `COUNT(*)` with no status filter.

### `Offices` — NEW

Office name for the rollup display and office listing. Fed by organization's existing `OfficeOpened`
(research R6).

| Column | Type | Notes |
|---|---|---|
| `OfficeId` | `Guid` (PK) | |
| `CompanyId` | `Guid` | |
| `Name` | `string` | e.g. "Munich" (FR-002 display, scenario 2) |

### `Employees` — EXTEND

Existing read model (003 US4) gains a display name for the today/tomorrow names (FR-007). Fed by the
`DisplayName` already on `EmployeeHired` (research R6).

| Column | Type | Notes |
|---|---|---|
| `EmployeeId` | `Guid` (PK) | existing |
| `UserId` | `Guid` | existing — actor resolution |
| `DisplayName` | `string` | **NEW** — shown for today/tomorrow only |

### `Rooms` — UNCHANGED

Already carries `RoomId`, `OfficeId`, `CompanyId`, `Capacity`, `Name` (003 US2). Supplies the capacity
denominator and the room name. No change.

## Projection (infrastructure)

### `IReservationProjection` / `ReservationProjection` — NEW

A total mapping from the aggregate's uncommitted events to read-model row changes staged on the shared
`AttendanceDbContext`:

- `ReservationPlaced` → add a `Reservations` row `(ReservationId, CompanyId, EmployeeId, OfficeId,
  RoomId, Date)`.
- `ReservationCancelled` → remove the `Reservations` row by `ReservationId`.

It performs no I/O of its own and stages changes only; the **event append's** `SaveChangesAsync`
commits events + rows in one transaction.

### `AttendanceDayRepository.SaveAsync` — EXTEND

```
apply ReservationProjection over attendanceDay.UncommittedEvents   # stage read-model rows
append events (expected version)                                   # same DbContext, one SaveChanges
on EventStoreConcurrencyException:
    context.ChangeTracker.Clear()   # discard staged events + rows; the bounded retry re-projects (R4)
    return Error.Conflict
```

## Application — query ports & use cases

Defined in `application` behind owned `IQueryHandler` abstractions (ADR-0005); implemented in
`infrastructure` over the read models. Queries cannot "not found" — an empty range/employee yields an
empty result, never an error (mirrors `ViewDayReservations`).

### Ports

- `IOccupancyReadModel.GetAsync(scope, from, to, ct)` → per-room rows for the range, joined to
  `Rooms` (capacity, name) and `Offices` (name); for today/tomorrow also the booked employees
  (id + `DisplayName`). `scope` is an office or a single room (FR-001/002/005).
- `IMyReservationsReadModel.GetAsync(employee, ct)` → all of the employee's `Reservations` rows
  (past + today + future), joined to `Offices`/`Rooms` for names (FR-004).

### Use cases

- **`ViewOccupancy(scope, from, to)` → `OccupancyView`** — handler derives "today" from `TimeProvider`
  (Europe/Berlin, research R7), asks the port for the range, computes per-room occupied/capacity and
  the office rollup (Σ occupied / Σ capacity), marks a room **full** when occupied == capacity (FR-008),
  and **includes names only for today and the next day**, counts only otherwise (FR-007).
- **`ViewMyReservations(employee)` → `IReadOnlyList<MyReservationView>`** — the employee resolved from
  the token `sub` via the existing `Employees` read model (003 US4); returns every reservation with
  office, room, and day (FR-004). Cancellability (future vs. past) is the existing 003 rule, surfaced by
  the client, enforced by the existing `DELETE /reservations/{id}`.

### Query DTOs (application)

```
OccupancyView(
  DateOnly Date,
  OfficeOccupancy Office,                 # OfficeId, Name, OccupiedTotal, CapacityTotal, IsFull
  IReadOnlyList<RoomOccupancy> Rooms)     # per room: RoomId, Name, Occupied, Capacity, IsFull,
                                          #           Names? (present only for today/tomorrow)
MyReservationView(Guid ReservationId, Guid OfficeId, string OfficeName,
                  Guid RoomId, string RoomName, DateOnly Date)
```

A range request returns one `OccupancyView` per day in `[from, to]`.

## Validation & policy rules (from requirements)

| Rule | Source | Where enforced |
|---|---|---|
| Occupied counted per (room, day); cancelled frees the place | FR-001/008 | projection (row add/delete) + `COUNT(*)` |
| Office rollup = Σ rooms' occupied / Σ rooms' capacity | FR-002, scenario 2 | `ViewOccupancy` handler |
| Range = each day shows its figure | FR-001, scenario 3 | handler iterates `[from, to]` |
| Names only for today and the next day; counts otherwise | FR-007, scenario 4 | `ViewOccupancy` handler (`TimeProvider`, R7) |
| Room/office distinguishable as **full** (occupied == capacity) | FR-008, scenario 9 | `IsFull` flag in the DTO |
| Any authenticated user may view any office/room | FR-005, scenario 7 | endpoint `RequireAuthorization`, no owner check |
| Views are read-only | FR-006 | only `GET` endpoints added |
| Past days viewable, read-only | FR-009, scenario 8 | range allows past dates; no mutation path |
| Latest data at open time (no live updates) | FR-010, edge | inline projection in the append txn (R2) |
| My reservations = all (past/today/future) with office, room, day | FR-004, scenario 6 | `ViewMyReservations` |
| Calendar highlights the viewer's own days | FR-003, scenario 5 | client intersects `/occupancy` with `/reservations/mine` (R7) |
| 0 reservations → 0 / capacity; rollup excludes nothing | edge | `COUNT(*)` returns 0; rollup sums all rooms |

## Migrations

One EF Core migration in attendance infrastructure: create `Reservations` (+ indexes), create
`Offices`, add `Employees.DisplayName`. The events table is untouched.
