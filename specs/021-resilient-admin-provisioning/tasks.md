# Tasks: Resilient default-admin provisioning

Dependency-ordered, test-first. Each task cites the acceptance criteria it satisfies.

## Red — failing tests

- [ ] **T1** Domain unit tests (`backend/tests/organization/Domain/Employees/EmployeeTests.cs`) for
  `Employee.RetryProvisioning(initialPassword)`:
  - re-raises `EmployeeHired` with the existing identifiers + supplied password when *provisioning* (FR-002, FR-003; US1-1).
  - resets *failed* → *provisioning* and re-raises when *failed* (FR-002; US1-2).
  - no-op (no event, stays *active*) when *active* (FR-004; US2-1).
- [ ] **T2** Seeder tests (`backend/tests/organization-integration/DefaultAdminSeederTests.cs`):
  - admin absent → hires as administrator (FR-005; US2-2) — existing test retained.
  - admin exists → dispatches `RetryEmployeeProvisioning(email, password)` (FR-001, FR-002; US1-1) — replaces the old "does not hire again" assertion.

## Green — implementation

- [ ] **T3** `Employee.RetryProvisioning(string initialPassword) : Result` (FR-002, FR-003, FR-004, FR-006).
- [ ] **T4** `IEmployeeRepository.GetByWorkEmailAsync` + EF implementation (supports T5).
- [ ] **T5** `RetryEmployeeProvisioning` command + handler (fetch by email → `RetryProvisioning` → save) (FR-002, FR-007).
- [ ] **T6** Register the handler in `OrganizationInfrastructureServiceCollectionExtensions`.
- [ ] **T7** `DefaultAdminSeeder`: when the admin exists, dispatch `RetryEmployeeProvisioning`; hire path unchanged (FR-001, FR-004, FR-005).

## Docs & verify

- [ ] **T8** Amend `docs/adr/0025-user-employee-provisioning-saga.md` with the startup reconvergence note.
- [ ] **T9** Gates: `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`.
- [ ] **T10** E2E: restart the app tier, confirm the admin reaches *active* and signs in (SC-001, SC-004).
