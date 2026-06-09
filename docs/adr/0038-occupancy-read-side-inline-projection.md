# 0038. Occupancy read side: inline synchronous projection into materialized read models

- **Status:** Proposed
- **Date:** 2026-06-09
- **Deciders:** Heiko Weiß

## Context and problem statement

The occupancy feature (`004-occupancy`) is a **read side** over the attendance context's
event-sourced `AttendanceDay` aggregate (ADR-0026). It must answer four query shapes for any
authenticated user (FR-001..009):

1. per-room occupancy (occupied / capacity, e.g. 3/8) and the office rollup (Σ occupied / Σ
   capacity, e.g. 12/30) for a day, week, or month;
2. **today and tomorrow only**, the names of the employees booked in each room (data
   minimisation — counts only on every other day);
3. an employee's **own reservations** across all time (past, today, future), each with office,
   room, and day;
4. which days in a range the viewer holds a reservation (the calendar highlight).

These are served from the reservation facts that live as `ReservationPlaced` /
`ReservationCancelled` events inside each company-day's stream (ADR-0012). The question is **how
the read side reads them**. The existing `ViewDayReservations` (003 US5) *replays a single
company-day stream on read* and notes the occupancy projection is deferred to 004. Replay is fine
for one named day, but "my reservations across all time" has no bounded set of streams to replay,
and week/month/calendar views replay many streams per request. So 004 needs **materialised** read
models, and a mechanism to keep them current.

ADR-0012's event store was built for exactly this: its `EfCoreEventStore` "does **not** own a
transaction — the caller commits events, Wolverine's durable outbox records, and **inline
projections** together via the context's `SaveChanges`, keeping them atomic in one Postgres
transaction … async catch-up projections are deferred." 004 is the first inline projection, so we
record how it is wired and the consistency it gives.

## Decision drivers

- **Read-your-writes (FR-010, edge case):** a view must reflect *the latest data at the moment it
  is opened*. An asynchronous projection with replication lag could show a stale count to the very
  user who just booked — the opposite of the requirement.
- **No async-projection infrastructure exists** (ADR-0012 deferred catch-up projections), and the
  attendance context owns a **single** database (ADR-0014) — the write model and read models share
  one `AttendanceDbContext`, so one transaction can carry both.
- **The aggregate stays the source of truth (ADR-0026):** the read models carry *no* invariants;
  they are a derived, rebuildable cache.
- **Simplicity first (Constitution VII):** add the least state and the least machinery that serves
  all four query shapes; do not build a message-driven projection bus for an in-process, same-database
  read side.
- **Correctness under the optimistic-retry loop:** reserve/cancel run a bounded `load → decide →
  save` retry on one scoped `DbContext` (ADR-0036); a projection that stages rows on that context
  must not let a failed attempt's rows leak into a later successful save.

## Considered options

- **A — Replay on read, no materialisation.** Extend the `ViewDayReservations` replay to ranges and
  to "my reservations". Rejected: "all my reservations" has no bounded stream set to replay; range
  and calendar views replay N streams per request; and the office rollup recomputes from scratch every
  call. Simple to start, but pushes unbounded work into every read.
- **B — Inline synchronous projection into materialised read models (chosen).** The reservation
  events are projected into read-model tables **in the same `SaveChanges`/transaction that appends
  them**, so the read side is always consistent with the write side and reads are simple indexed
  queries.
- **C — Asynchronous projection (Wolverine handler / catch-up worker).** A projector subscribes to
  the reservation events and updates the read models out of band. Rejected for v1: it introduces
  replication lag against FR-010, needs the async-projection infrastructure ADR-0012 deferred, and
  buys nothing at single-tenant volume where the write transaction can carry the projection for free.
- **D — Denormalised per-(room, day) counter table** (`Count++/--`) **in addition to** the
  reservation rows. Considered for O(1) range reads. Deferred (not rejected): the per-reservation read
  model is required regardless (for my-reservations, names, and calendar), and at v1 volume the
  rollup is a cheap indexed `GROUP BY` over it — a separate counter is redundant state to keep
  consistent. Recorded here as the first optimisation to reach for if range reads ever become hot.

## Decision

We choose **Option B**. Concretely:

- **One materialised read model carries the reservation facts:** `Reservations` — one row per *live*
  reservation `(ReservationId, CompanyId, EmployeeId, OfficeId, RoomId, Date)`. `ReservationPlaced`
  inserts a row; `ReservationCancelled` deletes it. This single model serves occupancy counts (group
  by room/office), "my reservations" (filter by employee), the calendar highlight (the viewer's
  distinct dates), and the today/tomorrow names (join `Employees`).
- **Occupancy figures are computed by query, not stored:** per-room occupied counts are a `GROUP BY`
  over `Reservations` for the date range, joined to the `Rooms` read model for capacity; the office
  rollup sums the rooms of an office. No counter table at v1 (Option D deferred).
- **The projection is inline and transactional.** An infrastructure `IReservationProjection` is
  applied by `AttendanceDayRepository.SaveAsync`: it maps the aggregate's *uncommitted* reservation
  events to read-model row changes staged on the **same** `AttendanceDbContext`, and the existing
  event append's `SaveChangesAsync` commits the events **and** the read-model rows in one Postgres
  transaction. The projection lives entirely in `infrastructure` — `domain`/`application` never see it
  (ADR-0005).
- **The projection is safe under the optimistic-retry loop.** On a concurrency conflict the repository
  **resets the context's change tracker** before returning `Error.Conflict`, so a failed attempt's
  staged events *and* read-model rows are discarded and the bounded retry re-projects against freshly
  reloaded state. The reserve-after-conflict path is covered by an integration test against real
  Postgres.
- **Two existing feeds are extended to supply display data — no new contracts.** Attendance already
  consumes `RoomAdded` (capacity, `Rooms`). 004 additionally consumes organization's existing
  `OfficeOpened` into a new `Offices` read model (office name for the rollup), and persists the
  `DisplayName` already carried by `EmployeeHired` onto the existing `Employees` read model (for the
  today/tomorrow names). These are consumer-side changes within attendance; organization's published
  language (ADR-0031) is unchanged.
- **The today/tomorrow name policy is an application rule.** The occupancy query handler derives
  "today" from the injected `TimeProvider` (Europe/Berlin, as reserve/cancel do, ADR-0036) and
  includes names only for today and the next day; all other days return counts only.

## Consequences

**Positive**
- **Read-your-writes consistency (FR-010):** the read models commit in the same transaction as the
  events, so a view opened immediately after a booking reflects it — no projection lag.
- The read side is simple indexed SQL; no message bus, no catch-up worker, no eventual-consistency
  reasoning for the MVP.
- One read model (`Reservations`) serves every 004 query shape, including the unbounded "my
  reservations"; the aggregate remains the single source of truth and the read models stay
  rebuildable.
- Realises the inline-projection seam ADR-0012 anticipated; sets the pattern for future attendance
  read models.

**Negative / trade-offs**
- The projection is **correctness-critical**: it must run inside the append transaction, be reset on
  conflict so retries don't double-apply, and stay an exact function of the events. Covered by
  integration tests (reserve, cancel, reserve-after-conflict, rebuild).
- Coupling the projection to the write transaction means a projection bug can fail a write. Mitigated
  by keeping the projector a tiny, total event→row mapping with no I/O of its own.
- Occupancy counts are computed per read (`GROUP BY`); acceptable at single-tenant v1 volume
  (ADR-0026). If reads become hot, add the Option D counter table behind the same query port.
- The read models must be **rebuildable** (replay all streams → repopulate) for schema changes and
  recovery; a rebuild path is required, not just forward projection.

**Follow-ups**
- Add an offline **rebuild** routine that truncates the read models and replays every company-day
  stream, so the projection can be re-derived after a schema change (tracked in tasks).
- Revisit Option D (per-(room, day) counter) only if a measured range-read hotspot appears; supersede
  this ADR if the read side moves to asynchronous projection at multi-tenant scale.
- When a second materialised read model needs the same inline seam, consider lifting the
  "apply projection in the save transaction, reset on conflict" into the repository base.
