# 0040. Shared frontend route guards and not-authorized view

- **Status:** Accepted
- **Date:** 2026-06-09
- **Deciders:** Heiko Weiß

## Context and problem statement

`005-identity-web` introduced two functional route guards — `authGuard` (requires a BFF session,
else redirects to `/bff/login`) and `adminGuard` (requires the `administrator` role, else routes to
`/not-authorized`) — plus the `NotAuthorized` view they route to. They live in
`libs/identity/feature` (`@roomy/identity-feature`, `type:feature`/`context:identity`).

Neither guard contains any identity-specific logic: both depend only on `SessionService` from
`@roomy/shared-data-access` (`context:shared`), and the `administrator` role is a cross-cutting
authorization concept, not an identity-domain detail. The `NotAuthorized` view is purely
presentational.

`006-organization-web` adds an administrator-only offices section that needs exactly the same two
guards and the same not-authorized destination. But `organization-feature` is tagged
`context:organization`, and ADR-0035 forbids a context lib from importing another context's libs —
`organization-feature` importing `@roomy/identity-feature` is a boundary violation. Each new
administrator-gated context (attendance admin actions, occupancy) would hit the same wall.

The options are to duplicate the guards into every context lib (drift, divergent redirect
behaviour) or to host them where every context may legitimately depend on them — `context:shared`.

## Decision

**Relocate the cross-context route guards and the not-authorized view into a new shared frontend
feature library** `libs/shared/feature` → `@roomy/shared-feature`
(`type:feature`/`context:shared`).

1. `authGuard`, `adminGuard`, and the `NotAuthorized` standalone component move from
   `libs/identity/feature/src/lib/auth/` and `…/admin/` into `@roomy/shared-feature`, which becomes
   their single home and re-exports them from its public entrypoint.
2. `identity-feature` consumes them from `@roomy/shared-feature` instead of its own internals; its
   route table and the `/not-authorized` route are rewired to the shared symbols. No behaviour
   change — the guards and view are byte-for-byte the same logic, only relocated.
3. `organization-feature` (and any future administrator-gated context lib) consumes the same guards
   and not-authorized route from `@roomy/shared-feature`.

This is allowed by ADR-0035's dependency rules: `context:identity`/`context:organization` may depend
on `context:shared`, and `type:feature → feature` is permitted. `@roomy/shared-feature` itself
depends only on `@roomy/shared-data-access` and Angular — no context libs — so `context:shared`
isolation holds.

## Considered options

- **A — Leave the guards in `identity-feature`; let other contexts import it.** Zero code movement,
  but a direct `organization → identity` boundary violation that the boundary lint (correctly)
  rejects, and it makes identity a de-facto shared lib it was never meant to be. Rejected.
- **B — Duplicate the guards into each context's feature lib.** No cross-context dependency, but the
  redirect/return-url and role logic — security-relevant — is copied N times and will drift.
  Rejected.
- **C — Put the guards in `@roomy/shared-data-access` (`type:data-access`).** Guards are routing
  policy, not data access, and the not-authorized view is a component — neither fits a data-access
  lib, whose dependency rule (`data-access → data-access, util`) also can't pull in routing/UI
  cleanly. Rejected.
- **D — A shared frontend feature lib owns them (chosen).** Single source of truth, correct layer
  (routed feature + its view), and every context depends on it legitimately through
  `context:shared`.

## Consequences

- A new library `@roomy/shared-feature` (`type:feature`/`context:shared`) exists, generated with the
  same `@nx/angular:library … --unitTestRunner=vitest-analog` convention as ADR-0035; the guard and
  not-authorized specs move with their subjects and stay green.
- `identity-feature` no longer declares the guards; its imports point at `@roomy/shared-feature`.
  This is a surgical, behaviour-preserving edit to already-merged identity code, justified by the
  relocation (it is the same logic, now shared).
- Every administrator-gated frontend context (organization now, attendance/occupancy later) reuses
  one guard implementation, so the BFF redirect and not-authorized behaviour stay uniform.
- `CLAUDE.md` already documents `context:shared → context:shared` and the frontend `type:feature`
  rule; no taxonomy change is needed — only the new lib is added under the existing rules.
