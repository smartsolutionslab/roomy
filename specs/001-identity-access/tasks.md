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
- [x] T011 Keycloak admin adapter (`IIdentityProviderPort`) in `libs/identity/infrastructure/Keycloak/` — `KeycloakIdentityProvider` + `KeycloakAdminOptions`. Three admin REST calls (acquire admin token → create user with initial password → assign realm role(s)); returns the new `KeycloakSubjectIdentifier`. Expected business outcomes come back as a failed `Result` whose code matches the `UserProvisioningFailed` reasons — `email_taken` (Conflict, HTTP 409), `password_rejected` (Validation, HTTP 400), `provider_error` (default) — never thrown; only transport faults throw. Partial-failure compensation deletes the just-created user if role assignment fails. Verified against **real Keycloak** with the production realm import via a minimal Aspire test app host (`tests/identity-keycloak-apphost/`); 4 integration tests in `tests/identity-integration/KeycloakIdentityProviderTests.cs` (employee + administrator role assignment confirmed by reading back the realm role-mappings, duplicate-email, short-password), RED-verified before implementation. Composition-root DI wiring is **T012**.
- [x] T012 Compose the host `apps/identity-api/Program.cs` — wires EF Core persistence + the Keycloak adapter (via `IdentityInfrastructureServiceCollectionExtensions.AddIdentityPersistence` / `AddKeycloakIdentityProvider`, the Keycloak adapter as a resilient typed `HttpClient`) and the Wolverine transactional outbox (`AddRoomyMessaging`, ADR-0005/0012) — all config-bound (the `identity`/`rabbitmq` connection strings and a `Keycloak` section), nothing hard-coded. Health/liveness via `MapDefaultEndpoints` (`/health`, `/alive`). The deferred **initial migration** lands here: `InitialCreate` in `libs/identity/infrastructure/Persistence/Migrations/` (snake_case `users`, unique `ux_users_email` / `ux_users_keycloak_subject_identifier`) generated through a host design-time factory; EF migrations are marked generated code in `.editorconfig`; the persistence integration tests now apply the migration (`MigrateAsync`) instead of `EnsureCreated`, so they validate it. A fast container-free DI registration test asserts the ports bind to their adapters. Wiring `identity-api` into the Aspire app host and the full end-to-end boot are the next setup step (#17/#21).

**Checkpoint**: Foundation ready — user stories can begin.

---

## Phase 3: User Story 1 — Administrator login (US1 / IA-1, #26) 🎯 MVP (P1)

**Goal**: A seeded `DefaultAdmin` can log in through the BFF and is recognised as an administrator.

**Independent Test**: With an empty DB, start the service; log in via the BFF with configured DefaultAdmin credentials → `GET /account/me` returns `role: administrator`; wrong password and unknown email both return the same generic failure.

- [x] T013 [P] [US1] Integration test in `tests/identity-integration/DefaultAdminSeederTests.cs` — RED-verified, against real Postgres: seeding provisions the Administrator role, activates the account, and persists it; a second run is idempotent (the provider is called once, one record exists). The real Keycloak provisioning path is already covered by `KeycloakIdentityProviderTests` (T011), so a recording stub stands in for the provider here to keep the focus on the seeder's orchestration.
- [x] T014 [P] [US1] HTTP integration test in `tests/identity-integration/AccountMeTests.cs` — boots the host in-process (WebApplicationFactory) against real Postgres, with the Wolverine runtime + DefaultAdmin seeder removed and a `TestAuthHandler` standing in for the BFF-forwarded token. Covers: authenticated admin → `200` `role: administrator`; authenticated employee → `200` `role: employee`; no session → `401`; authenticated subject with no account → `404`. The generic-login-failure case (FR-008) is a Keycloak/BFF concern, handled in slice 3 (T017), not this endpoint.
- [x] T015 [US1] DefaultAdmin seeding in `apps/identity-api/Seeding/` — `DefaultAdminSeeder` (provision via `IIdentityProviderPort` with the Administrator role → `User.Register` + `Activate` → persist; idempotent via `ExistsByEmailAsync`), `DefaultAdminOptions` (bound from the `DefaultAdmin` config section), and a `DefaultAdminSeederHostedService` that runs it once at startup in a scope and fails startup loudly on error (FR-004). Registered in `Program.cs`. T013 green.
- [x] T016 [US1] `GET /account/me` in `apps/identity-api/Endpoints/AccountEndpoints.cs` — resolves the authenticated subject (`sub` / name-identifier claim) to its account via `IUserRepository.GetByKeycloakSubjectAsync` (added with this slice, non-nullable `Result<User>`) and returns the `AccountResponse` projection (`role` flattened to `employee`/`administrator`); `401` enforced by `RequireAuthorization`, `404` for a subject with no record. The host wires JWT-bearer auth against the Keycloak realm (the BFF forwards the token, ADR-0013). T014 green.
- [x] T017 [US1] End-to-end stack wiring. The Keycloak realm (`apps/gateway/keycloak/roomy-realm.json` — roles `employee`/`administrator`, unique email, `length(8)` policy, the `roomy-bff` client) and the BFF OIDC login route (`/bff/login`, auth-code + PKCE, token forwarding) already existed from the gateway setup (#21). This slice composes them: the **Aspire app host** now runs `identity-api` with its own `identity` database + RabbitMQ + Keycloak references and the Keycloak-admin / DefaultAdmin config, and the **gateway references it**; a **YARP route** (`/account/{**}` → the `identity` cluster at `http://identity-api`, default authorization policy + access-token forwarding) exposes the account surface. The host now **applies migrations at startup** (`IdentityDatabaseMigrator`) before the seeder. An app-host **model test** (`tests/apphost/`, builds the graph without starting containers) guards the wiring. The browser OIDC login itself is the deferred Playwright e2e (plan.md), not automated here.

**Checkpoint**: Admin login MVP works end-to-end (browser login verification deferred to the Playwright e2e slice).

---

## Phase 4: User Story 2 — Log out (US2 / IA-6, #31) (P2)

**Goal**: A logged-in user can end their session.

**Independent Test**: After login, POST the BFF logout route → subsequent `GET /account/me` returns `401`.

- [x] T018 [P] [US2] Integration test in `apps/gateway/tests/LogoutTests.cs` — boots the gateway in-process (WebApplicationFactory) with the OIDC discovery document stubbed in-memory and a header-driven test auth scheme standing in for a session, so Keycloak is never contacted. Two facts: an authenticated `POST /bff/logout` ends the session by clearing the `__Host-roomy.bff` BFF cookie, and triggers Keycloak RP-initiated end-session (302 → the realm's `end_session_endpoint`). RED-verified — both returned `500` first, because the route signed out the framework-default `Cookies` scheme rather than the registered BFF cookie scheme. The live OIDC round-trip against a real Keycloak is the deferred Testcontainers e2e (#73). (Logout is a gateway/BFF concern, not identity-api, so the test lives with the gateway — not the `tests/identity/...` path this task originally guessed, written before the gateway setup #21 landed the route.)
- [x] T019 [US2] The BFF logout route already existed from the gateway setup (#21): `POST /bff/logout` in `apps/gateway/Bff/BffEndpoints.cs` (RequireAuthorization → `SignOut` clearing the session cookie + OIDC RP-initiated end-session), the `roomy-bff` client's `post.logout.redirect.uris`, and the OIDC `SaveTokens` that supplies the `id_token_hint`. This slice fixed the one latent defect T018 surfaced — it signed out `CookieAuthenticationDefaults.AuthenticationScheme` (`"Cookies"`) instead of `BffAuthenticationExtensions.CookieScheme` (`"RoomyBff"`), so the real session cookie would never be cleared (and the unregistered scheme threw a 500). No `SessionEndpoints.cs` was needed.

**Checkpoint**: Login + logout both work.

---

## Phase 5: User Story 3 — Provision employee accounts (US3 / IA-3, #28) (P2)

**Goal**: Hiring an employee provisions a usable account (eventual), per ADR-0025.

**Independent Test**: Publish `EmployeeHired` (role `employee`, password ≥ 8) → `UserRegistered` emitted and the employee can log in; password < 8 → `UserProvisioningFailed(password_rejected)`, no login; duplicate email → `UserProvisioningFailed(email_taken)`.

- [x] T020 [P] [US3] Use-case tests in `tests/identity/Features/ProvisioningTests.cs` — RED-verified, with in-memory port doubles: success persists an Active account and emits `UserRegistered` (admin elevation flattened to the role); `password_rejected` / `email_taken` / `provider_error` each emit `UserProvisioningFailed` with the mapped reason and persist nothing. The EmployeeHired→command mapping at the messaging edge is covered Docker-free by `tests/identity-integration/EmployeeHiredConsumerTests.cs`. The real Keycloak + Postgres + broker round-trip is the deferred e2e (organization does not emit `EmployeeHired` yet, and it needs the running stack — the same deferral US1 took for browser login).
- [x] T021 [US3] `RegisterUser` command + `RegisterUserHandler` in `libs/identity/application/UseCases/` — provider-first (Keycloak is the authority for the two business failures, returned as a `Result` code, never thrown), then `User.Register` under the saga's pre-allocated `UserId` + `Activate` + persist, publishing `UserRegistered` / `UserProvisioningFailed` through the owned `IIntegrationEventPublisher`. `User.Register` gained an overload honouring the pre-allocated identifier (the 1:1 `User`↔`Employee` correlation key, ADR-0025). Bound to its command-handler port via `AddIdentityUseCases`. T020 green.
- [x] T022 [US3] `EmployeeHiredConsumer` in `libs/identity/infrastructure/Messaging/` — the only place organization's published `EmployeeHired` is referenced (ADR-0031); it maps the foreign contract onto `RegisterUser` and invokes the handler, keeping `application` free of another context's published language. The contracts themselves live in the new per-context `contracts` libraries (T-new, ADR-0031), published over the Wolverine outbox/inbox; the host scans the identity infrastructure assembly for the consumer (`AddRoomyMessaging(..., assembly)`). A DI registration test guards the handler binding. The broker round-trip is the deferred e2e above.

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
