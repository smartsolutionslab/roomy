# Data Model: Attendance Planning (003)

Phase 1 model for the **attendance** context. Organised **by aggregate** (CLAUDE.md): a folder
+ namespace per aggregate holds the root, its value objects, its events, and its repository
interface. Root namespace `SmartSolutionsLab.Roomy.Attendance`.

This context is **event-sourced** (ADR-0012/0026): the `AttendanceDay` stream is the source of
truth; the two read models (`Rooms`, `Employees`) are state-based projections fed by integration
events, not part of the write model's consistency boundary.

---

## Aggregate: `AttendanceDay` (event-sourced) — `AttendanceDays/`

**Identity:** `CompanyIdentifier` + `BookingDate` (ADR-0026). One instance per company calendar
day; the consistency boundary for *no-overbooking* and *one-reservation-per-employee-per-day*.
**Stream id:** deterministic name-based `Guid` from `(CompanyId, Date)` (research R5).

**State (the fold over the stream):**

| Field | Type | Notes |
|---|---|---|
| `CompanyId` | `CompanyIdentifier` | stream identity part 1 |
| `Date` | `BookingDate` | stream identity part 2 (Europe/Berlin calendar day) |
| `Reservations` | `IReadOnlyCollection<Reservation>` | rebuilt by `Apply` |
| `Version` | `StreamVersion` | from `EventSourcedAggregate`; the optimistic-concurrency token |

**Behaviour (the only mutators; each `Raise`s an event applied through `Apply`):**

- `static AttendanceDay Rehydrate(IEnumerable<object> streamEvents)` — replays via the base
  `LoadFromHistory`; an empty stream yields a fresh, zero-reservation day.
- `Result Reserve(EmployeeIdentifier employee, RoomReference room, RoomCapacity capacity,
  BookingDate today)` — enforces, in order:
  1. `BookingWindow.IsBookable(Date, today)` else `Error.Validation` (`not_bookable` — covers
     past / weekend / beyond-window, FR-002/FR-006, scenarios 5–7).
  2. employee holds **no** reservation this day else `Error.Conflict`
     (`already_reserved_today`, FR-005, scenario 4).
  3. count of reservations **for `room`** `< capacity` else `Error.Conflict` (`room_full`,
     FR-004/FR-007, scenario 3).
  On success raises `ReservationPlaced` (FR-001/FR-003, scenarios 1–2).
- `Result Cancel(ReservationIdentifier reservation, EmployeeIdentifier actor, bool actorIsAdmin,
  BookingDate today)` — the reservation must exist (`Error.NotFound`), its `Date` must not be
  past (`Error.Validation` `past_immutable`, FR-009, scenario edge), and the actor must be the
  owner **or** an admin (`Error.Forbidden` `not_owner`, FR-012, scenario 11). On success raises
  `ReservationCancelled` (FR-008, scenarios 8–9).

> The aggregate is handed `capacity` and `today` — it never reads master data or a clock
> (research R3/R4). "Reserve a different room/office/day" (FR-010, scenario 13) is *cancel then
> reserve*; there is no combined edit method.

**Stream events** (`IDomainEvent`-free plain records; the event-sourced fold, research R5):

| Event | Fields |
|---|---|
| `ReservationPlaced` | `ReservationId, CompanyId, Date, EmployeeId, OfficeId, RoomId, OccurredAt` |
| `ReservationCancelled` | `ReservationId, CompanyId, Date, EmployeeId, RoomId, OccurredAt` |

Registered with stable names `attendance.reservation-placed.v1` /
`attendance.reservation-cancelled.v1` in the context's `EventTypeRegistry`.

### Entity: `Reservation` (inside the aggregate)

| Field | Type | Notes |
|---|---|---|
| `Id` | `ReservationIdentifier` | GUIDv7 branded id |
| `Employee` | `EmployeeIdentifier` | owner; unique per (day) within the aggregate |
| `Office` | `OfficeIdentifier` | denormalised for view/cancel |
| `Room` | `RoomIdentifier` | the (room, day) the capacity rule counts |

`Reservation` is an `IEntity`, never persisted directly — it exists only as the replay result
of the stream.

### Value objects (`AttendanceDays/`)

| Type | Rule (enforced with `Ensure.That(...)`) |
|---|---|
| `CompanyIdentifier` | GUIDv7 branded `…Identifier`, non-empty; implicit `Guid` conversion (EF) |
| `EmployeeIdentifier` | as above — attendance's own id for organization's `Employee` (by ID, ADR-0014) |
| `OfficeIdentifier` | as above — for organization's `Office` |
| `RoomIdentifier` | as above — for organization's `Room` |
| `ReservationIdentifier` | GUIDv7, generated on place |
| `RoomReference` | pairs `RoomIdentifier` + `OfficeIdentifier` (a reservation targets a room *in* an office) |
| `RoomCapacity` | `int >= 1`; the count ceiling the aggregate enforces |
| `BookingDate` | a `DateOnly` in the Europe/Berlin calendar; the bookable unit |
| `BookingWindow` | policy VO: `IsBookable(BookingDate candidate, BookingDate today)` = Mon–Fri **and** `today <= candidate <= today.AddDays(14)` (FR-002) |

**Repository port** (`AttendanceDays/IAttendanceDayRepository.cs`, domain):

```
Task<Result<AttendanceDay>> LoadAsync(CompanyIdentifier company, BookingDate date, CancellationToken ct);
Task SaveAsync(AttendanceDay attendanceDay, CancellationToken ct);   // append uncommitted at expected Version
```

`LoadAsync` returns a fresh aggregate for an empty stream (never `Error.NotFound`); a genuine
miss is not a concept here — a company-day always exists conceptually (research R2).

---

## Read model: `Rooms` (state-based projection) — `ReadModels/Rooms/`

Capacity master data mirrored from organization (research R3). EF table, owned by attendance.

| Column | Type | Source event |
|---|---|---|
| `RoomId` (PK) | `Guid` | `RoomAdded.RoomId` |
| `OfficeId` | `Guid` | `RoomAdded.OfficeId` |
| `CompanyId` | `Guid` | `RoomAdded.CompanyId` |
| `Capacity` | `int` | `RoomAdded.Capacity` |
| `RoomName` | `string` | `RoomAdded.Name` |
| `OfficeName` | `string` | `OfficeOpened.Name` (joined by `OfficeId`) |

**Port** (application): `IRoomDirectory.FindAsync(RoomIdentifier, CancellationToken)` →
`Result<RoomCapacityView>` (`Error.NotFound` `unknown_room` if the room isn't known yet —
e.g. event not yet consumed). Updated by `RoomAddedConsumer` / `OfficeOpenedConsumer` at the
infrastructure edge (wire event → internal command).

## Read model: `Employees` (state-based projection) — `ReadModels/Employees/`

Actor→employee resolution for authorization (research R3).

| Column | Type | Source event |
|---|---|---|
| `EmployeeId` (PK) | `Guid` | `EmployeeHired.EmployeeId` |
| `UserId` | `Guid` | `EmployeeHired.UserId` |

**Port** (application): `IEmployeeDirectory.FindByUserAsync(UserIdentifier, CancellationToken)`
→ `Result<EmployeeIdentifier>` (`Error.NotFound` `unknown_employee`). Updated by
`EmployeeHiredConsumer`.

---

## Error catalogue (domain → HTTP, see contracts/)

| Code | `ErrorType` | HTTP | Requirement |
|---|---|---|---|
| `not_bookable` | Validation | 422 | FR-002/FR-006 (scenarios 5–7) |
| `already_reserved_today` | Conflict | 409 | FR-005 (scenario 4) |
| `room_full` | Conflict | 409 | FR-004/FR-007 (scenarios 3, 12) |
| `past_immutable` | Validation | 422 | FR-009 (cancel edge) |
| `not_owner` | Forbidden | 403 | FR-012 (scenario 11) |
| `unknown_room` / `unknown_employee` | NotFound | 404 | read-model miss |
| `concurrency_retry_exhausted` | Conflict | 409 | FR-007 (scenario 12 fallback) |

---

## Cross-context dependencies (by ID + integration events only)

- **Consumes** organization's published language: `OfficeOpened`, `RoomAdded` (NEW),
  `EmployeeHired` (existing) — `libs/organization/contracts`, mapped to internal commands at
  the infrastructure edge (ADR-0031).
- **Publishes** nothing in this slice — reservation events stay internal to the attendance
  context; the occupancy projection (004) folds the same stream locally.
