# Quickstart & Validation: Hire Employee (008)

How to prove the slice works end-to-end. Details live in `data-model.md` and `contracts/`; this is the
run/validate guide. Tests are written **before** implementation (constitution I), grouped by story.

## Prerequisites

- Identity side on `main` (already): `RegisterUser`, identity's `EmployeeHired` consumer, and the
  `UserRegistered`/`UserProvisioningFailed` contracts — the dormant half this feature activates.
- Organization (002) on `main`: `Company`/`Office`/`Room`, the `Aggregate` domain-event base (ADR-0032),
  and the `OrganizationUnitOfWork` outbox drain (ADR-0037).
- Local stack via Aspire: `dotnet run --project backend/apps/apphost` (Postgres, RabbitMQ, Keycloak, gateway,
  identity-api, organization-api, attendance-api, db-migrator).
- The 008 migration (`Employees` table) applied by the db-migrator before organization-api starts
  (ADR-0033, `WaitForCompletion`).

## Layered validation (the test pyramid — `docs/testing-strategy.md`)

### 1. Domain unit tests (no infrastructure) — the state machine
Drive the `Employee` aggregate directly (Shouldly):

- `Hire(...)` ⇒ employee in `Provisioning`, raises one `EmployeeHired` domain event carrying the role,
  email, display name, and the initial password (US1).
- `CompleteProvisioning()` ⇒ `Active`; a second call is a no-op; calling on a `Failed` employee is
  rejected (terminal).
- `FailProvisioning(reason)` ⇒ `Failed` with the reason; a re-delivery is a no-op; calling on an `Active`
  employee is rejected (FR-007).
- Value objects: `WorkEmail` rejects malformed input; `EmployeeName` rejects empty; `EmployeeRole` maps
  to `HiredRole`.

### 2. Application + mapping unit tests
- `HireEmployeeHandler` pre-allocates a `UserId`, creates the employee, and commits once (mirrors
  `CreateOfficeHandler`); the `EmployeeHired` domain event → `EmployeeHired` contract carries the
  pre-allocated `UserId` and the password (`OrganizationIntegrationEventMap`).
- `UserRegisteredConsumer` maps to `CompleteEmployeeProvisioning`; `UserProvisioningFailedConsumer` maps
  `UserProvisioningFailureReason` → `ProvisioningFailureReason` and to `FailEmployeeProvisioning`.

### 3. Integration tests (real Postgres + RabbitMQ via the sibling test host) — the saga round-trip
The headline coverage (ADR-0025), per the Aspire-Postgres pattern (CI has Docker):

- **Happy path (US1):** hire ⇒ `EmployeeHired` is outboxed and committed with the employee row; the
  identity consumer provisions and publishes `UserRegistered`; organization consumes it and the employee
  converges to `Active` — and the colleague can authenticate (Keycloak) with the work email + initial
  password.
- **Email-taken compensation (US2):** hire a colleague whose email already exists ⇒ identity publishes
  `UserProvisioningFailed(EmailTaken)` ⇒ organization marks the employee `Failed`; **no** active employee
  and **no** orphaned login for that email (FR-007/FR-010).
- **Idempotency (US3):** re-deliver `EmployeeHired` and the acks ⇒ exactly one employee, one login,
  one terminal transition (inbox dedup + terminal no-ops, FR-008).
- The outbox commits the event atomically with the employee write (no employee row ⇒ no event).

### 4. API/contract tests (WebApplicationFactory through the host)
Assert `contracts/organization-api.md`:

- `POST /employees` as an administrator ⇒ **202** `{ employeeId, userId, state: "Provisioning" }`.
- Missing/invalid field ⇒ **422 `invalid_hire`**; non-administrator ⇒ **403**; no session ⇒ **401**.

### 5. Architecture & codegen
- `backend/tests/architecture`: organization layers stay within the dependency rule; the foreign contracts are
  referenced only at the infrastructure edge (consumers), never in `application`.
- `organization-api` Wolverine **codegen-verify** is green with the two new consumer handlers; the
  **OpenAPI drift gate** is green with `POST /employees`.

## Manual smoke (through the gateway)

1. Log in as the seeded administrator via the BFF.
2. `POST /employees` `{ displayName, email, role: "Employee", initialPassword }` ⇒ 202.
3. Within a few seconds, sign in as the new colleague (work email + initial password) ⇒ succeeds (US1,
   SC-002).
4. `POST /employees` reusing an existing email ⇒ 202, but that employee converges to `Failed` and the
   colleague cannot sign in (US2) — no half-account.

## Definition of done for the slice

- Every scenario (US1–US3) + edges has a test written **before** its code, now green.
- Full gate suite passes on affected projects: `dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, `pnpm nx affected -t lint test build`, the organization **OpenAPI
  drift gate**, and the organization **Wolverine codegen-verify** (new consumer).
- `CLAUDE.md` active-plan pointer updated; no analyzer or test suppressions. ADR-0025 followed (no new
  ADR).
