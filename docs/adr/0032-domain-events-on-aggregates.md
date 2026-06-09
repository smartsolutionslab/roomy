# 0032. Domain events raised by aggregates, collected and dispatch-deferred

- **Status:** Proposed
- **Date:** 2026-06-09
- **Deciders:** Heiko Weiß

## Context and problem statement

`CLAUDE.md` and ADR-0003 already name *domain events* as a first-class part of the
domain layer ("domain events for intra-context reactions"), and the identity data model
(`specs/001-identity-access/data-model.md`) specs `AdministratorGranted` —
`{ UserId, OccurredAt }`, raised when an account is elevated to Administrator (US4 /
IA-4). US4 is the first story that needs one, and there is **no mechanism yet**: the
shared-kernel has the marker interfaces `IAggregate`/`IEntity`/`IValueObject` but nothing
to *record* an event on an aggregate.

This is distinct from the existing integration-event path. `UserRegistered` /
`UserProvisioningFailed` are **integration** events — a context's published language,
carried cross-service over the Wolverine outbox (ADR-0031), published imperatively by an
application handler through the owned `IIntegrationEventPublisher`. `AdministratorGranted`
is **intra-context** and has **no consumer** in the MVP: it is the aggregate's own record
that an elevation happened, not a message another service reacts to. We need to decide how
an aggregate raises such an event without pulling a framework into the domain (ADR-0005)
and without building dispatch machinery no consumer yet needs.

## Decision drivers

- ADR-0005 / `SharedKernelPurityTests`: the domain and shared-kernel stay free of any
  framework or infrastructure dependency. A domain event is a plain record.
- Simplicity first (`CLAUDE.md`): build the minimum US4 needs. No event dispatcher,
  interceptor, or subscriber abstraction while nothing consumes the event (YAGNI).
- The pattern must be a clean seam: when a consumer *does* appear (e.g. an audit log, or a
  cross-context signal), dispatch slots in without reshaping the aggregate API.
- Determinism: the domain must not read an ambient clock. `OccurredAt` is supplied by the
  caller (the application handler, via the injected `TimeProvider`), as integration-event
  timestamps already are.

## Considered options

- **A — Collect on the aggregate, defer dispatch.** Add an `IDomainEvent` marker and an
  abstract `Aggregate` base in the shared-kernel that holds a private event list, exposes
  `DomainEvents` (read-only) and `ClearDomainEvents()`, and lets a subclass
  `RaiseDomainEvent(...)`. Aggregates raise events; tests assert on `DomainEvents`. No
  dispatcher is wired until a consumer exists.
- **B — Dispatch now via an EF `SaveChanges` interceptor.** Build the full pipeline —
  interceptor drains `DomainEvents` on commit and hands them to in-process handlers — as
  part of US4. Complete, but speculative infrastructure for an event nothing listens to.
- **C — Publish `AdministratorGranted` as an integration event.** Reuse
  `IIntegrationEventPublisher`. Rejected: it is not cross-context published language (it is
  absent from `integration-events.md`), and routing an intra-context signal onto the broker
  misrepresents it and adds an unwanted at-least-once delivery contract.

## Decision

We chose **Option A**. The shared-kernel gains:

- `IDomainEvent` — a marker interface, sibling to `IValueObject`. A domain event is an
  immutable record of something that happened in the domain; it carries primitives/value
  objects and its own `OccurredAt`.
- `Aggregate` — an abstract base implementing `IAggregate`, holding the event list. It
  exposes `IReadOnlyCollection<IDomainEvent> DomainEvents` and `void ClearDomainEvents()`,
  and offers `protected void RaiseDomainEvent(IDomainEvent)` to subclasses. Aggregate
  roots derive from `Aggregate` instead of implementing the `IAggregate` marker directly;
  `IAggregate` stays the marker the architecture tests key on (`Aggregate : IAggregate`).

Events are **recorded, not dispatched**. An aggregate raises an event into its collection;
nothing drains it in the MVP, and the per-request `DbContext` scope means a saved aggregate
is discarded with its events rather than outliving them. When a real consumer appears, the
dispatch seam is a single addition at the infrastructure edge — a `SaveChanges` interceptor
that drains `DomainEvents` into in-process handlers (or the outbox) and clears them as part
of the commit — and the aggregate API does not change. Until then the unit of work is a
plain `SaveChangesAsync`; `ClearDomainEvents()` exists for that future drain and for tests.

`OccurredAt` is passed into the raising method by the caller, never read from an ambient
clock, keeping the domain deterministic and testable (mirrors how the application stamps
integration events with the injected `TimeProvider`).

## Consequences

**Positive**
- The domain stays framework-free (ADR-0005) — a marker interface and a base class with a
  `List<IDomainEvent>`, nothing more.
- US4 ships exactly what it needs: `User.GrantAdministrator` raises `AdministratorGranted`,
  asserted directly on the aggregate. No dead dispatch infrastructure.
- A documented seam for later: adding dispatch is additive and local to infrastructure.

**Negative / trade-offs**
- A raised event with no consumer is, today, write-only state — collected and cleared
  without anyone reacting. This is intentional (the contract says the elevation "raises
  `AdministratorGranted`") but means the first real subscriber must also add the drain.
- Introducing an `Aggregate` base changes aggregate roots from implementing the
  `IAggregate` marker to deriving from a base class. Only `User` exists today, so the blast
  radius is one type; future aggregates follow the base-class convention.

**Follow-ups**
- `User` derives from `Aggregate` and raises `AdministratorGranted` on elevation (US4).
- When the first domain-event consumer lands, add the `SaveChanges`-interceptor drain that
  dispatches and clears `DomainEvents` on commit, and record it as a follow-up ADR; until
  then, dispatch stays deliberately unbuilt and the unit of work only saves.
