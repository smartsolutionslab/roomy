# 0015. Transport-agnostic messaging: RabbitMQ default, Azure Service Bus and AWS selectable

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

The microservices topology (ADR-0014) makes async integration events the default
inter-service mechanism, which needs a broker beneath Wolverine. We want a default that
fits our experience and self-hosting, but we do **not** want to lock the system to a
single broker or cloud — the transport should be switchable across environments (Azure,
AWS, self-hosted) without touching application or domain code.

## Decision drivers

- Avoid broker and cloud lock-in; portability across Azure and AWS.
- A sensible, well-understood default (RabbitMQ) for local dev and existing experience.
- Managed options available per environment (Azure Service Bus, AWS SQS/SNS).
- The application already depends only on owned messaging abstractions (ADR-0005).

## Considered options

- **RabbitMQ default, with the transport selectable by configuration** (Azure Service
  Bus, AWS SQS/SNS).
- A single fixed broker (RabbitMQ only, or Azure Service Bus only) — rejected: lock-in.

## Decision

**RabbitMQ is the default transport** — self-hostable, runnable as a container on ACA, or
via managed Amazon MQ for RabbitMQ on AWS. The transport sits behind the owned messaging
abstractions and Wolverine's multi-transport support, and is **selected by configuration**
at the composition root:

- `RabbitMq` (default)
- `AzureServiceBus`
- `AmazonSqs` (SQS + SNS)

Each transport is an infrastructure concern; application and domain code are unaware of
it, so switching is a composition-root + config change. To keep transports
interchangeable we route by message type (transport-neutral conventions) and design to
broker-neutral semantics: at-least-once delivery, idempotent consumers via the inbox, and
ordering-tolerant handlers. Broker-specific features (e.g. Service Bus sessions, SQS FIFO
specifics) are avoided unless abstracted behind the messaging port.

## Consequences

**Positive**
- No lock-in: run RabbitMQ self-hosted or on ACA, or switch to Azure Service Bus or AWS
  SQS/SNS by configuration.
- Application and domain stay broker-agnostic (ADR-0005); switching is localized to
  infrastructure + config.
- RabbitMQ default keeps local dev simple (a container in the Aspire app host) and uses
  existing experience.

**Negative / trade-offs**
- Portability requires sticking to lowest-common-denominator semantics; broker-specific
  features are off-limits unless abstracted.
- Multiple transport adapters to configure and test; per-broker topology differs
  (exchanges/queues vs topics/subscriptions vs SNS/SQS) and lives in the transport config.
- Each transport's operational model differs (managed vs self-managed) — chosen per
  environment.

**Follow-ups**
- Define a `Messaging:Transport` config switch and an infrastructure provider per
  transport (RabbitMQ, Azure Service Bus, Amazon SQS/SNS).
- Transport-neutral routing conventions; keep handlers idempotent and ordering-tolerant.
- Document the broker-neutral feature constraints so they aren't violated.
- Aspire runs RabbitMQ locally regardless of the deployed transport.
