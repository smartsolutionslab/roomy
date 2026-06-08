---
description: "Task list for Identity & Access (001)"
---

# Tasks: Identity & Access

**Input**: Design documents in `specs/001-identity-access/` (plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md)

**Tests**: REQUIRED. The constitution (Principle I) and `CLAUDE.md` golden rule 1 mandate test-first — every story phase writes failing tests before implementation.

**Organization**: Grouped by user story so each is independently implementable and testable.

## Story label map (priority order)

| Label | Story | Issue | Priority |
|---|---|---|---|
| US1 | IA-1 Administrator login | #26 | P1 (MVP) |
| US2 | IA-6 Log out | #31 | P2 |
| US3 | IA-3 Provision employee accounts | #28 | P2 |
| US4 | IA-4 Provision / elevate administrators | #29 | P2 |
| US5 | IA-2 Employee login | #27 | P2 |
| US6 | IA-5 Administrator is also an employee | #30 | P3 |

## Format: `[ID] [P?] [Story] Description with file path`

- **[P]**: parallelizable (different files, no incomplete-task dependency)
- Paths follow plan.md: `apps/identity-api/`, `libs/identity/{domain,application,infrastructure}/`, `tests/`

> **External dependencies (not blockers for design/unit work, but for green e2e):** login (US1/US2/US5) needs the Aspire app host + Keycloak + YARP BFF (setup issues #17, #21); provisioning (US3/US5) needs the `organization` context to emit `EmployeeHired` (#32-style work) — tested here by publishing the event directly.

---

## Phase 1: Setup (Shared Infrastructure)

- [x] T001 Create the identity context structure — `libs/identity/domain`, `libs/identity/application`, `libs/identity/infrastructure`, host `apps/identity-api`, and the `tests/architecture` wiring — added to `Roomy.slnx`. (`tests/identity` is created in the first Phase 2 slice alongside its initial tests, to avoid an empty test project.)
- [x] T002 [P] Boundary enforcement for `identity`: the .NET side is the convention-based architecture tests, which now load and inspect the identity `domain`/`application`/`infrastructure` assemblies (referenced from `Roomy.ArchitectureTests`). The Nx/`eslint` boundary rules cover the JS/TS frontend libs only; the `context:identity` tag is added when the first identity feature lib lands.
- [x] T003 [P] `Directory.Build.props` inheritance confirmed for the new projects (net10.0, nullable on, warnings-as-errors); per-project root namespace `SmartSolutionsLab.Roomy.Identity.*` set in each csproj.

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T004 [P] Architecture rules for `identity`: covered by the existing convention-based suite (`LayerDependencyConventionTests`, `NoMediatRTests`), which now **actively enforces** the identity `domain` (no longer dormant) since the value objects exist — domain depends on nothing but the shared kernel, no framework/MediatR. No separate `IdentityArchitectureTests.cs` is needed.
- [x] T005 [P] Failing unit tests for value objects in `tests/identity/Domain/ValueObjects/` — `UserId`, `KeycloakSubjectId`, `Email` (normalized/validated/equality), `DisplayName`, `Role` (Employee base + Administrator elevation). `UserStatus` is a closed enum with no behaviour to unit-test (its `Provisioning → Active` transition is an aggregate invariant, T007-T008). 24 tests, RED-verified before T006.
- [x] T006 [P] Value objects implemented in `libs/identity/domain/ValueObjects/` (invariants via `Ensure.That(...)`); branded-GUID ids, normalized `Email`, `Role` elevation. T005 green.
- [x] T007 Failing unit tests for the `User` aggregate in `tests/identity/Domain/UserTests.cs` — every user holds Employee (incl. administrators), `Register` starts in `Provisioning` with no Keycloak link, `Activate` transitions `Provisioning → Active` and links the subject (and rejects a non-provisioning account). Email *uniqueness* is a set-level invariant enforced by persistence (unique index, T010) — the aggregate carries a valid `Email` value object. 5 tests, RED-verified.
- [x] T008 `User` aggregate implemented in `libs/identity/domain/User.cs` — `Register` factory (Provisioning), `Activate` lifecycle, `IsEmployee`/`IsAdministrator`. No story behaviours or domain events yet (those land with the use cases). T007 green.
- [x] T009 Application ports in `libs/identity/application/` — `IUserRepository` (persistence port; `FindByEmail` backs the uniqueness guard) and `IIdentityProviderPort` (Keycloak provisioning; failures returned as a `Result`). The **owned dispatch** abstractions (`ICommand`/`ICommandHandler`/`ICommandDispatcher`/`IQuery*`) and `IIntegrationEventPublisher` already exist as shared contracts in `application-contracts` (ADR-0005, no MediatR); identity reuses them — referenced when the use cases define commands (T015/T021). Pure contracts (no behaviour to unit-test); the architecture conventions now verify the identity `application` layer is clean.
- [x] T010 EF Core persistence in `libs/identity/infrastructure/Persistence/` — `IdentityDbContext` (derives the shared `RoomyDbContext` baseline), `UserConfiguration` (value-object converters; unique indexes on `Email` and `KeycloakSubjectIdentifier`), and `UserRepository`. Per the new *no-nullable-returns* rule, the port is `GetByIdentifierAsync → Result<User>` (`Error.NotFound`) + `ExistsByEmailAsync → bool` (was nullable `FindBy…`). Verified against **real PostgreSQL via a minimal Aspire test app host** (`tests/identity-integration/` + `tests/identity-integration-apphost/`, 7 tests: round-trip of provisioning/activated accounts, not-found, exists, and both unique-index violations) — not SQLite, per the decision to exercise the real provider. The initial **migration is deferred to T012** (it needs the host's design-time wiring); the integration test creates the schema with `EnsureCreated`. A dedicated integration project (not `tests/identity/Infrastructure/`) keeps the domain unit tests fast and Docker-free.
- [ ] T011 Implement the Keycloak admin adapter (`IIdentityProviderPort`) in `libs/identity/infrastructure/Keycloak/` — create user, set initial password, assign realm role; integration test (Testcontainers Keycloak) in `tests/identity/Infrastructure/KeycloakAdapterTests.cs`, failing first.
- [ ] T012 Compose the host `apps/identity-api/Program.cs` — wire EF Core, the Keycloak adapter, and Wolverine (transactional outbox, ADR-0005/0012) at the composition root only; add a health endpoint.

**Checkpoint**: Foundation ready — user stories can begin.

---

## Phase 3: User Story 1 — Administrator login (US1 / IA-1, #26) 🎯 MVP (P1)

**Goal**: A seeded `DefaultAdmin` can log in through the BFF and is recognised as an administrator.

**Independent Test**: With an empty DB, start the service; log in via the BFF with configured DefaultAdmin credentials → `GET /account/me` returns `role: administrator`; wrong password and unknown email both return the same generic failure.

- [ ] T013 [P] [US1] Integration test in `tests/identity/Features/DefaultAdminSeedingTests.cs` — seeding is idempotent and creates the Keycloak admin user + account record. (RED)
- [ ] T014 [P] [US1] Integration test in `tests/identity/Features/AccountMeTests.cs` — authenticated admin → `GET /account/me` = `administrator`; invalid credentials → single generic failure (FR-008). (RED)
- [ ] T015 [US1] Implement the DefaultAdmin seeding hosted service in `apps/identity-api/Seeding/DefaultAdminSeeder.cs` — read email + initial password from configuration, provision via `IIdentityProviderPort`, persist the `User`.
- [ ] T016 [US1] Implement `GET /account/me` in `apps/identity-api/Endpoints/AccountEndpoints.cs` returning the account/role projection.
- [ ] T017 [US1] Configure the Keycloak realm (roles `employee`/`administrator`, unique-email, password min-length 8) and the YARP BFF OIDC login route (gateway client) — realm import under `apps/identity-api/keycloak/` and gateway config.

**Checkpoint**: Admin login MVP works end-to-end.

---

## Phase 4: User Story 2 — Log out (US2 / IA-6, #31) (P2)

**Goal**: A logged-in user can end their session.

**Independent Test**: After login, POST the BFF logout route → subsequent `GET /account/me` returns `401`.

- [ ] T018 [P] [US2] Integration test in `tests/identity/Features/LogoutTests.cs` — logout clears the session + Keycloak end-session; `GET /account/me` then returns `401`. (RED)
- [ ] T019 [US2] Implement the BFF logout route (clear session + OIDC RP-initiated logout) in the gateway config and `apps/identity-api/Endpoints/SessionEndpoints.cs` as needed.

**Checkpoint**: Login + logout both work.

---

## Phase 5: User Story 3 — Provision employee accounts (US3 / IA-3, #28) (P2)

**Goal**: Hiring an employee provisions a usable account (eventual), per ADR-0025.

**Independent Test**: Publish `EmployeeHired` (role `employee`, password ≥ 8) → `UserRegistered` emitted and the employee can log in; password < 8 → `UserProvisioningFailed(password_rejected)`, no login; duplicate email → `UserProvisioningFailed(email_taken)`.

- [ ] T020 [P] [US3] Integration test in `tests/identity/Features/ProvisioningTests.cs` — success path emits `UserRegistered`; short password and duplicate email emit the correct `UserProvisioningFailed` reasons; no account created on failure. (RED)
- [ ] T021 [US3] Implement the `RegisterUser` use case in `libs/identity/application/UseCases/RegisterUser.cs` — provision Keycloak user, persist `User` as Active, publish events.
- [ ] T022 [US3] Wire the integration-event contracts in `libs/identity/infrastructure/Messaging/` — consume `EmployeeHired`, publish `UserRegistered` / `UserProvisioningFailed` via the outbox (per `contracts/integration-events.md`).

**Checkpoint**: Employees can be provisioned and become loginable.

---

## Phase 6: User Story 4 — Provision / elevate administrators (US4 / IA-4, #29) (P2)

**Goal**: Accounts can be created as, or elevated to, Administrator; only admins manage accounts.

**Independent Test**: Provision with role `administrator` → admin capabilities; `POST /admin/users/{id}:grant-administrator` elevates an existing employee (idempotent) and raises `AdministratorGranted`; a non-admin calling `/admin/*` gets `403` (FR-007).

- [ ] T023 [P] [US4] Tests in `tests/identity/Features/AdminManagementTests.cs` — elevation raises `AdministratorGranted` and is idempotent; `/admin/users` + `/admin/users/{id}` return data for admins and `403` for employees. (RED)
- [ ] T024 [US4] Implement `GrantAdministrator` behaviour + `AdministratorGranted` event on `libs/identity/domain/User.cs`.
- [ ] T025 [US4] Implement `POST /admin/users/{userId}:grant-administrator` in `apps/identity-api/Endpoints/AdminUserEndpoints.cs`, syncing the role to Keycloak via `IIdentityProviderPort`.
- [ ] T026 [US4] Implement `GET /admin/users` and `GET /admin/users/{userId}` (administrator-only authorization) in `apps/identity-api/Endpoints/AdminUserEndpoints.cs`.

**Checkpoint**: Admin management surface complete.

---

## Phase 7: User Story 5 — Employee login (US5 / IA-2, #27) (P2)

**Goal**: A provisioned employee can log in and is scoped to employee capabilities.

**Independent Test**: A provisioned employee logs in via the BFF → `GET /account/me` = `employee`; calling `/admin/users` → `403`.

- [ ] T027 [P] [US5] Integration test in `tests/identity/Features/EmployeeLoginTests.cs` — employee login → `role: employee`; `/admin/*` → `403`. (RED)
- [ ] T028 [US5] Ensure the `employee` role claim maps through Keycloak → token → BFF authorization; add any missing role-claim mapping in the gateway/realm config. (Login infra reused from US1.)

**Checkpoint**: Both roles can log in with correct capabilities.

---

## Phase 8: User Story 6 — Administrator is also an employee (US6 / IA-5, #30) (P3)

**Goal**: Every account, including administrators, holds the Employee role and can plan attendance.

**Independent Test**: An admin's `GET /account/me` exposes the employee capability; the admin is a valid attendance subject (contract-level here; full behaviour in `003`).

- [ ] T029 [P] [US6] Unit test in `tests/identity/Domain/UserTests.cs` — constructing any `User` (incl. administrator) always carries the Employee role; an admin is a valid reservation subject at the contract level. (RED)
- [ ] T030 [US6] Confirm the aggregate construction guarantees the Employee role for every account (adjust `User` if needed); no separate "admin-only" account path exists.

**Checkpoint**: Role model holds for all accounts.

---

## Phase N: Polish & Cross-Cutting

- [ ] T031 [P] Produce the OpenAPI document for the identity internal surface and generate the typed Angular client (ADR-0018) in the web app's generated-client location.
- [ ] T032 [P] Add coverage + mutation thresholds for `identity` per `docs/testing-strategy.md`.
- [ ] T033 Run the `quickstart.md` scenarios end-to-end via the Aspire app host once #17/#21 land.

---

## Dependencies & Execution Order

- **Setup (P1)** → **Foundational (P2)** blocks everything → **User stories (P3+)**.
- **US1 (P1, MVP)** depends only on Foundational. **US2** depends on US1's login infra. **US3** depends on Foundational + messaging. **US4** depends on Foundational (+US3 for the elevate-existing case). **US5** depends on US1 (login) + US3 (a provisioned employee). **US6** depends on Foundational only.
- Within each story: **tests first (must FAIL)** → models → services → endpoints → integration. Commit after each task or logical group.

## Parallel Opportunities

- T002/T003 (Setup); T004/T005/T006 (Foundational value-object + arch tests); the `[P]` test tasks at the head of each story phase.
- After Foundational, US1, US3, US4, and US6 can be progressed in parallel; US2 and US5 follow their prerequisites.

## Implementation Strategy

1. Setup + Foundational → foundation ready.
2. **US1 only → STOP and validate** (DefaultAdmin admin login) = the identity MVP / walking-skeleton contribution.
3. Add US2, US3, US4, US5, US6 incrementally, each independently tested, none breaking the previous.

## Notes

- Tests must fail before implementation (constitution Principle I; verify RED).
- `[P]` = different files, no incomplete dependency. Avoid same-file conflicts.
- Login/logout (US1/US2/US5) cannot go fully green until the Aspire host + Keycloak + YARP BFF exist (#17, #21); design and the non-HTTP units can proceed now.
