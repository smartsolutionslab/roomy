# 0014. Microservices — one service per bounded context

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Roomy's bounded contexts can be deployed as modules of a single backend (modular
monolith) or as independent services (microservices). We need to choose the topology, as
it shapes data ownership, inter-context communication, local development, CI, and ops.

## Decision drivers

- Independent deployability, scaling, and fault isolation per context.
- Hard physical boundaries between contexts (no accidental coupling via a shared DB).
- Alignment with the chosen stack — .NET Aspire orchestration, YARP gateway, Wolverine
  messaging — and existing distributed-systems experience.

## Considered options

- **Microservices — one service per bounded context.**
- Modular monolith (one deployable, contexts as modules) — recommended for v1 simplicity,
  not chosen.

## Decision

Each bounded context is an **independently deployable service** behind the YARP
gateway/BFF, orchestrated by .NET Aspire. Consequences that follow and are now binding:

- **Database-per-service:** each service owns its data; no shared database, no
  cross-service joins or direct DB access.
- **Async integration events are the default** inter-service mechanism, via Wolverine
  with the transactional outbox/inbox (ADR-0012). Synchronous service-to-service calls
  only where genuinely required, via Aspire service discovery.
- **No distributed transactions:** multi-service workflows use sagas/process managers and
  eventual consistency.
- **Minimal, versioned shared contracts:** integration-event contracts are shared
  deliberately and kept small; no fat shared library that recouples services.

This brings the Wolverine + message-broker need forward relative to ADR-0005's "as late
as possible" (which assumed a monolith): the messaging backbone is required as soon as the
first cross-service flow exists. The repo layout (`apps/<context>-api`, `apps/gateway`)
already reflects this topology.

## Consequences

**Positive**
- Independent deploy/scale and fault isolation per context.
- Hard boundaries — contexts cannot couple through a shared database.
- The stack already suits it (Aspire, YARP, Wolverine, per-service Postgres) and matches
  the database-per-tenant target.

**Negative / trade-offs**
- The distributed-systems tax: network failure handling, eventual consistency, sagas
  instead of transactions, no cross-service joins, harder debugging — observability
  (OpenTelemetry tracing across services) becomes essential, not optional.
- More infrastructure and ops from day one (multiple services + a broker + service
  discovery), heavier for a solo-plus-agents team and a single-tenant v1.
- A single story may now span multiple services; the monorepo keeps such changes atomic
  in one PR, which mitigates this.

**Follow-ups**
- Choose a message broker (next decision).
- Define the integration-event contract strategy (minimal, versioned).
- Database-per-service provisioning, composed locally by the Aspire app host (with the
  broker, Keycloak, and Postgres).
- Saga/process-manager approach for multi-service workflows.
- Distributed tracing via OpenTelemetry across all services.
