---
description: "Task list for Office & Room Management (002)"
---

# Tasks: Office & Room Management

**Input**: Design documents in `specs/002-office-management/` (plan.md, spec.md, research.md, data-model.md, contracts/)

**Tests**: REQUIRED. Constitution Principle I and `CLAUDE.md` golden rule 1 mandate test-first — every story phase writes failing tests before implementation.

**Organization**: Grouped by user story so each is independently implementable and testable.

## Story label map (priority order)

| Label | Story | Acceptance scenarios | Priority |
|---|---|---|---|
| US1 | Create an office | 1, 7 (authz), edge: office-name unique | P1 (MVP) |
| US2 | Add a room to an office | 2, 6 (capacity ≥ 1), 7, edge: room-name unique | P1 (MVP) |
| US3 | Rename an office | 3 | P2 |
| US4 | Change an office's location | 4 | P2 |
| US5 | Rename a room | 5 | P2 |

## Format: `[ID] [P?] [Story] Description with file path`

- **[P]**: parallelizable (different files, no incomplete-task dependency)
- Paths follow plan.md: `apps/organization-api/`, `libs/organization/{domain,application,infrastructure}/`, `tests/`

---

## Phase 1: Setup (Shared Infrastructure)

- [x] T001 Create the organization context structure — `libs/organization/{domain,application,infrastructure}` csproj (root namespaces `SmartSolutionsLab.Roomy.Organization.{Domain,Application,Infrastructure}`, mirroring identity's project references: domain→shared-kernel; application→domain+shared-kernel+application-contracts; infrastructure→application+domain+shared-kernel+infrastructure-persistence), host `apps/organization-api` (`Roomy.Organization.Api`, refs service-defaults + infrastructure; JwtBearer + EF Design packages; **no** messaging project ref), and `tests/organization` + `tests/organization-integration` + `tests/organization-integration-apphost`. Add all to `Roomy.slnx`.
- [x] T002 [P] Wire boundary enforcement: add the organization `domain`/`application`/`infrastructure` ProjectReferences to `tests/architecture/.../Roomy.ArchitectureTests.csproj` so `LayerDependencyConventionTests` + `NoMediatRTests` actually inspect them (otherwise they pass vacuously — see `tests/architecture/README.md`).
- [x] T003 [P] Confirm `Directory.Build.props` inheritance for the new projects (net10.0, nullable on, warnings-as-errors); mark EF migrations as generated in `.editorconfig` (pattern already present for identity).

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

- [x] T004 [P] Failing unit tests for value objects in `tests/organization/Domain/ValueObjects/` — identifiers (`CompanyIdentifier`/`OfficeIdentifier`/`RoomIdentifier`: GUIDv7, reject empty, implicit conversions), `CompanyName`/`OfficeName`/`Location`/`RoomName` (non-empty, trimmed, equality), `Capacity` (≥ 1, rejects 0/negative). RED-verify before T005.
- [x] T005 [P] Value objects implemented in `libs/organization/domain/**` (folder per aggregate), invariants via `Ensure.That(...)`. T004 green.
- [x] T006 Failing unit tests for the `Office` aggregate in `tests/organization/Domain/Offices/OfficeTests.cs` — `Create` starts with no rooms; `Rename`/`RelocateTo`; `AddRoom` appends and returns the room, rejects a duplicate name (`Error.Conflict`); `Capacity` equals the sum of room capacities; `RenameRoom` renames, `Error.NotFound` for an unknown room, `Error.Conflict` for a duplicate. Plus a `Company` test (`Create`). RED.
- [x] T007 `Office` aggregate (+ `Room` entity) and `Company` implemented in `libs/organization/domain/{Offices,Companies}/`, with repository ports `IOfficeRepository` (`GetByIdentifierAsync → Result<Office>`, `ExistsByNameAsync(company,name) → bool`, `GetAllAsync`, `AddAsync`) and `ICompanyRepository` (`ExistsAsync`, `AddAsync`, `GetSeededAsync → Result<Company>`). No nullable returns. T006 green.
- [x] T008 EF Core persistence in `libs/organization/infrastructure/Persistence/` — `OrganizationDbContext : RoomyDbContext`, `CompanyConfiguration`/`OfficeConfiguration`/`RoomConfiguration` (value-object converters; `Office`→`Room` one-to-many mapped to the read-only backing field; `Office.Capacity` ignored; unique indexes `ux_offices_company_identifier_name`, `ux_rooms_office_identifier_name`), `OfficeRepository`, `CompanyRepository`, `OrganizationUnitOfWork`, `AddOrganizationPersistence`. Verified against **real PostgreSQL** via `tests/organization-integration` + `tests/organization-integration-apphost` (round-trip office with rooms, not-found, exists-by-name, both unique-index violations). `InitialCreate` migration generated via a design-time factory; integration tests `MigrateAsync`. Register `OrganizationDbContext` in the shared `db-migrator` (ADR-0033).

**Checkpoint**: Foundation ready — user stories can begin.

---

## Phase 3: US1 — Create an office (P1, MVP)

**Independent Test**: an admin `POST /offices {name, location}` → `201`; a second office with the same name → `409`; an employee → `403`; empty name → `400`.

- [x] T009 [US1] Use-case test `tests/organization/Features/CreateOfficeTests.cs` — success persists an office under the seeded company; duplicate name → `Error.Conflict` and nothing persisted (in-memory port doubles). RED.
- [x] T010 [US1] `CreateOffice` command + `CreateOfficeHandler` in `libs/organization/application/UseCases/` — resolve the seeded company, `ExistsByNameAsync` guard → conflict, else `Office.Create` + `AddAsync` + `IUnitOfWork.SaveChangesAsync`. Bound via `AddOrganizationUseCases`. T009 green.
- [x] T011 [US1] HTTP test `tests/organization-integration/OfficeEndpointsTests.cs` (in-process host, real Postgres, `TestAuthHandler`) — admin create → `201`; employee → `403`; no session → `401`; duplicate → `409`; empty name → `400`. RED.
- [x] T012 [US1] `POST /offices` + `GET /offices` + `GET /offices/{id}` in `apps/organization-api/Endpoints/OfficeEndpoints.cs` (+ `OfficeResponse`/`RoomResponse`), writes `RequireRole("administrator")`, reads `RequireAuthorization`. Host `Program.cs` wires `AddOrganizationPersistence`, JWT bearer + `KeycloakRealmRoles` flatten (mirrored from identity-api), `AddOrganizationUseCases`, and the `Company` seeder (`CompanySeeder` + `CompanySeederHostedService`, idempotent). T011 green.

**Checkpoint**: Offices can be created and listed; authz holds.

---

## Phase 4: US2 — Add a room (P1, MVP)

**Independent Test**: admin `POST /offices/{id}/rooms {name, capacity}` → `201`, room appears in the office and contributes to its capacity; capacity 0 → `400`; duplicate room name → `409`; employee → `403`.

- [x] T013 [US2] Use-case test `tests/organization/Features/AddRoomTests.cs` — success appends a room and persists; duplicate name → `Error.Conflict`; unknown office → `Error.NotFound`. RED. (Capacity ≥ 1 already covered by the `Capacity` value-object tests, T004.)
- [x] T014 [US2] `AddRoom` command + handler in `libs/organization/application/UseCases/` — load office (`Error.NotFound`), `Office.AddRoom` (returns `Result<Room>`), save. T013 green.
- [x] T015 [US2] HTTP test in `OfficeEndpointsTests.cs` — admin add room → `201` + room reflected in `GET /offices/{id}` capacity; capacity 0 → `400`; duplicate → `409`; employee → `403`. RED.
- [x] T016 [US2] `POST /offices/{officeId}/rooms` endpoint. T015 green.

**Checkpoint**: Rooms can be added; office capacity is the derived sum.

---

## Phase 5: US3 — Rename an office (P2)

- [x] T017 [US3] Use-case + HTTP tests — rename success; duplicate name → `409`; unknown office → `404`. RED.
- [x] T018 [US3] `RenameOffice` command + handler (`ExistsByNameAsync` guard, ignoring the office's own current name) and `PATCH /offices/{officeId}/name`. Green.

---

## Phase 6: US4 — Change an office's location (P2)

- [x] T019 [US4] Use-case + HTTP tests — relocate success; empty location → `400`; unknown office → `404`. RED.
- [x] T020 [US4] `ChangeOfficeLocation` command + handler and `PATCH /offices/{officeId}/location`. Green.

---

## Phase 7: US5 — Rename a room (P2)

- [x] T021 [US5] Use-case + HTTP tests — rename success; duplicate within office → `409`; unknown room/office → `404`. RED.
- [x] T022 [US5] `RenameRoom` command + handler (delegates to `Office.RenameRoom`) and `PATCH /offices/{officeId}/rooms/{roomId}/name`. Green.

---

## Phase 8: End-to-end wiring & polish

- [ ] T023 Wire `organization-api` into the Aspire app host (`apps/apphost/AppHost.cs`) — add the `organization` database, the `db-migrator` reference to it, the `organization-api` project gated `WaitForCompletion(db-migrator)` + Keycloak reference/config, and a gateway YARP route `/offices/{**}` → the `organization` cluster (default policy + token forwarding). Extend the app-host model test (`tests/apphost/`) to assert the wiring. Live browser/e2e is deferred (mirrors identity's deferral).
- [ ] T024 [P] Produce the OpenAPI document for the organization surface (the typed Angular client + an admin UI are a later frontend slice).

---

## Dependencies & Execution Order

- **Setup (T001–T003)** → **Foundational (T004–T008)** blocks everything → **stories**.
- **US1** is the MVP and depends only on Foundational. **US2** depends on US1 (an office to add rooms to). **US3/US4/US5** depend on Foundational + US1; they are independent of each other (`[P]` once US1 lands).
- Within each story: **tests first (must FAIL)** → domain/handler → endpoint → integration. Commit after each task or logical group.

## Notes

- Tests must fail before implementation (verify RED).
- No new ADR is required (see plan.md Constitution Check).
- This slice publishes **no** integration events and wires **no** Wolverine (research.md D4).
