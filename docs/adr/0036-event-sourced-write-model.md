# 0036. Event-sourced write model: aggregate base, repository, and optimistic-retry

- **Status:** Proposed
- **Date:** 2026-06-09
- **Deciders:** Heiko Weiß

## Context and problem statement

The **attendance** context (`003-attendance`) is the first **event-sourced** context. ADR-0012
chose a hand-rolled append-only event store on PostgreSQL and ADR-0026 drew the consistency
boundary as the `AttendanceDay` aggregate (`CompanyId + Date`). The store *seam* already exists
(`libs/infrastructure-persistence/EventStore`: `IEventStore`, `EfCoreEventStore`,
`EventStoreDbContext`, `EventTypeRegistry`, `StreamId`, `StreamVersion`, `EventEnvelope`). What
does **not** exist is the **write model on top of that seam**: how an aggregate is reconstructed
from its stream and records new events, how a repository bridges the aggregate to `IEventStore`,
and how the last-place race (FR-007, scenario 12) is resolved. These are structural choices that
persistence will be built around, so we record them before the code (golden rule 4).

This is distinct from ADR-0032. That ADR added the state-based `Aggregate` base — an entity that
*records* `IDomainEvent`s for intra-context reactions while its state is held in EF-mapped
fields and dispatch is deferred. An **event-sourced** aggregate inverts that relationship: its
state **is** the left-fold over its event stream; there are no separately persisted fields, and
the events are the source of truth (ADR-0012), not an optional side-record. The two models
coexist — identity/organization are state-based; attendance is event-sourced — so they need
distinct bases rather than one overloaded type.

## Decision drivers

- ADR-0005 / `SharedKernelPurityTests`: the domain and shared-kernel stay free of any framework
  or infrastructure dependency. The aggregate base is plain C#; `IEventStore` is touched only at
  the infrastructure edge.
- ADR-0012: events are the source of truth; optimistic concurrency is enforced by the DB unique
  constraint on `(stream_id, version)`. The write model must carry the expected version.
- ADR-0026: both invariants (no-overbooking per room-day; one reservation per employee per day)
  are enforced inside one aggregate, so the race reduces to a single optimistic-concurrency check
  per company-day.
- Simplicity first (`CLAUDE.md`): build the minimum attendance needs; no snapshotting, no async
  projections, no upcasting until load or schema change justifies them (ADR-0012 follow-ups).
- Determinism: the domain must not read an ambient clock; `today` and `OccurredAt` are supplied
  by the caller (mirrors ADR-0032 and the integration-event timestamps).
- Reusability: occupancy (`004`) and any future event-sourced context need the same base, so it
  belongs in the shared-kernel beside `Aggregate`/`IAggregate`.

## Considered options

**For the aggregate base:**

- **A — A dedicated `EventSourcedAggregate` base in the shared-kernel.** Replay via an abstract
  `Apply(@event)` reducer; `Raise(@event)` applies *and* collects into `UncommittedEvents`;
  `Version` tracks the loaded/last-applied `StreamVersion`. Carries `IAggregate` so the
  architecture tests still key on it.
- **B — Extend the existing `Aggregate` (ADR-0032) with replay.** Bolt `LoadFromHistory`/`Apply`
  onto the state-based base. Rejected: conflates two persistence models on one type — a
  state-based aggregate's `DomainEvents` are an optional side-record drained by an interceptor,
  whereas an event-sourced aggregate's events *are* its state. Overloading one base blurs which
  model a given aggregate follows and weakens the architecture tests' intent.
- **C — Keep the base inside the attendance domain.** Rejected: occupancy and future
  event-sourced contexts would duplicate it; the shared-kernel already owns the aggregate markers.

**For the last-place race (FR-007 / scenario 12):**

- **D — Bounded optimistic-retry in the application handler.** Both writers load version *v*,
  both append *v+1*; the unique constraint lets one win and the loser gets
  `EventStoreConcurrencyException`. The handler catches it, **reloads** (now at *v+1*, the room
  one fuller) and **re-decides** against fresh state — so the loser is correctly rejected as
  *room full* rather than overwriting. A small bound (default 3) caps livelock; exhaustion
  returns `Error.Conflict` (`concurrency_retry_exhausted`, "safe to retry").
- **E — Pessimistic lock on the stream.** Rejected: ADR-0012 commits to optimistic concurrency
  at the DB; locking reintroduces contention management unneeded at v1 volume (ADR-0026).
- **F — Retry inside the repository.** Rejected: re-deciding "is the room still full?" is a
  domain question only the use case can answer; the repository stays a thin event-store bridge.

## Decision

We choose **A + D + a thin event-sourced repository**.

**1. `EventSourcedAggregate` (shared-kernel).** An abstract base, sibling to `Aggregate`:

- `void LoadFromHistory(IEnumerable<object> events)` — replays each event through `Apply`,
  advancing `Version`; a stream with no events yields a fresh instance at `StreamVersion.None`.
- `protected abstract void Apply(object @event)` — the reducer; **every** state change happens
  here and nowhere else (events are the source of truth).
- `protected void Raise(object @event)` — appends to `UncommittedEvents` **and** calls `Apply`,
  so the in-memory instance reflects the change immediately.
- `StreamVersion Version { get; }` (the loaded/last-applied version, i.e. the expected version on
  save) and `IReadOnlyList<object> UncommittedEvents { get; }`, drained by the repository on save.

It is framework-free and carries `IAggregate`, so `LayerDependencyConventionTests` /
`SharedKernelPurityTests` continue to key on the marker. Events on the stream are plain records,
registered with stable names in the context's `EventTypeRegistry` (ADR-0012).

**2. Event-sourced repository.** The domain defines the port (e.g.
`IAttendanceDayRepository`); the infrastructure implementation bridges to `IEventStore`:
- **Load** = `ReadStreamAsync(streamId)` → `Aggregate.Rehydrate(replay)` (which calls
  `LoadFromHistory`). An empty stream returns a *fresh* aggregate, never `Error.NotFound` — a
  company-day always exists conceptually.
- **Save** = `AppendAsync(streamId, expectedVersion: aggregate.Version, aggregate.UncommittedEvents)`.
  The `(stream_id, version)` unique constraint is the single serialization point.

**3. Bounded optimistic-retry in the application handler.** The load → decide → save cycle is
wrapped in a bounded retry; on `EventStoreConcurrencyException` it reloads and re-evaluates the
invariant, so a concurrent last-place winner forces the loser to a correct domain rejection.

`today` is passed into the use case from the application edge (Europe/Berlin date via the
injected `TimeProvider`); the aggregate receives it explicitly and reads no ambient clock.

## Consequences

**Positive**
- A clear, framework-free seam for event sourcing that other contexts (occupancy, future) reuse,
  with the two persistence models (state-based vs event-sourced) kept as distinct, intention-
  revealing bases the architecture tests can tell apart.
- The last-place race (FR-007/scenario 12) is resolved with one DB-enforced concurrency check and
  a re-decide, no locks and no distributed transaction (ADR-0026).
- State is always the fold over the log, so projections (occupancy, `004`) are rebuildable by
  replay (ADR-0012).

**Negative / trade-offs**
- We own the correctness-critical replay/append logic and its tests (round-trip, concurrency).
- A busy `AttendanceDay` stream can grow large; **snapshotting is deferred** (ADR-0026/0012
  follow-up) — revisit when a measured stream length justifies it.
- The retry bound is a tuned constant; under pathological contention a writer can still get
  `concurrency_retry_exhausted` and must retry — acceptable at v1 volume, revisit with the
  ADR-0026 hotspot.

**Follow-ups**
- `AttendanceDay : EventSourcedAggregate` (003, US1) is the first user; the base ships with it.
- Event versioning/upcasting stays unbuilt until the first event-schema change (ADR-0012); v1
  event names carry an explicit `.v1` suffix to make that future cheap.
- Define the snapshotting threshold when stream length is measured, and supersede the relevant
  part of this ADR if the retry bound or boundary changes under load.
