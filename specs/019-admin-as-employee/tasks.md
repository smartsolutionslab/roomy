---
description: "Task list for 019-admin-as-employee"
---

# Tasks: Administrator is also an employee

**Input**: Design documents from `specs/019-admin-as-employee/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/README.md, quickstart.md
**Tests**: REQUIRED — the constitution mandates test-first (Red → Green). Test tasks precede their implementation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2 (maps to the spec's user stories)

## Path Conventions

Backend bounded contexts under `backend/`. No frontend change.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Nothing to scaffold — the feature reuses the existing organization seeding pattern (`CompanySeeder` + hosted service) and the existing hire saga. No new project, dependency, or migration.

- [ ] T001 Confirm the precondition holds on the branch: ADR-0058 (`roomy_user_id` claim) is merged into `main` and the branch is rebased on it, so a hired admin's `UserId` reaches the Keycloak `roomy_user_id` attribute. (No code change.)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: ADR-before-code gate. **No implementation may begin until this is complete.**

- [ ] T002 Write **ADR-0059 — "DefaultAdmin bootstrap is organization-led"** in `docs/adr/0059-defaultadmin-bootstrap-organization-led.md`: record that the seeded administrator is provisioned by an organization-side `HireEmployee(Administrator)` at startup (resolving ADR-0025's deferred follow-up), the identity-side seeder is removed, and the bootstrap is a single saga path. Status **Accepted**; add it to the ADR index/README. (Decision content is in `research.md`.)

**Checkpoint**: ADR recorded — implementation can begin.

---

## Phase 3: User Story 1 - Administrator plans their own attendance (Priority: P1) 🎯 MVP

**Goal**: The seeded administrator is provisioned as an `Administrator` `Employee` via the existing hire saga, so they can reserve a desk and view their own reservations.

**Independent Test**: On a clean stack, after startup the admin can `POST /reservations` (201) and `GET /reservations/mine` (200), and the admin appears in the attendance employee directory with a `UserId` matching the identity `User`.

### Tests for User Story 1 (write first; MUST fail before implementation) ⚠️

- [ ] T003 [P] [US1] saga-e2e test in `backend/tests/saga-e2e/DefaultAdminBootstrapTests.cs`: after the stack starts on a clean slate, the seeded DefaultAdmin (a) exists as exactly one `Administrator` `Employee` in organization, (b) has a matching identity `User` (admin role) and Keycloak user carrying `roomy_user_id` = that `UserId`, and (c) appears in the attendance employee directory; then a reservation created for the admin succeeds.
- [ ] T004 [P] [US1] attendance-integration test in `backend/tests/attendance-integration/ReservationEndpointTests.cs` (or a new `AdminReservationTests.cs`): a caller whose `roomy_user_id` resolves to a directory employee can `POST /reservations` → 201 and `GET /reservations/mine` → 200 (guards the resolution path the admin now uses).

### Implementation for User Story 1

- [ ] T005 [US1] Add `DefaultAdminOptions` in `backend/apps/organization-api/Seeding/DefaultAdminOptions.cs` (Email, DisplayName, InitialPassword; `SectionName = "DefaultAdmin"`) and bind it in `backend/apps/organization-api/Program.cs`.
- [ ] T006 [US1] Add `DefaultAdminSeeder` in `backend/apps/organization-api/Seeding/DefaultAdminSeeder.cs`: after the company is seeded, issue `HireEmployee(EmployeeName.From(DisplayName), WorkEmail.From(Email), EmployeeRole.Administrator, InitialPassword)` via the command handler (mirrors `CompanySeeder`). Register it and a `DefaultAdminSeederHostedService` in `Program.cs`, ordered **after** `CompanySeederHostedService`.
- [ ] T007 [US1] Move the `DefaultAdmin__Email/DisplayName/InitialPassword` environment variables from `identityApi` to `organizationApi` in `backend/apps/apphost/AppHost.cs`.
- [ ] T008 [US1] Remove the identity-side bootstrap: delete `backend/apps/identity-api/Seeding/DefaultAdminSeeder.cs`, `DefaultAdminSeederHostedService.cs`, `DefaultAdminOptions.cs`, and their registration/config binding in `backend/apps/identity-api/Program.cs`.
- [ ] T009 [US1] Remove the now-obsolete `backend/tests/identity-integration/DefaultAdminSeederTests.cs` (the tested unit no longer exists; its coverage moves to T003/T010). Confirm no other identity test references the removed types.

**Checkpoint**: On a clean stack the administrator can reserve and view their own reservations; T003/T004 pass.

---

## Phase 4: User Story 2 - Every account maps to exactly one staff member (Priority: P2)

**Goal**: The DefaultAdmin bootstrap upholds the 1:1 `User`↔`Employee` invariant and is idempotent — repeated startups create no duplicate.

**Independent Test**: Run the organization admin seeder twice; exactly one `Administrator` `Employee` exists for the seeded company and no error occurs.

### Tests for User Story 2 (write first; MUST fail before implementation) ⚠️

- [ ] T010 [P] [US2] organization-integration test in `backend/tests/organization-integration/DefaultAdminSeederTests.cs`: seeding creates exactly one `Administrator` `Employee` for the seeded company linked to a fresh `UserId`; running the seeder again creates **no** second employee and does not throw.

### Implementation for User Story 2

- [ ] T011 [US2] Add the idempotency guard in `backend/apps/organization-api/Seeding/DefaultAdminSeeder.cs`: before hiring, no-op if an employee with the admin `WorkEmail` already exists (presence check, not exception-driven). Ensure ordering after `CompanySeeder` so the seeded company exists.

**Checkpoint**: US1 and US2 both hold; repeated startups are safe.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [ ] T012 [P] Update the ADR-0025 follow-up trail and context-map notes: reference ADR-0059 from `001-identity-access` / `002-office-management` where they describe DefaultAdmin provisioning; ensure the ADR index lists 0059.
- [ ] T013 [P] Update `CLAUDE.md` if a convention note is warranted (DefaultAdmin is bootstrapped organization-led via a seeded `HireEmployee(Administrator)`).
- [ ] T014 Run the full gate on affected projects — `pnpm nx affected -t lint test build`, `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes` — and execute `quickstart.md` (clean reseed → admin reserves and lists their reservation).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: precondition check only.
- **Foundational (Phase 2 / ADR-0059)**: BLOCKS all implementation (Principle V).
- **US1 (Phase 3)**: after Foundational. Delivers the MVP.
- **US2 (Phase 4)**: builds on the US1 seeder (same file) — sequential after T006, not parallel with it.
- **Polish (Phase 5)**: after US1 + US2.

### Within / across stories

- T003, T004 are written first and must FAIL before T005–T009.
- T005–T009 are the cutover and are **not independent**: removing the identity seeder (T008) without adding the organization seeder (T006) leaves no admin, and running both at once double-provisions (identity `email_taken`). Implement T005→T006→T007→T008→T009 as one coherent swap, ideally one commit.
- T010 written before T011. T011 edits the same file as T006 → sequential.

### Parallel Opportunities

- T003 and T004 (different test files) can be written in parallel.
- Polish T012 and T013 (different docs) can run in parallel.
- The implementation tasks T005–T009 and T011 touch overlapping wiring/files → keep sequential.

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 2: ADR-0059.
2. Phase 3: write T003/T004 (Red) → implement the cutover T005–T009 (Green).
3. **STOP and VALIDATE**: clean stack, admin reserves + lists reservations.

### Then harden

4. Phase 4: idempotency (T010 Red → T011 Green) — safe restarts, exactly one admin.
5. Phase 5: docs + full gate + quickstart.

---

## Notes

- Test-first is non-negotiable here (constitution Principle I): each test task precedes its implementation and must be observed failing first.
- The whole feature reuses the existing saga; the only genuinely new code is the organization-side `DefaultAdminSeeder` (+ options/hosted-service) and the removal of the identity-side one.
- Commit per logical group; keep the cutover (T005–T009) coherent so no intermediate state leaves the system without an admin.
- Implement in a **fresh agent context** (golden rule 7); rehydrate from this plan and its artifacts.
