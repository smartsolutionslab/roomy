# Research: Attendance Planning (003)

Phase 0 decisions. Each resolves an unknown the plan depends on. Format: Decision /
Rationale / Alternatives considered.

This is the **first event-sourced context** (ADR-0012). The event-store *seam* already
exists (`libs/infrastructure-persistence/src/EventStore`: `IEventStore`, `EfCoreEventStore`,
`EventStoreDbContext`, `EventTypeRegistry`, `StreamId`, `StreamVersion`, `EventEnvelope`,
`EventMetadata`, outbox). What is missing is the **write model on top of that seam** — an
event-sourced aggregate base, an event-sourced repository, and the concurrency-retry policy —
plus the **cross-context capacity feed** from organization. Those are the substance below.

---

## R1 — Event-sourced aggregate base (NEW shared-kernel primitive → ADR-0039)

**Decision.** Add an `EventSourcedAggregate` base to `shared-kernel` alongside the existing
state-based `Aggregate`. It rebuilds from a stream, tracks the loaded version, and collects
uncommitted events:

- `LoadFromHistory(IEnumerable<object> events)` — replays each through `Apply`, advancing
  `Version`.
- `protected abstract void Apply(object @event)` — the reducer; every state change happens
  here and nowhere else (events are the source of truth, ADR-0012).
- `protected void Raise(object @event)` — appends to `UncommittedEvents` **and** calls
  `Apply`, so the in-memory instance reflects the change immediately.
- `Version` (the loaded/last-applied `StreamVersion`) and `IReadOnlyList<object>
  UncommittedEvents`, drained by the repository on save.

It stays framework-free (ADR-0005); it carries `IAggregate` so the architecture tests still
key on it.

**Why a new base, not the existing `Aggregate`.** `Aggregate` records `IDomainEvent` for
*intra-context reactions* but never replays them to reconstruct state — it is the state-based
model identity/organization use. Event sourcing inverts that: state **is** the fold over the
event log. Bolting replay onto `Aggregate` would conflate two persistence models on one type.

**Architectural → ADR.** This is a cross-cutting primitive other event-sourced contexts will
reuse, so it is recorded as **ADR-0039 (Event-sourced write model)** *before* the code
(golden rule 4). ADR-0039 also covers R2 and R3 below (one decision: the write model).

**Alternatives considered.**
- *Keep it local to the attendance domain.* Rejected: occupancy and any future event-sourced
  context need the same base; shared-kernel is where `IAggregate`/`IDomainEvent` already live.
- *Adopt a library (Marten/EventFlow).* Rejected by ADR-0012 (no second persistence library;
  hand-rolled store on the one Postgres).

---

## R2 — Event-sourced repository + optimistic-retry for the last-place race (ADR-0039)

**Decision.** The domain defines the port `IAttendanceDayRepository`
(`LoadAsync(companyId, date)` → `Result<AttendanceDay>`; `SaveAsync(attendanceDay)`); the
infrastructure implementation bridges to `IEventStore`:

- **Load** = `ReadStreamAsync(streamId)` → `AttendanceDay.Rehydrate(replay)`. An empty stream
  yields a fresh aggregate at `StreamVersion.None` (a company-day with no reservations is a
  valid, not-found-as-empty state — so `Load` returns a *new* aggregate, never
  `Error.NotFound`).
- **Save** = `AppendAsync(streamId, expectedVersion: aggregate.Version, uncommittedEvents)`.
  The DB unique constraint on `(stream_id, version)` is the single serialization point
  (ADR-0012).

**Concurrency (scenario 12, FR-007).** The aggregate is the consistency boundary
(ADR-0026); two concurrent reservations for the last place both load version *v*, both try to
append version *v+1*, and the unique constraint lets exactly one win. The loser's
`AppendAsync` throws `EventStoreConcurrencyException`. The **application handler** wraps the
load→decide→save cycle in a **bounded optimistic-retry loop** (default 3 attempts): on
conflict it reloads (now at *v+1*, the room one fuller) and re-evaluates the invariant — so
the loser is correctly rejected as *room full* rather than silently overwriting. Exhausting
retries returns `Error.Conflict` ("please retry"). No locks, no distributed transaction.

**Why retry in the handler, not the repository.** The decision (is the room still full after
the concurrent write?) is a domain question that must re-run against fresh state; only the
use case knows how to re-decide. The repository stays a thin event-store bridge.

**Alternatives considered.** Pessimistic row lock on the stream — rejected (ADR-0012 commits
to optimistic concurrency at the DB; locking reintroduces contention management we don't
need at v1 volume per ADR-0026).

---

## R3 — Room capacity & actor identity: local read models fed by integration events

**Decision (capacity — "full feed now").** The `AttendanceDay` aggregate does **not** store
capacity; it is master data owned by organization (ADR-0014: no cross-service join). The
application handler reads the room's capacity from a **local `Rooms` read model** and passes
it into `attendanceDay.Reserve(employeeId, room, capacity, today)`. The aggregate enforces
*count-for-room < capacity* given the capacity it is handed, keeping it pure and unit-testable
without any organization dependency.

The `Rooms` read model (state-based EF table: `RoomId` PK, `OfficeId`, `CompanyId`,
`Capacity`, names) is maintained by **consuming two NEW organization integration events**:

- `OfficeOpened(OfficeId, CompanyId, Name, Location, OccurredAt)`
- `RoomAdded(RoomId, OfficeId, CompanyId, Name, Capacity, OccurredAt)`

These are added to organization's **published language** (`libs/organization/contracts`,
namespace `SmartSolutionsLab.Roomy.Contracts.Organization`, ADR-0031) and **emitted by the
organization context** when an office/room is created (the publish side lives in 002's
infrastructure — see the dependency note in plan.md). Attendance consumes them at its
infrastructure edge, maps each to an internal command, and updates the read model — wire event
→ internal command at the edge, so `application` never references organization's contracts
(CLAUDE.md).

**Decision (actor → employee, FR-011/FR-012).** Authorization needs the acting user's
`EmployeeId` (for "reserve/cancel my own") and admin status (for "act on behalf"). Attendance
keeps a small **`Employees` read model** (`EmployeeId`, `UserId`) fed by the **already-published**
`EmployeeHired` event. The acting user is identified by the forwarded Keycloak token's subject
(`sub`); the handler resolves `sub → EmployeeId` via this read model. **Admin status comes
from the JWT realm role** (`administrator`), exactly as `identity-api` flattens it today — not
re-derived from `HiredRole`, because elevation can change after hire (identity's
`GrantAdministrator`).

**Why read models, not queries to other services.** ADR-0014 forbids cross-service reads;
the read models are attendance's own, rebuildable from the event feed, and available
synchronously at reservation time.

**Alternatives considered.** Call organization's API for capacity per reservation — rejected
(synchronous cross-service coupling on the hot path; ADR-0014). Derive admin from
`EmployeeHired.Role` — rejected (stale after later elevation).

---

## R4 — Bookable-day policy: clock, timezone, window (domain policy, explicit `today`)

**Decision.** A `BookingWindow` value object in the domain answers `IsBookable(BookingDate
candidate, BookingDate today)` → working day (Mon–Fri) **and** `today <= candidate <= today +
14` (inclusive). The domain stays clock-free: **"today" is computed at the application edge**
from `TimeProvider.GetUtcNow()` converted to the **Europe/Berlin** calendar date
(`TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")`), then passed into the use case. The
aggregate/policy receives `today` explicitly and never reads an ambient clock.

**Why `TimeProvider` at the edge.** It is the established convention (identity handlers inject
`TimeProvider`); it keeps the domain deterministic and the window rules trivially unit-testable
by passing a fixed `today` (covers scenarios 5/6/7 without time travel).

**Alternatives considered.** Inject a clock into the domain — rejected (framework/ambient
dependency in the core, ADR-0005). Store the window length as config — deferred; 14 days is
fixed by FR-002, encoded as a named constant.

---

## R5 — Stream identity & event catalogue

**Decision.** One stream per company-day (ADR-0026). `StreamId` wraps a **`Guid`**
(`StreamId.From(Guid)`), so the company-day stream id is a **deterministic, name-based Guid**
derived from `CompanyId` + `Date` — a UUIDv5-style hash of the string
`attendance-day:{companyId:N}:{date:yyyy-MM-dd}` (a small `AttendanceDayStreamId` helper in the
attendance infrastructure). Deterministic derivation means the same company-day always maps to
the same stream without a lookup table. The stream's events (the source of truth, applied by `AttendanceDay.Apply`):

| Event | Carries | Persisted name |
|---|---|---|
| `ReservationPlaced` | `ReservationId, CompanyId, Date, EmployeeId, OfficeId, RoomId, OccurredAt` | `attendance.reservation-placed.v1` |
| `ReservationCancelled` | `ReservationId, CompanyId, Date, EmployeeId, RoomId, OccurredAt` | `attendance.reservation-cancelled.v1` |

Registered once in an `EventTypeRegistry` at the attendance composition root. Explicit stable
names (not CLR type names) so later renames don't invalidate the log (ADR-0012). These are
**stream/domain events**, internal to the attendance context — not integration events; no
cross-context publish in this slice (the occupancy projection in 004 folds the *same* stream
locally).

**Alternatives considered.** A single `ReservationChanged` event with a kind flag — rejected
(obscures the fold; FR-010 makes place/cancel distinct facts). Publishing reservation events
cross-context now — rejected (occupancy is a local projection in the same service, 004).

---

## R6 — View access (scenario 11) without a projection

**Decision.** For this slice, the read endpoint (`GET` a day's reservations) **replays the
`AttendanceDay` aggregate** and returns its reservations — no separate read model. Any
employee may view; only the owner or an admin may cancel/replace (FR-012). The richer
occupancy rollup and "my reservations" overview are **out of scope (004)**.

**Why replay, not a projection.** v1 volume per company-day is small (ADR-0026); a synchronous
replay is simplest and avoids a projection this slice doesn't otherwise need. 004 introduces
the persistent occupancy projection.

**Alternatives considered.** Build the occupancy projection here — rejected (explicitly 004's
scope; keeps 003 to the reservation write model).

---

## Open follow-ups (recorded, not blocking 003)

- Snapshotting threshold for a busy `AttendanceDay` stream — deferred (ADR-0026 follow-up).
- Event versioning/upcasting approach — not needed until the first event-schema change
  (ADR-0012 follow-up); v1 names carry an explicit `.v1` suffix to make that future cheap.
- Eviction on capacity reduction — out of scope (002 post-MVP, per spec).
