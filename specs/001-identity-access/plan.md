# Implementation Plan: Identity & Access

**Branch**: `001-identity-access` | **Date**: 2026-06-07 | **Spec**: `specs/001-identity-access/spec.md`

**Input**: Feature specification from `/specs/001-identity-access/spec.md`

## Summary

The identity context manages user accounts and the Employee/Administrator role model and
exposes login/logout and role-based authorization. Authentication is **email + password via
Keycloak** (self-hosted OIDC) behind the YARP BFF — no tokens in the SPA (ADR-0013). Keycloak
owns credential verification; the identity context owns the account/role record and provisions
the matching Keycloak user. Account provisioning is the **organization-led `HireEmployee` saga**
(ADR-0025, eventual consistency). A `DefaultAdmin` is seeded into Keycloak from configuration so
the system is administrable from first start.

## Technical Context

**Language/Version**: C# / .NET 10 (root namespace `SmartSolutionsLab.Roomy`, file-scoped
namespaces, nullable on, warnings-as-errors, async-all-the-way with `CancellationToken`).

**Primary Dependencies**: ASP.NET Core host; EF Core on PostgreSQL (infrastructure only);
Wolverine for integration events (composition root only, behind owned abstractions, ADR-0005);
owned application command/query abstractions (no MediatR). **Keycloak** as the OIDC provider;
the identity service talks to it via the Keycloak Admin REST API (infrastructure adapter only)
to provision users and assign roles. The YARP gateway is the OIDC client and holds the session
(BFF pattern, ADR-0013).

**Storage**: PostgreSQL — the identity service's own database (no shared DB, ADR-0011/0014). It
stores the account/role projection and the link to the Keycloak subject; **credentials live in
Keycloak, never in this database** (no `PasswordHash` stored here).

**Testing**: xUnit (unit + integration); NetArchTest architecture rules in `tests/architecture`;
integration tests against PostgreSQL (and a Keycloak container) via Testcontainers, per
`docs/testing-strategy.md`. Auth/BFF flow covered by an e2e check (Playwright) in a later UI slice.

**Target Platform**: Linux container on Azure Container Apps (ADR-0017), reached only through the
YARP gateway/BFF (ADR-0013/0018). The identity API is internal, not public.

**Project Type**: backend microservice (the `identity` service, one of three) — Clean Architecture
layers `domain` / `application` / `infrastructure` + an ASP.NET Core host. The Angular login UI
(`@roomy/identity-feature-*`) is a later slice, not this plan's core.

**Performance Goals**: not latency-critical for the MVP; login p95 < 300 ms is ample.

**Constraints**: no tokens in the SPA (BFF, ADR-0013); authentication failures MUST be generic
(no account-existence disclosure); password minimum length 8, no complexity rules (enforced in
Keycloak's password policy); account email unique across the system.

**Scale/Scope**: single-tenant, one seeded company, small user count (tens–hundreds). Six user
stories IA-1…IA-6 (#26–#31).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Verdict | Notes |
|---|---|---|
| I. Spec-Driven & Test-First | ✅ | Spec exists with testable AC (IA-1…6); tests precede code in Phase Red. |
| II. Clean Architecture & DDD | ✅ | `User` aggregate (account + role) in `identity`; layers enforced by NetArchTest. |
| III. Context Isolation — IDs & integration events | ✅ | Identity owns its DB; emits `UserRegistered` / `UserLoggedIn`; `RegisterUser` is invoked by the organization-led provisioning saga (ADR-0025), by `UserId` only. |
| IV. No Framework in the Core | ✅ | Keycloak/EF/Wolverine adapters live only in `infrastructure`/composition root; `domain`/`application` stay framework-free. |
| V. Decisions Are Recorded (ADR-before-code) | ✅ | Auth conflict resolved in favour of **ADR-0013** (Keycloak); the spec was amended to conform. Provisioning recorded in **ADR-0025** (now Accepted). No undocumented decisions remain. |
| VI. Green Before Done | ✅ | Standard gates apply. |
| VII. Small, Single-Purpose Changes | ✅ | One story per branch; this slice is the identity service. |

**Gate: PASS.** Both prior blockers are resolved — authentication is Keycloak (ADR-0013, spec
amended) and provisioning is the organization-led saga (ADR-0025, Accepted; spec scenario 4 /
FR-006 amended to eventual login). Phase 0/1 design proceeds.

## Project Structure

### Documentation (this feature)

```text
specs/001-identity-access/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (internal API + integration-event contracts)
└── tasks.md             # Phase 2 output (/speckit-tasks — not created here)
```

### Source Code (repository root)

```text
apps/
└─ identity-api/                     # ASP.NET Core host (composition root: EF Core, Wolverine, Keycloak adapter)

libs/
└─ identity/
   ├─ domain/                        # User aggregate, Email/Role value objects, domain events  (no infra deps)
   ├─ application/                   # RegisterUser / RecordLogin use cases, ports (IUserRepository, IIdentityProviderPort, IIntegrationEventPublisher)
   └─ infrastructure/               # EF Core persistence, Keycloak Admin adapter, integration-event publishing

tests/
├─ architecture/                     # NetArchTest dependency-rule + no-MediatR rules
└─ identity/                         # unit + integration (Testcontainers: Postgres + Keycloak)
```

**Structure Decision**: One bounded-context service (`identity`) in three Clean Architecture
layers plus an ASP.NET Core host, matching `CLAUDE.md` and ADR-0003/0014. Login itself is handled
by Keycloak + the YARP BFF (ADR-0013), so this service exposes a small account/role-management
surface and a Keycloak provisioning adapter rather than a custom login endpoint. The Angular login
UI (`@roomy/identity-feature-*`) is deferred to a follow-up slice once contracts exist.

## Complexity Tracking

> No complexity exceptions requested.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
