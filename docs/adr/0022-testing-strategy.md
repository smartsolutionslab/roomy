# 0022. Testing strategy: TDD pyramid, coverage as a diagnostic, mutation testing, Playwright e2e, contract tests

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

TDD is established (ADR-0009) and unit/integration are covered in the language standards,
but coverage policy, mutation testing, e2e, and contract testing had been discussed and
never recorded. With agents writing most tests and a microservices topology, we need an
explicit strategy that resists hollow tests and keeps slow tests few.

## Decision drivers

- Resist coverage-gaming by agents; measure test *quality*, not just execution.
- Keep e2e thin and stable despite eventual consistency across services.
- One authoritative source for the whole testing pyramid.

## Decision

Adopt the pyramid and policy documented in `docs/testing-strategy.md`. Key choices:

- **Coverage is a diagnostic, not a target.** Merge-gate a ~85–90% line+branch floor on
  **domain + application** only; report (don't gate) infrastructure/UI; exclude generated
  code, migrations, the composition root, and behaviourless DTOs.
- **Mutation testing is the real quality signal** — Stryker.NET / StrykerJS on
  domain+application, run nightly.
- **E2e uses Playwright**, thin and critical-path only, driving the real stack composed by
  .NET Aspire (with a Keycloak test realm), eventual consistency handled by polling, smoke
  per PR + full nightly.
- **Contract tests** at the BFF/service seams (OpenAPI provider verification + the typed
  generated client; async event-schema checks) keep e2e thin.

## Consequences

**Positive**
- Test quality is measured (mutation), not faked (coverage); agents can't game a number.
- E2e stays small and meaningful; integration breakage is caught cheaply at the seams.
- A single authoritative testing document.

**Negative / trade-offs**
- Mutation testing and the full e2e suite are slow — hence nightly, not per-PR.
- Playwright + an Aspire-composed stack is real e2e infrastructure to maintain.

**Follow-ups**
- Wire the domain/application coverage floor into CI (Coverlet, Vitest coverage).
- Stand up Playwright against the Aspire app host with a Keycloak test realm.
- Configure Stryker.NET / StrykerJS nightly jobs.
- Establish contract verification at the seams.
