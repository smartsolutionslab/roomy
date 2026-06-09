# Implementation Plan: Office & Room Management

**Branch**: `feat/002-organization-office-room` | **Date**: 2026-06-09 | **Spec**: `specs/002-office-management/spec.md`

**Input**: Feature specification from `/specs/002-office-management/spec.md`

## Summary

The **organization** context (supporting subdomain, admin-managed master data) lets an
administrator create offices and the rooms inside them. An `Office` belongs to the single
seeded `Company`, has a name and a location, and contains one or more `Room`s. Capacity lives
on the **room** (a positive whole number, fixed at creation in the MVP); an office's capacity is
the **derived sum** of its rooms. Office names are unique within the company; room names are
unique within their office. All write operations require the **Administrator** role; reads are
available to any authenticated account (employees need to see offices/rooms to plan attendance
later). This is the first slice of a brand-new bounded-context service (`organization-api`) with
its own PostgreSQL database (ADR-0014).

**Explicitly out of this slice** (kept lean per *simplicity-first*): the `Employee` aggregate
and the `HireEmployee` → `EmployeeHired` provisioning saga (ADR-0025) — that is a separate spec;
its `EmployeeHired` contract already exists in `libs/organization/contracts` only because
identity's US3 consumes it. Office/Room management publishes **no** integration events yet
(occupancy's `OfficeOpened`/`RoomAdded` are introduced when the **attendance** context first
consumes them — "as late as the design allows", ADR-0005), so this service wires **no Wolverine
messaging** at all.

## Technical Context

**Language/Version**: C# / .NET 10 (root namespace `SmartSolutionsLab.Roomy`, file-scoped
namespaces, nullable on, warnings-as-errors, async-all-the-way with `CancellationToken`).

**Primary Dependencies**: ASP.NET Core host; EF Core on PostgreSQL (infrastructure only, deriving
the shared `RoomyDbContext` baseline, ADR-0012); owned application command/query abstractions from
`application-contracts` (no MediatR, ADR-0005). JWT-bearer validation of the BFF-forwarded Keycloak
access token for authorization (ADR-0013). **No messaging, no Keycloak Admin adapter** — this
context provisions nothing in Keycloak; it only *authorizes* against the forwarded token's roles.

**Storage**: PostgreSQL — the organization service's own database (`organization`), no shared DB
(ADR-0011/0014). Two tables: `offices` and `rooms` (rooms FK → offices), plus a single seeded
`companies` row.

**Testing**: xUnit v3 (unit + integration); NetArchTest dependency-rule + no-MediatR rules in
`tests/architecture` (the new organization layers MUST be referenced there or they enforce nothing
vacuously — see `tests/architecture/README.md`); EF mapping verified against **real PostgreSQL** via
a minimal Aspire test app host (mirroring `tests/identity-integration-apphost`); HTTP endpoints via
`WebApplicationFactory` with a `TestAuthHandler` standing in for the BFF-forwarded token; Shouldly
assertions.

**Target Platform**: Linux container on Azure Container Apps (ADR-0017), reached only through the
YARP gateway/BFF (ADR-0013/0018). The organization API is internal, not public.

**Project Type**: backend microservice (the `organization` service, one of three) — Clean
Architecture layers `domain` / `application` / `infrastructure` + an ASP.NET Core host.

**Performance Goals**: not latency-critical for the MVP; admin master-data CRUD.

**Constraints**: writes require the Administrator role (FR-009); office names unique within the
company, room names unique within their office (FR-010); room capacity ≥ 1, fixed at creation
(FR-006/FR-007); office capacity is derived, never set (FR-008).

**Scale/Scope**: single-tenant, one seeded company, a handful of offices/rooms. Backlog stories
US2 (create office) + US3 (edit office) plus room management.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Verdict | Notes |
|---|---|---|
| I. Spec-Driven & Test-First | ✅ | Spec exists with testable AC (scenarios 1–7, FR-001…010); each criterion becomes a failing test first. |
| II. Clean Architecture & DDD | ✅ | `Office` aggregate root **contains** its `Room` entities (the consistency boundary for room-name uniqueness + derived capacity); `Company` is a minimal seeded root. Layers enforced by NetArchTest. |
| III. Context Isolation — IDs & integration events | ✅ | Organization owns its DB. No cross-context references. **No events published this slice** (none consumed yet); when attendance needs capacity, `OfficeOpened`/`RoomAdded` land in `libs/organization/contracts` (ADR-0031) over the outbox. |
| IV. No Framework in the Core | ✅ | EF Core lives only in `infrastructure`; auth wiring only in the host. `domain`/`application` stay framework-free. |
| V. Decisions Are Recorded (ADR-before-code) | ✅ | No **new** ADR required: the three-service topology (ADR-0014), persistence baseline (ADR-0012), and BFF/JWT authorization (ADR-0013) already cover this slice. The aggregate-boundary and single-seeded-`Company` choices are recorded in `data-model.md`/`research.md` (standard modelling, not a contested structural decision). |
| VI. Green Before Done | ✅ | Standard gates apply (`dotnet build -warnaserror`, `dotnet test`, `dotnet format`, Nx affected lint). |
| VII. Small, Single-Purpose Changes | ✅ | One context, one branch; the saga/Employee piece is deliberately deferred to its own spec. |

**Gate: PASS.** No blockers. Phase 0/1 design proceeds.

## Project Structure

### Documentation (this feature)

```text
specs/002-office-management/
├── spec.md              # Pre-existing feature spec
├── plan.md              # This file
├── research.md          # Phase 0 output — decisions & rationale
├── data-model.md        # Phase 1 output — aggregates, value objects, persistence
├── contracts/           # Phase 1 output — internal HTTP API surface (+ note on deferred events)
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
apps/
└─ organization-api/                 # ASP.NET Core host (composition root: EF Core, JWT auth, endpoints, Company seeder)

libs/
└─ organization/
   ├─ domain/                        # Office aggregate (+ Room entity), Company, value objects, repository ports  (no infra deps)
   ├─ application/                   # Create/Rename office, Change location, Add/Rename room use cases; IUnitOfWork
   ├─ infrastructure/               # EF Core persistence (OrganizationDbContext, configurations, repository, migration)
   └─ contracts/                    # (exists) EmployeeHired/HiredRole — NOT touched by this slice

tests/
├─ architecture/                     # add organization domain/application/infrastructure ProjectReferences
├─ organization/                     # unit: domain + application (Shouldly, fast, Docker-free)
├─ organization-integration/         # EF round-trip (real Postgres) + HTTP endpoint tests (WebApplicationFactory)
└─ organization-integration-apphost/ # minimal Aspire app host: Postgres + `organization` database
```

**Structure Decision**: One bounded-context service (`organization`) in three Clean Architecture
layers plus an ASP.NET Core host, matching `CLAUDE.md` and ADR-0003/0014, mirroring the existing
`identity` service exactly. Reads are authenticated; writes require the Administrator role. No
messaging backbone is wired because nothing is published or consumed in this slice.

## Phase 0 — Research (decisions)

See `research.md`. Headlines:

- **Room is modelled *inside* the `Office` aggregate**, not as its own aggregate — the office is the
  consistency boundary for "room names unique within the office" and the derived capacity sum.
- **`Company` is a minimal seeded root** (Identifier + Name), seeded once at startup like identity's
  `DefaultAdmin`. `Office` references it by `CompanyIdentifier`. This honours the documented model
  (CLAUDE.md: "Company (seeded root)") and gives office-name uniqueness a real scope, without
  building company management the MVP doesn't need.
- **Office-name and room-name uniqueness are set-level invariants** enforced by unique indexes plus
  an `ExistsBy…` pre-check in the handler (mirroring identity's `Email` uniqueness), not by the
  aggregate alone.
- **No integration events** are published; the auth helper (Keycloak realm-role → `ClaimTypes.Role`
  flattening + JWT bearer) is **mirrored locally** in `organization-api` from `identity-api` rather
  than extracted to a shared lib, to keep this slice surgical and ADR-free (consolidation is a
  separate, later refactor).

## Phase 1 — Design

See `data-model.md` (aggregates, value objects, persistence mapping) and `contracts/` (the internal
HTTP surface). The Angular admin UI for offices/rooms is a later frontend slice, not this plan.

## Complexity Tracking

> No complexity exceptions requested.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
