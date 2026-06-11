# 0059. DefaultAdmin bootstrap is organization-led

- **Status:** Accepted
- **Date:** 2026-06-11
- **Deciders:** Heiko Weiß

## Context and problem statement

Every Roomy account is a 1:1 pair: a `User` in **identity** and an `Employee` in **organization**
(ADR-0025, the context map). ADR-0025 chose an **organization-led saga** for creating accounts —
`HireEmployee` (organization) → `EmployeeHired` → identity `RegisterUser` — and explicitly left one
follow-up open: *"decide how the seeded `DefaultAdmin` (which must exist before any saga runs)
provisions both its `User` and its `Employee` record at startup."*

It was never decided, and the seeded `DefaultAdmin` was bootstrapped the easy way: a
`DefaultAdminSeeder` hosted service in **identity-api** creates the `User` (and Keycloak account)
directly, bypassing the hire saga. So the admin has a `User` but **no `Employee`**, and therefore
no row in the attendance employee directory. The result (surfaced once the `roomy_user_id` claim
fix, ADR-0058, made current-user resolution work): an administrator gets `unknown_employee` (404)
on `POST /reservations` and `GET /reservations/mine`, while every hired employee succeeds. The
seeded admin silently violates the 1:1 `User`↔`Employee` invariant.

The admin is a genuine bootstrap problem: it must exist *before* any user-initiated `HireEmployee`
can run (someone has to be the first administrator), so it cannot be created by an ordinary hiring
action — it has to be seeded at startup.

## Decision drivers

- ADR-0025: organization-led saga is the single account-creation entry point; integrate by ID +
  integration events, eventual consistency, no distributed transactions.
- ADR-0003/0014: a context owns its own database; no cross-context aggregate references or writes.
- The 1:1 `User`↔`Employee` invariant must hold for **every** account, including the seeded admin.
- Avoid a second, parallel provisioning path (and the double-provisioning failure it invites).
- Minimal new surface: reuse the existing saga rather than invent bootstrap-only machinery.

## Considered options

- **A — Organization-led bootstrap (chosen).** The **organization** context seeds the admin at
  startup by issuing `HireEmployee(…, EmployeeRole.Administrator, …)` once (after the seeded
  `Company` exists, idempotently). The existing saga then provisions the identity `User` (admin
  role) + Keycloak user, and the attendance directory row — exactly the path every hire takes. The
  identity-side `DefaultAdminSeeder` is removed.
- **B — Keep the identity seeder and also seed an organization `Employee`.** Two bootstrap paths on
  one shared `UserId`. The organization side would emit `EmployeeHired`, and identity's consumer
  would try to `RegisterUser` an already-existing admin `User` → Keycloak `email_taken`. The
  bootstrap must be a single path.
- **C — Have identity-api create the organization `Employee` directly.** Violates context isolation
  (a service writing another service's database) — ADR-0003/0014.

## Decision

**Option A.** The seeded `DefaultAdmin` is bootstrapped **organization-led**: an organization-side
startup seeder issues `HireEmployee` with the `Administrator` role (idempotent — a no-op if an
employee with the admin email already exists), and the existing saga provisions the rest. The
identity-side `DefaultAdminSeeder` (and its options/hosted-service) is removed; identity receives
the admin through the same `EmployeeHired` → `RegisterUser` path as any hire. The `DefaultAdmin:*`
configuration moves from `identity-api` to `organization-api`.

This resolves ADR-0025's open follow-up. The hire path already carries `Administrator` end to end
(`EmployeeRole.Administrator` → `HiredRole.Administrator` → `Role.Employee.GrantAdministrator()`),
and the admin's single `UserId` flows to the identity `User`, the attendance directory, and the
Keycloak `roomy_user_id` attribute (ADR-0058), so the administrator can reserve and view their own
reservations like any employee.

## Consequences

**Positive**
- The 1:1 `User`↔`Employee` invariant holds for the seeded admin; `unknown_employee` no longer
  occurs for an administrator.
- One account-creation path for everyone (reuses ADR-0025's saga); no bootstrap-only mechanism,
  no cross-context write, no new contract.
- Admin provisioning becomes eventually consistent like every hire — consistent mental model.

**Negative / trade-offs**
- The admin's `User`/Keycloak now appear *after* the saga converges rather than synchronously at
  identity startup; first-login readiness is eventual (acceptable for a dev/bootstrap account, and
  consistent with ADR-0025's eventual-consistency stance).
- `DefaultAdmin:*` configuration moves to `organization-api`; the AppHost wiring changes.
- A brief startup window where the admin `Employee` exists before its `User` is provisioned.

**Follow-ups**
- Update `001-identity-access` / `002-office-management` notes so the DefaultAdmin provisioning
  reads as organization-led (this ADR closes ADR-0025's deferred item).
