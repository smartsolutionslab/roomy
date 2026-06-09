# Implementation Plan: Hire Employee

**Branch**: `feat/008-hire-employee` | **Date**: 2026-06-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-hire-employee/spec.md`

## Summary

An administrator hires a colleague; the employee is recorded immediately in a **provisioning** state and
their login account is provisioned in the background, becoming usable once provisioning completes
(eventual consistency, ADR-0025). This builds the **organization side** of the provisioning saga and
completes the round-trip — the **identity side already exists and is dormant** (`EmployeeHired` consumer
→ `RegisterUser` → publishes `UserRegistered` / `UserProvisioningFailed`), waiting only for a publisher.

The work: a new **`Employee` aggregate** in organization (state-based, EF Core — mirrors `Office`/`Room`)
with a provisioning state machine; a **`HireEmployee`** use case that persists the employee and raises an
`EmployeeHired` **domain event**, which the existing unit-of-work drains to the **transactional outbox**
(ADR-0037) as the `EmployeeHired` **integration event** (the contract already exists); two **inbox
consumers** that map identity's `UserRegistered` / `UserProvisioningFailed` back to internal commands
(`CompleteProvisioning` → *active*, `FailProvisioning` → *provisioning failed*, the compensation); and a
**`POST /employees`** admin-only endpoint. Organization-api, today **publish-only**, becomes a
**consumer** too (joins the inbox; adds a consumer assembly to codegen). No new ADR — ADR-0025 governs.
See [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/),
[quickstart.md](./quickstart.md).

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: EF Core (Npgsql) on PostgreSQL (organization's own DB, ADR-0014); Wolverine
transactional **outbox + inbox** over RabbitMQ (ADR-0005/0015); the owned
`ICommandHandler`/`Result`/`Ensure`/`IUnitOfWork` abstractions (no MediatR, no framework in core); the
state-based `Aggregate` base that records domain events (ADR-0032) and the commit-time drain (ADR-0037)
**Storage**: PostgreSQL — a new `Employees` table in organization's database (state-based, not
event-sourced); the identity and attendance read models are untouched
**Testing**: xUnit v3 + Shouldly (domain/unit; application with fakes), integration against real Postgres
+ RabbitMQ via the sibling Aspire test host (the saga round-trip), WebApplicationFactory (API/contract),
NetArchTest (architecture)
**Target Platform**: Linux server (containerised), composed locally by Aspire
**Project Type**: Backend bounded-context feature — `libs/organization/{domain,application,infrastructure}`
+ `apps/organization-api`; consumes `libs/identity/contracts`; publishes via `libs/organization/contracts`
**Performance Goals**: v1 single-tenant, low hire volume — correctness and the no-half-account guarantee
first; provisioning convergence in seconds under normal operation (SC-002)
**Constraints**: eventual consistency (no distributed transaction, ADR-0014); cross-context only by ID +
integration events (ADR-0031) — organization never reads identity's DB; no framework in domain/application
(ADR-0005); initial password is a transient secret, never persisted (FR-009); admin-only (FR-001); BFF —
no tokens in the SPA (ADR-0013); warnings-as-errors; no suppressions
**Scale/Scope**: 1 new aggregate (`Employee`) + 1 hire use case + 2 ack use cases/consumers + 1 endpoint;
publishes 1 existing contract (`EmployeeHired`), consumes 2 existing identity contracts; 1 migration; 3
prioritized stories; the identity half is already implemented

## Constitution Check

*GATE: must pass before Phase 0 and re-checked after Phase 1.*

| Principle | Status | How this plan satisfies it |
|---|---|---|
| I. Spec-Driven & Test-First | ✅ | Every scenario (US1–US3) + edges becomes a failing test first (quickstart §1–4); aggregate state machine, drain, and compensation are red→green. |
| II. Clean Architecture & DDD | ✅ | `Employee` is an aggregate root (consistency boundary) with behaviour (`Hire`/`CompleteProvisioning`/`FailProvisioning`) and value objects; `domain`→`application`→`infrastructure`→`app`. Mirrors `Office`. |
| III. Context Isolation — IDs & Events | ✅ | Organization publishes `EmployeeHired`; consumes identity's `UserRegistered`/`UserProvisioningFailed` from `libs/identity/contracts` (`context:shared`), each mapped to an **internal command at the infra edge** so `application` never sees a foreign contract (ADR-0031). 1:1 link by `UserId`/`EmployeeId`; no cross-DB access. |
| IV. No Framework in Core | ✅ | Domain/application use owned abstractions only; Wolverine inbox/outbox + EF wired at the composition root; the domain→integration map and command-mapping consumers live in `infrastructure`. |
| V. Decisions Recorded (ADR-before-code) | ✅ | **ADR-0025 (Accepted)** already decides the organization-led saga, eventual consistency, and compensation; this feature **realizes its follow-ups** (define the contracts — done — and the compensating action — here). No new ADR. Reuses ADR-0031/0032/0037. |
| VI. Green Before Done — No Suppressions | ✅ | Full gate suite incl. organization OpenAPI drift gate + Wolverine codegen-verify (organization gains a consumer); no suppressions. |
| VII. Small, Single-Purpose Changes | ✅ | One story on `feat/008-hire-employee`; atomic commits grouped by US (hire → acks/compensation → endpoint). |

**Gate result:** PASS. ADR-0025 governs; no unjustified complexity (Complexity Tracking empty).

## Project Structure

### Documentation (this feature)

```text
specs/008-hire-employee/
├── plan.md          # this file
├── research.md      # Phase 0 — decisions R1–R6
├── data-model.md    # Phase 1 — Employee aggregate, state machine, mapping
├── quickstart.md    # Phase 1 — layered validation incl. the saga round-trip
├── contracts/       # Phase 1 — organization-api.md, integration-events.md
└── tasks.md         # Phase 2 — /speckit-tasks (NOT created here)
```

### Source Code (repository root)

```text
libs/
  organization/
    domain/Employees/                     # NEW aggregate (Roomy.Organization.Domain, type:domain)
      Employee.cs                          # aggregate root: Hire / CompleteProvisioning / FailProvisioning
      EmployeeIdentifier.cs UserIdentifier.cs   # branded ids (UserId pre-allocated, the saga correlation key)
      WorkEmail.cs EmployeeName.cs EmployeeRole.cs  # value objects (no primitive obsession)
      ProvisioningState.cs ProvisioningFailureReason.cs  # state + reason
      EmployeeHired.cs                     # intra-context domain event (carries the VOs + transient password)
      IEmployeeRepository.cs
    application/                           # Roomy.Organization.Application (type:application)
      UseCases/
        HireEmployee.cs HireEmployeeHandler.cs           # admin hires → Employee.Hire → unit of work
        CompleteEmployeeProvisioning.cs ...Handler.cs    # UserRegistered ack → Active
        FailEmployeeProvisioning.cs ...Handler.cs        # UserProvisioningFailed ack → Failed (compensation)
    infrastructure/                        # Roomy.Organization.Infrastructure (type:infrastructure)
      Messaging/
        OrganizationIntegrationEventMap.cs # EXTEND — EmployeeHired domain event → EmployeeHired contract
        UserRegisteredConsumer.cs          # NEW — identity contract → CompleteEmployeeProvisioning
        UserProvisioningFailedConsumer.cs  # NEW — identity contract → FailEmployeeProvisioning
      Persistence/
        EmployeeConfiguration.cs EmployeeRepository.cs   # EF mapping + repo (mirrors Office)
        OrganizationDbContext.cs           # EXTEND — Employees DbSet + config
        Migrations/                        # NEW — Employees table
      OrganizationInfrastructureServiceCollectionExtensions.cs  # EXTEND — register repo + 3 handlers
  organization/contracts/EmployeeHired.cs  # EXISTS (organization's published language) — unchanged
  identity/contracts/                      # EXISTS — UserRegistered/UserProvisioningFailed consumed here
apps/
  organization-api/
    Endpoints/EmployeeEndpoints.cs         # NEW — POST /employees (admin-only)
    Program.cs                             # EXTEND — scan the consumer assembly (inbox) alongside publish
    Internal/Generated/WolverineHandlers/  # regenerated — 2 new consumer handlers
    Roomy.Organization.Api.json            # re-emitted OpenAPI (POST /employees)
  gateway/appsettings.json                 # NEW /employees route → organization cluster
tests/
  organization/                            # Employee domain + application (state machine, compensation)
  organization-integration/                # saga round-trip + consumers + endpoint (real Postgres/RabbitMQ)
  architecture/                            # confirm organization layers stay within the dependency rule
```

**Structure Decision**: Keep the hiring side wholly inside **organization** (ADR-0025): the `Employee`
aggregate mirrors `Office` (state-based EF Core, `Aggregate` base that records domain events), the hire
use case commits through the existing `IUnitOfWork` whose drain already publishes via the outbox
(ADR-0037), and the two ack-consumers mirror the inbound-mapping pattern used by identity/attendance. The
only structural shift is that **organization-api becomes a consumer** (it joins the durable inbox and
adds a handler assembly to the committed Wolverine codegen), in addition to its existing publisher role.

## Dependency & sequencing note (important)

The identity half is **already on `main`** (`RegisterUser`, the `EmployeeHired` consumer, and the
`UserRegistered`/`UserProvisioningFailed` contracts), so the saga completes the moment organization
publishes `EmployeeHired`. Recommended order:

1. **Employee aggregate + `HireEmployee`** (domain + application), publishing `EmployeeHired` via the
   drain — testable in isolation (the domain tests pass role/email/password explicitly; the publish is an
   outbox integration test). This alone makes a hired colleague provisionable end-to-end (US1).
2. **Ack-consumers + compensation** (`UserRegistered` → active, `UserProvisioningFailed` → failed),
   making organization a consumer (inbox + codegen) — US2/US3.
3. **`POST /employees`** endpoint + gateway route + OpenAPI re-emit.

US1's write side does not depend on the consumers; the consumers do not depend on the endpoint. The
full round-trip (hire → provisioned/active, and hire-with-taken-email → failed) is the headline
integration test (quickstart §3).

## Complexity Tracking

*No entries — no constitution violations to justify. The new state (one `Employee` aggregate + its
provisioning state machine) and organization-api gaining a consumer role are required by ADR-0025's
saga, not speculative generality. The integration-event contracts already exist; the compensating action
is the spec's core guarantee, not optional.*
