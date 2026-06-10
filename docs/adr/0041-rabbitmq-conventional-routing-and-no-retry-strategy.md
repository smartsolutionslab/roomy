# 0041. Route integration events by convention; no EF retry strategy with the Wolverine outbox

- **Status:** Proposed
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

The messaging backbone (ADR-0005/0015/0037) wires Wolverine's durable transactional outbox/inbox over
RabbitMQ: a context publishes integration events that another context consumes, integrating only by ID
and events (ADR-0014). Until now every test exercised this **without a broker** — consumers were called
directly (`Handle(...)`), or the Wolverine runtime was dropped in the in-process host tests, or the
outbox drain was verified against a capturing fake (`OrganizationUnitOfWorkTests`). The first genuine
**cross-service round-trip over a real RabbitMQ + Keycloak + both hosts** (the `008` hire-employee
provisioning saga, exercised by `tests/saga-e2e`) revealed that the backbone **did not actually deliver
any integration event between services**. Two independent defects were masking each other:

1. **No routing.** The RabbitMQ transport was configured `UseRabbitMq(...).AutoProvision()` with **no
   routing rules and no conventional routing**. `IMessageBus.PublishAsync(integrationEvent)` therefore
   had no cross-process route: Wolverine dropped the message (no subscribers), so `EmployeeHired` never
   reached identity, `RoomAdded`/`OfficeOpened` never reached attendance, and the provisioning acks never
   returned to organization. Every publish was a silent no-op over the wire.

2. **Retrying execution strategy vs. the outbox transaction.** Once routing was enabled, every publish
   and every consume threw
   `InvalidOperationException: NpgsqlRetryingExecutionStrategy does not support user-initiated
   transactions`. The shared `AddRoomyDbContext` enabled `EnableRetryOnFailure()` on **every** context,
   and Wolverine's outbox/inbox begins a user-initiated transaction to commit the aggregate write and the
   outbox/inbox rows atomically (ADR-0012/0037). The two are mutually exclusive; the retrying strategy
   only surfaced the conflict once a message was actually sent or received.

Both were invisible to the existing pyramid (unit + contract + drain tests), which never opened a broker.
This is the gap issue #68 tracked ("assert the message is delivered over RabbitMQ to a consumer").

## Decision drivers

- The async backbone must actually deliver events cross-service — it is load-bearing for every saga and
  feed (the capacity feed, the provisioning saga, all future flows), not optional.
- Atomicity (ADR-0012): the publish/consume must commit in the same Postgres transaction as the aggregate
  write; the durable inbox dedups redeliveries. Wolverine owns that transaction.
- Reuse: one transport configuration for every host; adding a context must stay mechanical.
- The fix must be verified by a real round-trip, not asserted in isolation.

## Decision

**1. Enable Wolverine conventional routing on the RabbitMQ transport.**
`ConfigureTransport` adds `.UseConventionalRouting()`. Each integration-event type maps to an exchange
named for it, and each consumer's queue binds to that exchange — so a published event reaches every
context that handles it, by type, with no per-message wiring. `AutoProvision` creates the topology at
startup. The publisher publishes the contract type and the consumer handles the same type, so both sides
agree on the exchange (ADR-0031).

**2. Do not enable the Npgsql retrying execution strategy on context DbContexts.**
`AddRoomyDbContext` no longer calls `EnableRetryOnFailure()`. At-least-once delivery and retry are
provided by **Wolverine** at the messaging layer (the durable outbox relays until acked; the inbox dedups
on redelivery); a transient database fault surfaces to the caller rather than being retried in the data
layer. This keeps the DbContext compatible with the user-initiated transactions the outbox/inbox require.

These together make the backbone functional; the change regenerates each messaging host's committed
Wolverine codegen (ADR-0034), because consumers now enroll the outbox transaction for their own outgoing
messages.

## Consequences

**Positive**
- Integration events are actually delivered cross-service: the provisioning saga converges end-to-end,
  and the capacity feed (`RoomAdded`/`OfficeOpened` → attendance) routes for real.
- One conventional configuration covers every event type and host; adding a contract or consumer needs no
  routing code.
- The outbox/inbox transaction is no longer in conflict with the data-layer strategy; publishes/consumes
  commit atomically as designed.

**Negative / trade-offs**
- Dropping `EnableRetryOnFailure` removes automatic data-layer retry of transient Postgres faults for
  **all** operations (including non-messaging reads/writes). Acceptable: messaging resilience is owned by
  Wolverine, and a transient fault now fails fast and is retried by the caller/relay rather than silently
  inside EF. Revisit with an explicit `CreateExecutionStrategy`-wrapped unit of work if data-layer retry
  is later wanted alongside the outbox.
- Conventional routing names exchanges by message type; renaming/moving a contract type changes its
  exchange (mitigated by the stable `Contracts.*` namespace, ADR-0031).
- Every messaging host's committed codegen changes and must match a Linux regeneration in CI's
  `codegen verify`.

**Follow-ups**
- The cross-service round-trip is now covered by `tests/saga-e2e` (real RabbitMQ + Keycloak + both hosts),
  closing the verification gap of issue #68.
- Consider a lighter Testcontainers-only messaging round-trip (no Keycloak) for faster feedback, if the
  full-stack e2e proves slow in CI.
