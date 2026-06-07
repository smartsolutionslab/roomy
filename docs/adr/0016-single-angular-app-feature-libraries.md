# 0016. Single Angular app with feature libraries per context

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

With a microservices backend (ADR-0014), the frontend could mirror it with independently
deployable micro-frontends, or stay a single app that is internally modular. We need to
choose how the Angular frontend is composed.

## Decision drivers

- Simplicity of build, deploy, and end-to-end testing for a solo-plus-agents team.
- Real modularity and enforced boundaries between context features.
- A simple client surface — the SPA should talk to one origin.

## Considered options

- **Single Angular app with feature libraries per context (Nx).**
- Micro-frontends with Native Federation, one per context — independent deployability at
  the cost of build/runtime complexity (shared-dependency management, version skew,
  cross-remote routing/state).

## Decision

A **single Angular app** (`apps/web`) composed of **feature libraries per bounded
context** (Nx `type:feature | ui | data-access | util` plus a context tag), with the same
`@nx/enforce-module-boundaries` discipline as the backend. Routes are lazy-loaded per
feature for code-splitting. A shared UI/design-system library holds common components;
per-context data-access libraries wrap BFF calls. The SPA talks only to the single YARP
BFF origin, which composes the backend services — the frontend has no knowledge of
individual services. No runtime federation.

## Consequences

**Positive**
- One build and one deployment; simplest ops and e2e.
- Modularity with enforced boundaries via Nx; lazy-loaded routes for performance.
- The SPA hits one origin (the BFF) and stays simple.

**Negative / trade-offs**
- No independent per-context frontend deployment — acceptable for solo + agents and v1;
  revisit only if separate teams come to own separate frontends.
- Feature libraries must respect boundaries (already enforced by Nx).

**Follow-ups**
- Define the frontend Nx tag taxonomy and boundary rules.
- Lazy-loaded routes per feature; a shared UI/design-system library; per-context
  data-access libraries calling the BFF.
- Keep frontend feature libraries aligned with the backend context names.
