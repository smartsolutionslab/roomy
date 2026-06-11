# Implementation Plan: Administrator is also an employee

**Branch**: `019-admin-as-employee` | **Date**: 2026-06-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/019-admin-as-employee/spec.md`

## Summary

The seeded `DefaultAdmin` is provisioned as an identity `User` only (the `DefaultAdminSeeder`
in `identity-api` bypasses the hire saga), so it has no `Employee` in organization and no row
in the attendance employee directory. As a result `/reservations/mine` and `POST /reservations`
return `unknown_employee` for an administrator, violating the 1:1 `User`↔`Employee` invariant —
exactly the follow-up ADR-0025 left open ("decide how the seeded `DefaultAdmin` provisions both
its `User` and its `Employee`").

**Approach:** make the DefaultAdmin bootstrap **organization-led**, like every other account
(ADR-0025). The organization context seeds the admin by issuing `HireEmployee` with the
`Administrator` role at startup (after the seeded `Company` exists, idempotently). That drives
the **existing** saga end to end — `EmployeeHired` → identity `RegisterUser` (Keycloak + `User`
with admin elevation) → attendance directory row — so no new cross-context mechanism is added.
The identity-side `DefaultAdminSeeder` is removed; identity receives the admin through the same
consumer path as any hire. Because the admin's `UserId` now flows through all three contexts and
into the `roomy_user_id` Keycloak attribute (ADR-0058, just merged), the administrator can reserve
and view their own reservations.

## Technical Context

**Language/Version**: .NET 10 / C#; Angular 22 (no frontend change expected)

**Primary Dependencies**: Wolverine (saga / outbox-inbox), EF Core on PostgreSQL, Keycloak (OIDC), .NET Aspire (local orchestration)

**Storage**: PostgreSQL — organization DB (`employees`), identity DB (`users`), attendance read model (`employees`); Keycloak realm (user + `roomy_user_id` attribute)

**Testing**: xUnit + Shouldly + NSubstitute (unit); WebApplicationFactory + Testcontainers/Aspire (integration); saga-e2e for the full chain

**Target Platform**: Linux/Windows server (containerized services behind the YARP gateway)

**Project Type**: Web — three backend bounded contexts + gateway + Angular SPA

**Performance Goals**: N/A (startup seeding; one-time bootstrap)

**Constraints**: No distributed transaction (saga + eventual consistency, ADR-0014/0025); no cross-context aggregate references (ADR-0003); idempotent startup seeding; the admin must exist before any user-initiated hire saga runs

**Scale/Scope**: One bootstrap account per environment; ~3–5 files changed plus an ADR and tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Spec-Driven & Test-First** — PASS. Spec 019 with testable criteria; each acceptance criterion becomes a failing test first (organization hire-admin seeder unit test; saga-e2e asserting the admin reaches the attendance directory and can reserve).
- **II. Clean Architecture & DDD** — PASS. Reuses `HireEmployee` (organization application) and the `Employee` aggregate; the bootstrap trigger lives at the organization composition root (a hosted seeder), not in domain/application.
- **III. Context Isolation — IDs & Integration Events** — PASS. The chain is the existing `EmployeeHired` integration event; no new cross-context reference. The admin's link is by `UserId`/`EmployeeId`.
- **IV. No Framework in the Core** — PASS. No new framework use in domain/application.
- **V. Decisions Are Recorded (ADR-before-code)** — **GATE**: relocating the DefaultAdmin bootstrap from identity to organization is a structural decision and resolves ADR-0025's open follow-up. A new **ADR-0059 — "DefaultAdmin bootstrap is organization-led"** MUST be written and Accepted before the implementing code. (Authored in Phase 1.)
- **VI. Green Before Done — No Suppressions** — PASS (gates run on affected projects; no suppressions).
- **VII. Small, Single-Purpose Changes** — PASS. One story, one branch; surgical (remove one seeder, add one organization-side seeder + config wiring).

**Result:** PASS, conditioned on authoring ADR-0059 in Phase 1 before implementation.

## Project Structure

### Documentation (this feature)

```text
specs/019-admin-as-employee/
├── plan.md              # This file
├── research.md          # Phase 0 — bootstrap-mechanism decision
├── data-model.md        # Phase 1 — entities touched (reuses Employee)
├── quickstart.md        # Phase 1 — end-to-end validation
├── contracts/           # Phase 1 — no NEW contracts (reuses EmployeeHired); README explains
└── tasks.md             # Phase 2 — /speckit-tasks (not produced here)
```

### Source Code (repository root)

```text
backend/
├─ apps/
│  ├─ identity-api/
│  │  └─ Seeding/DefaultAdminSeeder.cs        # REMOVED (bootstrap moves to organization)
│  └─ organization-api/
│     └─ Seeding/DefaultAdminSeeder.cs        # NEW — issues HireEmployee(Administrator) idempotently
├─ libs/
│  ├─ organization/
│  │  ├─ application/Commands/HireEmployee.cs # reused (verify Administrator role accepted end-to-end)
│  │  └─ domain/Employees/                    # Employee.Hire reused
│  └─ identity/…                              # EmployeeHiredConsumer reused (already maps Administrator)
└─ tests/
   ├─ organization-integration/               # admin-seeder idempotency + hire-as-administrator
   └─ saga-e2e/                               # admin reaches attendance directory; can reserve
docs/adr/0059-defaultadmin-bootstrap-organization-led.md   # NEW (Phase 1, ADR-before-code)
```

**Structure Decision**: Web app with three backend bounded contexts. The change is confined to the
**organization** composition root (new seeder) and the removal of the **identity** seeder; all
provisioning reuses the existing saga. Admin config (`DefaultAdmin:Email/DisplayName/InitialPassword`)
moves from `identity-api` to `organization-api` and is wired in the AppHost.

## Complexity Tracking

*No constitution violations to justify.* The only gate is the required ADR-0059 (Principle V), which
is satisfied by authoring the ADR before code — not a deviation.
