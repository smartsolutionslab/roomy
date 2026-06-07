# 0002. Nx monorepo with enforced module boundaries

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Roomy spans a .NET backend organised into bounded contexts and an Angular frontend.
With agents generating much of the code, the biggest structural risk is unwanted
coupling — code reaching across context or layer boundaries it should not. We need a
repository structure where those boundaries are not just documented but mechanically
enforced, and where CI only rebuilds and retests what actually changed.

## Decision drivers

- Boundaries must be enforced in CI, not left to reviewer vigilance.
- Fast feedback: build/test only affected projects.
- Backend and frontend coordinated in one place, with a single dependency graph.

## Considered options

- **Nx monorepo** with tagged projects and a module-boundary lint rule.
- Multiple separate repositories (polyrepo).
- A plain monorepo without tooling-enforced boundaries.

## Decision

We use an **Nx monorepo**. Every project carries tags (by context and by layer), and
the `@nx/enforce-module-boundaries` lint rule defines which tags may depend on which.
Agents physically cannot import across a forbidden boundary without the lint failing.
`nx affected` scopes CI to changed projects.

## Consequences

**Positive**
- Context and layer boundaries are enforced automatically and visibly.
- One dependency graph spanning backend and frontend; coordinated changes are atomic.
- Faster CI via affected-only execution.

**Negative / trade-offs**
- Nx is an additional tool to learn and keep configured.
- Tagging discipline is required for every new project.

**Follow-ups**
- Define the canonical tag taxonomy (`context:*`, `type:domain|application|infrastructure|app`).
- Mirror these boundaries in the .NET solution with architecture tests (see 0003).
