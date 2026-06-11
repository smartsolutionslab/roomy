# Phase 0 Research: Administrator is also an employee

## Decision 1 — Bootstrap the DefaultAdmin organization-led, via a seeded `HireEmployee(Administrator)`

**Decision**: Move the DefaultAdmin bootstrap out of `identity-api` and into the **organization**
context. At organization startup, after the seeded `Company` exists, a hosted seeder issues
`HireEmployee(name, email, EmployeeRole.Administrator, initialPassword)` once (idempotently). The
existing saga does the rest. Remove `identity-api/Seeding/DefaultAdminSeeder.cs`.

**Rationale**:
- ADR-0025 chose an **organization-led** saga as the single entry point for creating an account
  (`HireEmployee` → `EmployeeHired` → identity `RegisterUser`). Bootstrapping the admin the same way
  upholds the 1:1 `User`↔`Employee` invariant by construction and resolves ADR-0025's explicit
  follow-up rather than inventing a parallel path.
- The full chain already exists and already supports administrators (verified):
  - `EmployeeRole.Administrator` exists; `OrganizationIntegrationEventMap` maps it to
    `HiredRole.Administrator` on `EmployeeHired`.
  - identity's `EmployeeHiredConsumer` maps `HiredRole.Administrator` → `Role.Employee.GrantAdministrator()`
    and issues `RegisterUser`, which provisions the Keycloak user (with the `roomy_user_id` attribute,
    ADR-0058) and the identity `User`.
  - attendance's `EmployeeHiredConsumer` inserts the `{EmployeeId, UserId, DisplayName}` directory row.
- `HireEmployeeHandler` mints a fresh `UserIdentifier` and threads it through `Employee` → `EmployeeHired`
  → identity `User` → attendance directory → Keycloak `roomy_user_id`. So the admin's identity is
  consistent across all three contexts, which is exactly what `/reservations/mine` and `POST /reservations`
  need.
- No new cross-context mechanism, contract, or aggregate reference — strictly a reuse, satisfying
  Principles III and VII.

**Alternatives considered**:
- **Keep the identity seeder AND add an organization admin `Employee`.** Rejected: two bootstrap paths
  on a shared `UserId`. The organization side would emit `EmployeeHired`, and identity's consumer would
  try to `RegisterUser` an already-existing admin `User` → Keycloak `email_taken` conflict. The bootstrap
  must be a single path.
- **Have identity-api seed the organization `Employee` directly.** Rejected: violates context isolation
  (each service owns its DB; no cross-context writes) — ADR-0003/0014.
- **Hire as `Employee`, then elevate via `GrantAdministrator`.** Rejected as unnecessary: the hire path
  already carries `Administrator` end-to-end in one step. (Kept as a fallback only if a constraint
  surfaces against hiring an admin directly.)

## Decision 2 — Record the bootstrap location as ADR-0059 (ADR-before-code)

**Decision**: Author **ADR-0059 — "DefaultAdmin bootstrap is organization-led"** before implementation.
It records that the seeded administrator is provisioned by an organization-side `HireEmployee(Administrator)`
at startup (resolving ADR-0025's deferred follow-up), and that the identity-side seeder is removed.

**Rationale**: Relocating the bootstrap and removing a seeder is a structural, cross-cutting decision
(Principle V). ADR-0025 explicitly deferred it. Recording it keeps the ADR trail complete and updates the
`001-identity-access` context-map understanding.

**Alternatives considered**: Folding the note into ADR-0025 — rejected; accepted ADRs are immutable and
amended only by a superseding/forward ADR.

## Decision 3 — Idempotent, ordering-safe startup seeding

**Decision**: The organization admin-seeder runs after company seeding and is a no-op if an employee with
the admin email already exists (guard before issuing `HireEmployee`). It tolerates repeated startups and
restarts. Convergence of the downstream `User`/Keycloak/attendance rows is **eventual** (saga + outbox/inbox),
consistent with ADR-0025.

**Rationale**: FR-006 requires idempotent set-up with no duplicate staff record; the admin must exist before
any user-initiated hire saga runs, so it is seeded at startup rather than via an API call. `WorkEmail`
uniqueness in organization backs the guard.

**Alternatives considered**: Relying solely on a unique-email constraint to reject duplicates — rejected as
it would surface as an error on every restart rather than a clean no-op.

## Open questions

None blocking. Confirm during implementation that `HireEmployee` with `EmployeeRole.Administrator` is
reachable from the seeder (application command is role-agnostic; only the public hire *endpoint* may
restrict roles — the seeder calls the command/handler directly, not the endpoint).
