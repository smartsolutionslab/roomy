# 0012. EF Core for persistence; hand-rolled event store on PostgreSQL for event-sourced contexts

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Persistence is chosen per bounded context. Most contexts are state-based; some are
event-sourced. We want a single database engine and minimal third-party coupling
(consistent with the framework-independence stance in ADR-0005), and we self-host (GDPR,
Hetzner), so standing up a separate event-store product or adopting a document-store
library is undesirable. EF Core is the chosen baseline; the open question was how to
event-source without a dedicated store.

## Decision drivers

- One database engine and one persistence stack to operate.
- Framework independence / minimal third-party coupling.
- Transactional consistency between events, the outbox, and read models.
- Self-hosting simplicity; alignment with the database-per-tenant target (ADR-0011).

## Considered options

- **EF Core baseline + hand-rolled append-only event store on the same PostgreSQL.**
- Marten as the event store on shared Postgres — rejected to avoid a second persistence
  library, despite its strong fit.
- EventStoreDB / KurrentDB — rejected to avoid operating a separate datastore.

## Decision

EF Core on PostgreSQL is the **default** persistence for state-based aggregates and
relational read models. **Event-sourced contexts use a hand-rolled event store on the
same PostgreSQL via EF Core:**

- An **append-only events table** keyed by stream (aggregate) id, with a per-stream
  `version`, `jsonb` payload and metadata, and a monotonic global sequence (`bigserial`)
  for ordering.
- **Optimistic concurrency enforced by a unique constraint on `(stream_id, version)`** —
  at the database, not only in code.
- **Events are the source of truth; projections/read models are derived and
  rebuildable** by replaying events.
- For v1, **projections update synchronously (inline) in the same transaction as the
  event append**, alongside a transactional **outbox** table. A single Postgres
  transaction makes events + outbox + projections atomic.
- **Async catch-up projections and snapshots are deferred** until load justifies them
  (simplicity first).

The application repository ports keep their shape (ADR-0005); only the infrastructure
implementation differs for event-sourced contexts. PostgreSQL is the pinned database
engine.

## Consequences

**Positive**
- One database engine and one persistence stack; simplest ops, fits self-hosting and the
  database-per-tenant target.
- No third-party event-store or library coupling; full control of the schema and event
  model.
- Events, outbox, and inline projections commit atomically in one Postgres transaction.

**Negative / trade-offs**
- We own the correctness-critical pieces: DB-enforced optimistic concurrency, event
  (de)serialization with a type registry, and event versioning/upcasting as events
  evolve.
- We forgo mature built-in subscriptions and projection tooling; async projection
  infrastructure is future work if needed.
- More code and tests than adopting Marten — accepted; revisit if the event-sourcing
  needs outgrow a hand-rolled store.

**Follow-ups**
- Define the events-table schema and event-type registry in the first event-sourced
  context's spec.
- Decide the event versioning/upcasting approach before the first event-schema change.
- Keep projections rebuildable from the event log.
- Add the transactional outbox table now; Wolverine can take it over later (ADR-0005).
