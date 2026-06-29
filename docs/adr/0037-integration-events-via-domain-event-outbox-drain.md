# 0037. Integration events published by draining domain events into the outbox at commit

- **Status:** Proposed
- **Date:** 2026-06-09
- **Deciders:** Heiko Weiß

## Context and problem statement

The attendance capacity feed (`003-attendance`, US2) needs the **organization** context to publish
the integration events `OfficeOpened` and `RoomAdded` when an office/room is created, atomically with
the database write (the outbox guarantee, ADR-0012). Organization is **state-based** (EF Core, not
event-sourced) and its use cases run from plain ASP.NET **Minimal-API endpoints** invoking owned
command handlers.

The messaging backbone (ADR-0005, `AddRoomyMessaging`) wires Wolverine's durable transactional
outbox, but `Policies.AutoApplyTransactions()` only wraps **Wolverine message handlers** (and Wolverine
HTTP endpoints) in the outbox transaction. The only existing publisher — identity's `EmployeeHired`
consumer — publishes from *inside* a Wolverine handler, so it is covered. There is **no path today**
to publish transactionally from an HTTP-invoked command handler: a bare
`IIntegrationEventPublisher.PublishAsync` there sends immediately, outside any outbox transaction, so a
crash between the DB commit and the send would drop the event. We must decide how a state-based context
emits integration events without that gap, and without dragging Wolverine into `domain`/`application`
(ADR-0005).

ADR-0032 already added the state-based `Aggregate` base that **records** `IDomainEvent`s and
deliberately left dispatch unbuilt, with a documented follow-up: *"when the first domain-event consumer
lands, add the SaveChanges-interceptor drain that dispatches and clears `DomainEvents` on commit."*
US2 is that first consumer.

## Decision drivers

- **Atomicity (ADR-0012):** the published event must commit in the same transaction as the aggregate
  write, then relay at-least-once. No publish-after-commit gap.
- **Core stays framework-free (ADR-0005):** `domain` and `application` reference neither Wolverine nor
  the broker. Outbound integration events are an infrastructure concern.
- **Published-language boundary (ADR-0031):** the cross-context contract carries IDs/primitives and is
  mapped at the infrastructure edge — the outbound mirror of how consumers map a wire event to an
  internal command.
- **Reuse:** every future state-based publisher needs the same seam; build it once.
- **Simplicity first:** realize the already-documented ADR-0032 seam; don't reshape the endpoints.

## Considered options

- **A — Drain domain events into the outbox at commit.** Aggregates raise intra-context domain events
  (ADR-0032) as they mutate. The context's infrastructure unit-of-work, at `SaveChangesAsync`, drains
  the tracked aggregates' `DomainEvents`, maps the publishable ones to their integration-event
  contracts, **stages them into Wolverine's EF-Core outbox enrolled on the same `DbContext`**, and
  commits — so the aggregate rows and the outbox rows land in one transaction; the events are cleared
  after. The domain→integration mapping lives at the infrastructure edge.
- **B — Convert organization's create endpoints to Wolverine HTTP.** Then `AutoApplyTransactions`
  wraps them and a publish is outboxed. Rejected: refactors freshly-merged 002 endpoints, couples the
  HTTP surface to Wolverine.HTTP, and still leaves the "publish from a use case" question unanswered
  for non-HTTP callers (sagas, seeders).
- **C — Publish-after-commit.** `SaveChangesAsync` then `PublishAsync`. Rejected: a crash in between
  drops the event — exactly the non-atomicity ADR-0012's outbox exists to prevent.
- **D — Inject Wolverine's `IDbContextOutbox` into the application handler.** Rejected: pulls a
  framework type into `application`, violating ADR-0005.

## Decision

We choose **Option A**. Concretely:

- **Aggregates raise domain events** for the facts that have cross-context significance — organization's
  `Office.Create` raises an `OfficeOpened` *domain event*, `Office.AddRoom` raises a `RoomAdded`
  *domain event* (intra-context `IDomainEvent`s carrying the aggregate's own value objects, ADR-0032).
- **The infrastructure unit of work drains and dispatches on commit.** The context's `IUnitOfWork`
  implementation (infrastructure) collects `DomainEvents` from the tracked aggregates, maps each
  publishable one to its **integration-event contract** (ADR-0031) via a small per-context map, stages
  them through Wolverine's `DbContext`-enrolled outbox, calls `SaveChangesAsync` (one transaction for
  aggregate + outbox), flushes, and clears the drained events. This is the realization of ADR-0032's
  deferred seam.
- **The mapping is an infrastructure edge.** `domain`/`application` never see Wolverine or the
  `Contracts.*` types; the domain event → integration event translation sits beside the messaging
  adapter, mirroring the inbound wire-event → internal-command mapping (ADR-0031).
- **The publisher port is unchanged** (`IIntegrationEventPublisher`, ADR-0005) for the
  publish-from-within-a-Wolverine-handler case (identity); this ADR adds the *commit-time drain* path
  for state-based use cases that are **not** Wolverine handlers.

## Consequences

**Positive**
- Atomic publish for state-based contexts with no endpoint refactor; the outbox guarantee (ADR-0012)
  holds for organization exactly as it does for identity.
- Domain events become the single source of truth for both intra-context reactions and outbound
  integration events; aggregates stay expressive and framework-free.
- A reusable seam: a new publisher is "raise a domain event + add one mapping entry."

**Negative / trade-offs**
- We own a correctness-critical drain: it must run inside the transaction, be idempotent against
  re-entrancy, and clear events so they are not double-published. Tested against real Postgres +
  RabbitMQ.
- The domain→integration mapping is a small amount of glue per context (kept at the infrastructure
  edge, not in the core).
- Domain events are now load-bearing for delivery, not just optional reactions — a missing `Raise`
  silently means a missing publish, so the drain path needs integration coverage.

**Follow-ups**
- Implement the drain in organization's `IUnitOfWork` and emit `OfficeOpened`/`RoomAdded` (003 US2,
  T016); wire `AddRoomyMessaging` into `organization-api` (publish-only).
- Attendance consumes the two events at its messaging edge into the `Rooms` read model (003 US2,
  T017/T018), replacing the temporary `UnprovisionedRoomDirectory`.
- If a third state-based publisher appears, consider lifting the drain into a shared base unit of work.

## Update (023, identity is the second state-based publisher)

Identity became the second state-based publisher, so the anticipated follow-up landed: the commit-time
drain was lifted out of `OrganizationUnitOfWork` into a shared `OutboxUnitOfWork` base (in
`infrastructure-messaging`) that owns "collect `DomainEvents` → map → stage in the outbox enrolled on the
same `DbContext` → clear". Both `OrganizationUnitOfWork` and `IdentityUnitOfWork` now derive from it and
supply only their per-context domain→integration map (the infrastructure-edge glue stays per context,
ADR-0031). This closes the silent-drop gap in identity — `User.GrantAdministrator` raised
`AdministratorGranted` but the bare identity unit of work never drained it — and publishes
`AdministratorGranted` (a new identity integration contract) through the same atomic seam. One drain
implementation, so the two contexts cannot drift.
