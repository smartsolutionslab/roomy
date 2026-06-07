# 0018. REST/JSON with OpenAPI now; gRPC reserved for hot internal paths

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Async integration events are the default for inter-service work (ADR-0014), so this
decision covers the **synchronous** paths: the public surface (SPA ↔ BFF) and the
occasional service-to-service call. We need a protocol that is simple now without closing
the door on efficiency later.

## Decision drivers

- The SPA ↔ BFF edge is HTTP/JSON regardless (browser).
- Synchronous inter-service calls are the exception, not the rule.
- Simplicity and debuggability now; a performance path available later.

## Considered options

- REST/JSON everywhere.
- gRPC for internal service-to-service, REST at the edge.
- **Hybrid: REST/JSON + OpenAPI now, gRPC introduced later only for specific hot paths.**

## Decision

**REST/JSON documented with OpenAPI** for v1, at the edge and for internal synchronous
calls. The typed Angular client is generated from the OpenAPI spec. Service API contracts
are versioned. **gRPC is introduced later only for specific high-throughput or
low-latency internal paths**, contract-first (`.proto`) when it arrives. Synchronous calls
stay behind abstractions, so swapping one path to gRPC is localized and does not ripple.

## Consequences

**Positive**
- Simplest v1; one mental model; easy debugging; OpenAPI-driven typed Angular client.
- The gRPC path is preserved without paying its tooling cost now.

**Negative / trade-offs**
- REST internal sync calls are less efficient than gRPC — acceptable while sync is rare.
- When gRPC arrives, two protocols coexist; mitigated by confining it to identified hot
  paths.

**Follow-ups**
- OpenAPI generation per service; generate the Angular client from the spec.
- Define an API versioning convention.
- Revisit gRPC when a concrete hot internal path is identified.
