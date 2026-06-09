# Phase 0 Research: Occupancy Views

Decisions resolving the unknowns behind the 004 read side. Each records what was chosen, why, and the
alternatives weighed. Architectural choices are captured in **ADR-0038**; this file holds the
finer-grained design rationale the plan builds on.

## R1 — Materialised read models vs. replay on read

**Decision:** Materialise the reservation facts into read models (ADR-0038 Option B); do not replay
streams on read.

**Rationale:** "My reservations across all time" (FR-004) has no bounded set of company-day streams to
replay; week/month/calendar (FR-001/003) would replay many streams per request, and the office rollup
(FR-002) would recompute from scratch each call. `ViewDayReservations` (003 US5) already flagged the
occupancy projection as 004's job. A materialised, indexed read model turns every query shape into
simple SQL.

**Alternatives considered:** Replay-on-read (rejected — unbounded "my reservations", repeated multi-
stream replays); a hybrid (replay today/tomorrow for names, materialise the rest) — rejected as two
code paths for one feature when one read model already yields names by a date filter.

## R2 — Inline synchronous projection vs. asynchronous

**Decision:** Project **inline**, in the same `SaveChanges`/transaction that appends the reservation
events (ADR-0038), via an infrastructure `IReservationProjection` invoked from
`AttendanceDayRepository.SaveAsync`. The read-model rows and the events share one `AttendanceDbContext`
and commit atomically.

**Rationale:** FR-010 and the spec's edge case require a view to reflect *the latest data the moment it
is opened*. An asynchronous projector would lag and could show the booking employee a stale count.
ADR-0012's event store was explicitly built for inline projections ("the caller commits events … and
inline projections together via the context's `SaveChanges`"), and the context owns one database
(ADR-0014), so the write transaction carries the projection for free. Async projection infrastructure
is deferred (ADR-0012) and unjustified at single-tenant volume (ADR-0026).

**Alternatives considered:** Wolverine handler / catch-up worker (rejected — replication lag vs.
FR-010, needs deferred infrastructure); publish-after-commit (rejected — a crash between commit and
project drops the update, the very gap the transactional design avoids).

## R3 — One `Reservations` read model vs. a denormalised counter table

**Decision:** A single `Reservations` read model — one row per *live* reservation — is the projection
target. Occupancy counts are an indexed `GROUP BY` over it joined to `Rooms` (capacity). The
per-(room, day) counter table is **deferred** (ADR-0038 Option D).

**Rationale:** The per-reservation rows are required regardless — for "my reservations" (FR-004), the
calendar own-day highlight (FR-003), and the today/tomorrow names (FR-007). Given those rows exist, a
counter table is a strict aggregate of them: redundant state to keep transactionally consistent, for
an O(rows-in-range) `GROUP BY` that is trivially cheap at v1 volume. Adding it now fails the
simplicity test (Constitution VII). It is recorded as the first optimisation to reach for behind the
same query port if range reads ever become hot.

**Alternatives considered:** Counter table now (deferred — premature, redundant); storing
pre-computed rollups per office-day (rejected — same redundancy, plus offices change as rooms are
added).

## R4 — Projection correctness under the optimistic-retry loop

**Decision:** On a save-time concurrency conflict the repository **resets the `AttendanceDbContext`
change tracker** before returning `Error.Conflict`, so a failed attempt's staged events and read-model
rows are discarded and the bounded retry (ReservePlace/CancelReservation, ADR-0036) re-projects against
freshly reloaded state. The projection is applied immediately before the event append within
`SaveAsync`, so the append's `SaveChanges` commits both.

**Rationale:** Reserve/cancel reuse one scoped `DbContext` across retry attempts. Without a reset, rows
staged by a losing attempt would remain tracked and could commit on a later successful save, corrupting
the read model (e.g. an insert for a reservation that lost the last-place race, scenario 12). Resetting
the tracker on conflict is the clean, total fix and also discards the stale staged *events*. Covered by
an integration test that forces a conflict then succeeds, asserting the read model matches the committed
stream exactly.

**Alternatives considered:** Idempotent keyed upserts (insufficient — does not stop double-staging of a
losing attempt's rows within one context); a fresh `DbContext` per attempt (rejected — larger change to
the proven retry/repository design for no extra safety once the tracker is reset).

## R5 — Read models are derived: a rebuild path is required

**Decision:** Treat all read models as a rebuildable cache (ADR-0026 — they carry no invariants).
Provide an offline **rebuild** routine that truncates `Reservations` (and re-derives from feeds where
applicable) and replays every company-day stream to repopulate it.

**Rationale:** Inline projection only moves the read model *forward*. A schema change, a projector fix,
or recovery needs a way to re-derive the read model from the source of truth (the event streams). The
aggregate remains authoritative, so a rebuild is a pure function of the log. `Offices`/`Employees` are
re-derivable by replaying their integration-event feeds (already idempotent consumers).

**Alternatives considered:** No rebuild / migrate-in-place (rejected — leaves no recovery path and
makes any projector bug unrecoverable without manual SQL).

## R6 — Office name and employee name: extend feeds, no new contracts

**Decision:** Get the office name by consuming organization's **existing** `OfficeOpened` into a new
`Offices` read model; get employee names by persisting the `DisplayName` already carried by the
**existing** `EmployeeHired` onto the `Employees` read model (003 US4 stored only the id link). No new
or changed integration-event contracts.

**Rationale:** Cross-context data comes by ID + events only (ADR-0014/0031). Both names are already in
organization's published language; attendance simply consumes more of what is already emitted. The
`Rooms` read model already supplies room name + capacity. This keeps the rollup display
(office "Munich" 12/30, room "A1" 3/8) and the today/tomorrow names sourced locally — never a
cross-service join.

**Alternatives considered:** A synchronous lookup to organization for names (rejected — cross-service
read, ADR-0014); enriching the `ReservationPlaced` event with names (rejected — names are organization
master data, not attendance facts, and would duplicate/serve-stale a changing value in the log).

## R7 — Today/tomorrow name policy and the calendar highlight

**Decision:** The **application** `ViewOccupancy` handler derives "today" from the injected
`TimeProvider` (Europe/Berlin, as reserve/cancel do, ADR-0036) and includes employee names only for
today and the next calendar day; every other day returns counts only (FR-007 data minimisation). The
calendar (US7/FR-003) renders over the same `/occupancy` figures, with the viewer's own days flagged by
intersecting with `/reservations/mine` — no separate endpoint.

**Rationale:** "Today/tomorrow" is a calendar policy, so it belongs with the time seam in application,
not in the SQL adapter. Serving names is the read model's natural output filtered by date, so no extra
query is needed. The highlight is a presentation concern derived from data the client already holds
(the viewer's reservations), keeping the API surface to two endpoints.

**Alternatives considered:** Pushing the date policy into the read-model query (rejected — buries a
domain/calendar rule in infrastructure and complicates testing the boundary); a dedicated calendar
endpoint returning per-day figures + own-day flags (rejected — redundant with `/occupancy` +
`/reservations/mine`; more surface to version and drift-gate).
