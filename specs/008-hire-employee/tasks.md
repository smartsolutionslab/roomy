---
description: "Task list for Hire Employee (008)"
---

# Tasks: Hire Employee

**Input**: Design documents in `specs/008-hire-employee/` (plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md)

**Tests**: REQUIRED. The constitution (Principle I) and `CLAUDE.md` golden rule 1 mandate test-first — every story phase writes failing tests before implementation (Red → Green → Refactor).

**Organization**: Grouped by user story. This builds the **organization side** of the provisioning saga (ADR-0025); the **identity side already exists on `main`** and is activated by the first published `EmployeeHired`.

## Story label map (priority order)

| Label | Story | Spec scenarios | Priority |
|---|---|---|---|
| US1 | Hire a colleague → account provisioned (happy path) | US1 1–4 | **P1 (MVP)** |
| US2 | No half-accounts when provisioning fails (compensation) | US2 1–3 | P2 |
| US3 | Hiring is safe to repeat (idempotent) | US3 1–2 | P3 |

## Format: `[ID] [P?] [Story] Description with file path`

- **[P]**: parallelizable (different files, no incomplete-task dependency).
- Paths follow plan.md: `libs/organization/{domain,application,infrastructure}/`, `apps/organization-api/`, `apps/gateway/`, `tests/organization*/`, consuming `libs/identity/contracts`, publishing `libs/organization/contracts`.

> **No new ADR** — ADR-0025 (Accepted) governs. **No producer dependency** — every contract already exists; this feature publishes `EmployeeHired` and consumes identity's `UserRegistered`/`UserProvisioningFailed`. The identity half (`RegisterUser` + consumer) is on `main`, so the saga completes the moment organization publishes.

> **Headline guarantee (FR-007):** no half-accounts. The compensation (US2) and idempotency (US3) are what make the saga correct — get the round-trip integration tests (T013/T015/T016) green, not just the unit tests.

---

## Phase 1: Setup

- [x] T001 Add a `ProjectReference` from `libs/organization/infrastructure` to `libs/identity/contracts` so the new consumers can map identity's `UserRegistered`/`UserProvisioningFailed` (`context:shared`, ADR-0031); confirm the Nx tags still pass the boundary lint and that `Roomy.ArchitectureTests` already references the three organization projects (added in 002 — otherwise the rules are vacuous).

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: No user-story work can begin until this phase is complete. This builds the `Employee` aggregate (the consistency boundary for the whole provisioning lifecycle) and its persistence — shared by all three stories.

- [x] T002 [P] RED domain unit tests in `tests/organization/Domain/Employees/` (Shouldly) for the value objects — `EmployeeIdentifier`/`UserIdentifier` (GUIDv7 branded, non-empty, implicit `Guid`), `WorkEmail` (rejects malformed, normalizes), `EmployeeName` (rejects empty), `EmployeeRole` (Employee/Administrator ↔ `HiredRole`), `ProvisioningState`, `ProvisioningFailureReason`.
- [x] T003 [P] Implement those value objects + the `EmployeeHired` **domain event** (carries the VOs + the transient initial password) + `IEmployeeRepository` in `libs/organization/domain/Employees/` (invariants via `Ensure.That(...)`). T002 green.
- [x] T004 RED domain tests in `tests/organization/Domain/Employees/EmployeeTests.cs` — `Hire(...)` ⇒ employee in `Provisioning`, raises one `EmployeeHired` domain event with the role/email/name/password; `CompleteProvisioning()` ⇒ `Active` (second call a no-op; rejected on a `Failed` employee); `FailProvisioning(reason)` ⇒ `Failed` with the reason (idempotent; rejected on an `Active` employee). State machine + terminal guards (data-model.md, FR-007).
- [x] T005 Implement the `Employee` aggregate (`: Aggregate`, `RaiseDomainEvent`) in `libs/organization/domain/Employees/Employee.cs` — mirrors `Office`. T004 green.
- [x] T006 Add `EmployeeConfiguration` + `EmployeeRepository` (mirrors `Office`; fetch-that-may-miss returns `Result<Employee>`), the `Employees` `DbSet` in `OrganizationDbContext`, and the **migration** for the `employees` table (no password column). **Integration test** (real Postgres via the sibling test host, [[aspire-postgres-integration-tests]]): an employee round-trips, including its `ProvisioningState`/`FailureReason`.
- [x] T007 [P] Confirm the architecture suite stays green — `Employee` lives in `Roomy.Organization.Domain`; the foreign identity contracts are referenced only at the infrastructure edge, never in `application` (`tests/architecture`).

**Checkpoint**: the `Employee` aggregate and its persistence exist; the saga wiring can begin.

---

## Phase 3: User Story US1 — Hire a colleague, account provisioned (Priority: P1) 🎯 MVP

**Goal:** an administrator hires a colleague; the employee is recorded in *provisioning*, `EmployeeHired` is published, identity provisions the login, and on `UserRegistered` the employee converges to *active* — the colleague can sign in once provisioning completes (FR-001..006).

**Independent test:** `POST /employees` as an admin returns 202; the published `EmployeeHired` drives identity to provision and publish `UserRegistered`; organization consumes it and the employee is *active*; the colleague can authenticate. Full round-trip with only this story.

- [ ] T008 [P] [US1] RED application tests in `tests/organization/Application/` — `HireEmployeeHandler` gets the seeded company, **pre-allocates a `UserId`**, creates the employee, and commits once (mirrors `CreateOfficeHandler`); and a mapping test that `OrganizationIntegrationEventMap` turns the `EmployeeHired` domain event into the `EmployeeHired` **contract** carrying the pre-allocated `UserId` and the password.
- [ ] T009 [US1] Implement `HireEmployee` command + `HireEmployeeHandler` (`libs/organization/application/UseCases/`) and EXTEND `OrganizationIntegrationEventMap` with the `EmployeeHired` domain→contract mapping (`libs/organization/infrastructure/Messaging/`); register the handler + repository in `OrganizationInfrastructureServiceCollectionExtensions`. T008 green.
- [ ] T010 [US1] RED application test for the `UserRegistered` ack path; implement `CompleteEmployeeProvisioning` command + handler (`libs/organization/application/UseCases/`) and `UserRegisteredConsumer` (`libs/organization/infrastructure/Messaging/`) mapping `Contracts.Identity.UserRegistered` → the command (employee ⇒ `Active`).
- [ ] T011 [US1] Make organization-api a **consumer**: in `apps/organization-api/Program.cs` pass the organization **infrastructure** assembly to `AddRoomyMessaging` so the inbox scans the consumers (it stays a publisher via `AddIntegrationEventOutbox`). Regenerate the committed Wolverine codegen (`apps/organization-api/Internal/Generated/`); confirm organization-api is in the CI `codegen verify` step ([[wolverine-codegen-platform-sensitive]]).
- [ ] T012 [US1] RED API/contract tests (`tests/organization-integration/`, WebApplicationFactory) for `POST /employees` — **202** `{ employeeId, userId, state: "Provisioning" }`; **422 `invalid_hire`** (bad email/empty name/unknown role/empty password); **403** non-admin; **401** no session — per `contracts/organization-api.md`. Implement `EmployeeEndpoints` (`apps/organization-api/Endpoints/`, admin-only), register in `Program.cs`, add the `/employees` route to `apps/gateway/appsettings.json`, re-emit the OpenAPI spec (`Roomy.Organization.Api.json`) and confirm/add organization-api to the CI OpenAPI drift-gate step. T012 green.
- [ ] T013 [US1] **Integration test** — the happy-path round-trip against real Postgres + RabbitMQ + Keycloak: hire ⇒ `EmployeeHired` is outboxed and committed with the employee row; identity provisions and publishes `UserRegistered`; organization consumes it and the employee converges to **`Active`**; the colleague can authenticate with the work email + initial password (US1, SC-002).

**Checkpoint**: MVP — hiring works end-to-end and a hired colleague can sign in once provisioning completes.

---

## Phase 4: User Story US2 — No half-accounts when provisioning fails (Priority: P2)

**Goal:** when provisioning fails (e.g. the email is already in use), the employee is marked *provisioning failed* with a reason and no usable half-account remains (FR-007/FR-010).

**Independent test:** hire a colleague whose email is already taken ⇒ identity publishes `UserProvisioningFailed(EmailTaken)` ⇒ organization marks the employee *failed*; there is no active employee and no orphaned login for that email.

- [ ] T014 [P] [US2] RED application test for the `UserProvisioningFailed` ack path; implement `FailEmployeeProvisioning` command + handler (mapping `UserProvisioningFailureReason` → `ProvisioningFailureReason`) and `UserProvisioningFailedConsumer` (`libs/organization/infrastructure/Messaging/`); register the handler. Regenerate the Wolverine codegen (second consumer handler).
- [ ] T015 [US2] **Integration test** — the email-taken compensation round-trip: hire a colleague whose work email already exists ⇒ identity publishes `UserProvisioningFailed(EmailTaken)` ⇒ the employee converges to **`Failed`** with the reason; assert no active employee and no usable login exist for that email (FR-007/FR-010, US2).

**Checkpoint**: US1 + US2 — both the success and the no-half-account failure outcomes are enforced end-to-end.

---

## Phase 5: User Story US3 — Hiring is safe to repeat (idempotent) (Priority: P3)

**Goal:** at-least-once delivery and retries never produce duplicate employees or logins; re-using an email is the US2 failure, not a duplicate (FR-008).

**Independent test:** re-deliver `EmployeeHired` and the acks ⇒ exactly one employee and one login; one terminal transition.

- [ ] T016 [US3] **Integration test** — re-deliver `EmployeeHired` and re-deliver the `UserRegistered`/`UserProvisioningFailed` acks; assert exactly one employee, one login account, and a single terminal transition (the inbox de-duplicates and `CompleteProvisioning`/`FailProvisioning` are terminal-state no-ops, FR-008). A new hire reusing an existing email resolves to *failed* (US2), never a silent duplicate.

**Checkpoint**: the saga is correct under retries — no duplicates, no half-accounts.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T017 [P] Run the full gate suite on affected projects: `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `pnpm nx affected -t lint test build`, the organization **OpenAPI drift gate**, and the organization **Wolverine codegen-verify** (organization now consumes).
- [ ] T018 [P] Cross-spec follow-up (ADR-0025): amend `specs/001-identity-access/spec.md` scenario 4 / FR-006 so the login criterion reads as **eventual** ("can log in once provisioning completes"), aligning the already-implemented `001` with the saga. The seeded `DefaultAdmin` startup provisioning remains a separate ADR-0025 follow-up (out of scope here).
- [ ] T019 Run the `quickstart.md` manual smoke through the gateway (hire → sign in; hire-taken-email → failed); update the `CLAUDE.md` active-plan pointer and the build-out roadmap to reflect 008.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: the identity-contracts reference — no blockers.
- **Foundational (Phase 2)**: depends on Setup; **BLOCKS all stories** (the `Employee` aggregate + persistence).
- **US1 (Phase 3)**: depends on Phase 2 — the MVP; T011 (organization-as-consumer) is a one-time host change the later consumer reuses.
- **US2 (Phase 4)**: depends on Phase 2 and on US1's host/consumer wiring (T011) — adds the failure consumer + compensation.
- **US3 (Phase 5)**: depends on US1 + US2 (it asserts idempotency over both ack paths).
- **Polish (Phase 6)**: after the desired stories are complete.

### Within each story

- Tests are written and **fail** before implementation (Red → Green → Refactor).
- Domain → application/handlers → consumers/endpoint → round-trip integration.
- Commit per task or logical group (atomic Conventional Commits).

### Parallel opportunities

- Within Phase 2: T002 (VO tests) and T007 (architecture) are `[P]`; the aggregate (T004/T005) follows the VOs.
- T008 (US1 application/mapping tests) is `[P]`; T014 (US2 consumer) is `[P]` and touches different files than US1's consumer.
- T017/T018 polish are `[P]`.

---

## Parallel Example: User Story US1

```bash
# After Phase 2 is green:
Task: "RED HireEmployeeHandler + EmployeeHired mapping tests (T008)"
# then the UserRegistered ack path and the endpoint converge on the round-trip (T013).
```

---

## Implementation Strategy

### MVP first (US1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (the `Employee` aggregate) → 3. Phase 3 US1 →
**STOP & VALIDATE**: hire works end-to-end and the colleague can sign in once provisioning completes → demo.

### Incremental delivery

1. Setup + Foundational → the aggregate exists.
2. US1 → hire → provisioned/active (MVP) → demo.
3. US2 → no half-accounts on failure → demo.
4. US3 → idempotent under retries → demo.
5. Polish → gates, the `001` eventual-login alignment, quickstart.

---

## Notes

- [P] = different files, no incomplete-task dependency. [Story] maps a task to a backlog story for traceability.
- The round-trip integration tests (T013/T015/T016) are the real proof — the unit tests alone do not exercise the outbox/inbox or the identity half.
- No new integration-event contracts and no new ADR: this realizes ADR-0025 with the existing contracts.
- Verify tests fail before implementing; run the gate suite before "done"; no analyzer/test suppressions.
