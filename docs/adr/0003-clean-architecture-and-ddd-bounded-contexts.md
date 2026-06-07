# 0003. Clean Architecture and DDD bounded contexts

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Roomy's domain — attendance planning across offices, spaces, and teams — has real
rules and invariants that must not leak into infrastructure or UI concerns. With code
generated largely by agents, the architecture has to make the *correct* dependency
direction the *only possible* one, so business logic stays isolated and testable
regardless of who (or what) writes it.

## Decision drivers

- Keep domain logic free of framework and infrastructure dependencies.
- Make invariants enforceable and testable in isolation.
- Decompose the system along domain seams, not technical layers alone.

## Considered options

- **Clean Architecture per bounded context**, contexts as DDD modules.
- A traditional layered (n-tier) architecture across the whole app.
- Transaction-script / anemic services.

## Decision

Each bounded context is structured with **Clean Architecture**: `domain` (entities,
aggregates, value objects, domain events) depends on nothing; `application` (use cases,
ports) depends only on `domain`; `infrastructure` implements ports and depends inward;
hosts compose everything. Contexts integrate only via IDs and integration events —
never by referencing another context's aggregate types. The dependency rule and key
DDD invariants are enforced by architecture tests (NetArchTest) in `tests/architecture`.

## Consequences

**Positive**
- Domain logic is portable and unit-testable without infrastructure.
- The dependency rule is verified in CI, not assumed.
- Clear seams for the contexts; coupling is explicit and intentional.

**Negative / trade-offs**
- More moving parts and ceremony per context than a single-layer app.
- Requires mapping between layers (e.g. domain ↔ persistence models).

**Follow-ups**
- Implement the NetArchTest rule set (dependency rule + "no MediatR" + value-object
  conventions + cross-context-by-ID).
- Record the messaging mechanism for cross-context integration events (see 0005).
