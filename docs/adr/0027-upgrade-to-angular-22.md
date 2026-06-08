# 0027. Upgrade the frontend to Angular 22 (ahead of Nx support)

- **Status:** Accepted
- **Date:** 2026-06-08
- **Deciders:** Heiko Weiß

## Context and problem statement

The Angular app (ADR-0016) was pinned to Angular 21 (`~21.2.0`). Angular 22.0.0 released
on 2026-06-03, on Angular's regular six-month major cadence. The supporting toolchain is
already ahead of the framework: Nx `22.7.5`, TypeScript `6.0.3`, Node `>=20`. The frontend
is still early-scaffold (one app, one util lib), so the migration surface is small.

The complication: as of 2026-06-08, **no released, beta, or canary `@nx/angular` accepts
Angular 22's build tooling** — every published build peer-requires `@angular/build >= 19.0.0
< 22.0.0`. Angular 22 is five days old and Nx has not yet shipped support. However, the
`web` app builds, serves, and tests through the `@angular/build:*` executors **directly**
(see `apps/web/project.json`); `@nx/angular` is used here only for generators, the
module-boundary rules, and migrations — not on the build path. So the version skew is a
peer-metadata constraint, not a functional one.

We must decide whether to wait for Nx to publish support or upgrade now ahead of it.

## Decision drivers

- Stay inside Angular's active support window; upgrading while the surface is tiny is the
  cheapest it will ever be.
- The build path does not depend on `@nx/angular`, so the Nx peer cap is advisory here.
- Golden rule 3 (green before done): an unsupported combination is only acceptable if the
  full gate suite (lint, test, build) is green and stays green.
- Golden rule 5 (small, single-purpose): a version bump, not a feature-adoption project.

## Considered options

- **Force the upgrade to Angular 22 now**, with an explicit pnpm peer-dependency override
  for `@nx/angular`, and gate it against the full suite.
- **Defer until `@nx/angular` ships Angular 22 support** and upgrade via `nx migrate` then.
- **Stay on Angular 21** indefinitely — rejected; falls behind the support window.

## Decision

We upgrade the frontend to **Angular 22 (`~22.0.0`)** now, ahead of Nx support, because
the build path is independent of `@nx/angular` and the full gate suite is green on the new
version. Specifics:

- All `@angular/*` packages plus the build tooling (`@angular/build`, `@angular/cli`,
  `@angular-devkit/*`, `@schematics/angular`, `@angular/compiler-cli`,
  `@angular/language-service`) and `@angular/cdk` move to `~22.0.0`; `angular-eslint` to
  `^22.0.0`. `@nx/*` packages stay at `22.7.5`.
- A **declared, reviewable pnpm `peerDependencyRules.allowedVersions` override** records
  that we knowingly run `@nx/angular@22.7.5` against the Angular 22 build tooling. This is
  an explicit, visible declaration — not a suppressed gate or test.
- We **adopt no new Angular 22 APIs** in this change (Signal Forms, Resource API, Angular
  Aria are evaluated per-feature later).
- The official v22 migrations were run. Their two **behaviour-preservation shims were
  dropped as unnecessary** for this greenfield shell: `provideHttpClient(withXhr())` reverts
  to `provideHttpClient()` (v22's fetch default — there are no HTTP calls yet), and the
  `nullishCoalescingNotNullable` / `optionalChainNotNullable` diagnostic suppressions were
  removed (no redundant `?.`/`??` exist, so we keep v22's stricter defaults). The suite is
  green without either shim.

The version pin in `CLAUDE.md` moves from `21 (21.2.x)` to `22 (22.x)` in the same change
so the documentation does not drift.

## Consequences

**Positive**
- Frontend is on the current Angular major while the surface is one app + one lib.
- We stay on v22's modern defaults (fetch HTTP backend, stricter template diagnostics)
  rather than carrying behaviour-preservation shims for behaviour that does not exist.
- The override is explicit and easy to find and remove.

**Negative / trade-offs**
- We run an **officially unsupported `@nx/angular` + Angular combination** until Nx ships
  support. `@nx/angular` generators/migrations that assume the Angular 21 builder API could
  misbehave; we accept this because the build path bypasses them and the gates are green.
- The peer override is technical debt to unwind.
- Recurring six-month upkeep is now the baseline cadence.

**Follow-ups**
- **Unwind the override:** when `@nx/angular` publishes a release whose peer range admits
  `@angular/build@22`, bump `@nx/*` to it, run `nx migrate`, and delete the
  `peerDependencyRules` override. Track this as the trigger to return the workspace to a
  fully supported state.
- Evaluate Signal Forms vs. Reactive Forms for form-bearing features in their own slices
  (ADR-0020 validation policy still applies).
- Revisit the Resource API for data-access libraries when the BFF client lands.
