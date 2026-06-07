# 0026. AttendanceDay aggregate granularity (CompanyId + Date)

- **Status:** Proposed
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

The **attendance** context must enforce two invariants atomically (`003-attendance`,
FR-005/FR-007, scenario 12): for any (room, day) the reservations never exceed the room's
capacity, and each employee holds **at most one reservation per day** across all rooms and
offices. An aggregate is the consistency boundary (ADR-0003), so the question is how wide
to draw it. The context map and event storming model the boundary as **`AttendanceDay`,
identified by `CompanyId + Date`** — one aggregate instance per calendar day for the whole
company. We are recording that choice and its trade-off because aggregate granularity is a
structural decision that is expensive to reverse once persistence and the event store
(ADR-0012) are built around it.

## Decision drivers

- The "one reservation per employee per day" rule spans **all rooms and offices** — it
  cannot be enforced inside a per-room or per-office aggregate without cross-aggregate
  coordination.
- The no-overbooking guarantee must hold **under concurrency** (scenario 12) — i.e. with a
  single serialization point per unit of consistency (optimistic concurrency on the event
  store, ADR-0012).
- Single-tenant v1 with one seeded company (ADR-0011): "the company's day" is the whole
  tenant's day.
- Write contention vs. invariant simplicity.

## Considered options

- **A — `AttendanceDay` = `CompanyId + Date`** (one aggregate per company-day). Both
  invariants live inside one boundary; concurrency is a single optimistic-concurrency check
  per day.
- **B — `RoomDay` = `RoomId + Date`** (one aggregate per room-day). No-overbooking is local
  and contention is per room, but "one reservation per employee per day" now spans many
  aggregates and needs a separate cross-aggregate guard (e.g. a uniqueness projection or a
  saga), reintroducing the very race the invariant forbids.
- **C — `EmployeeDay` = `EmployeeId + Date`.** Makes the per-employee rule trivial but moves
  capacity enforcement out of the aggregate entirely.

## Decision

We choose **Option A — `AttendanceDay` identified by `CompanyId + Date`**. Every reservation
or cancellation for a given calendar day loads, mutates, and appends to a single aggregate
instance, so both invariants are enforced inside one transaction with one concurrency check.
The `Occupancy` read model (`004-occupancy`) is a projection off this aggregate's events and
carries no invariants of its own.

We accept the resulting write contention: under single-tenant v1 the booking volume per
company-day is small, and the clean, race-free invariant is worth more than write
parallelism at this stage.

## Consequences

**Positive**
- Both invariants are enforced atomically in one boundary — no cross-aggregate saga or
  uniqueness service needed for "one per employee per day".
- Concurrency (scenario 12) reduces to a single optimistic-concurrency conflict on the
  event store per company-day; the loser retries or is rejected as full.
- A natural, append-only event stream per day fits the hand-rolled event store (ADR-0012).

**Negative / trade-offs**
- **Write contention hotspot:** every booking company-wide on the same date serializes on
  one aggregate. Acceptable at v1 volume, but it is the first thing to revisit at scale or
  when multi-tenant/per-tenant databases (ADR-0011 target) change the load profile.
- The aggregate can grow large on a busy day (many `Reservation` entities); snapshotting may
  become necessary.
- Re-partitioning later (e.g. to `OfficeDay`) is a migration of the event stream's identity —
  costly, hence recording the choice now.

**Follow-ups**
- Define the optimistic-concurrency / retry behaviour on the event store for the last-place
  race (scenario 12).
- Set a threshold at which snapshotting the `AttendanceDay` stream is introduced.
- Revisit the boundary if/when multi-tenant load or per-tenant scaling makes the company-day
  hotspot a measured problem; supersede this ADR if it is re-partitioned.
