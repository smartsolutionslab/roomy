# Phase 1 Data Model: Hire Employee

The feature adds the **`Employee` aggregate** to the organization domain (state-based, EF Core — it is
**not** event-sourced; it mirrors `Office`). Cross-context contracts (`EmployeeHired`, `UserRegistered`,
`UserProvisioningFailed`) already exist and are unchanged. What follows is the aggregate, its provisioning
state machine, the internal commands, and the infrastructure mapping.

## Aggregate: `Employee` (organization domain, `Employees/`)

A hired colleague of the seeded company and the consistency boundary for the provisioning lifecycle. Owns
no other entities. Behaviour lives here (constitution II); the provisioning state is an enforced
invariant, not a flag.

| Field | Type | Notes |
|---|---|---|
| `Identifier` | `EmployeeIdentifier` | GUIDv7 branded id (aggregate identity) |
| `CompanyIdentifier` | `CompanyIdentifier` | the seeded company (single-tenant v1) |
| `UserIdentifier` | `UserIdentifier` | **pre-allocated** at hire — the 1:1 saga correlation key (FR-006) |
| `Name` | `EmployeeName` | display name VO (non-empty) |
| `Email` | `WorkEmail` | work email VO (well-formed) |
| `Role` | `EmployeeRole` | `Employee` (base) or `Administrator` (elevation) |
| `State` | `ProvisioningState` | `Provisioning` → `Active` \| `Failed` |
| `FailureReason` | `ProvisioningFailureReason?` | set only in `Failed`; coarse (mirrors identity's reasons) |

> The **initial password is NOT a field** — it is supplied to `Hire`, carried on the `EmployeeHired`
> domain event, mapped onto the integration event, and never persisted (FR-009).

### Behaviour (aggregate methods)

- **`Employee.Hire(company, user, name, email, role, initialPassword)`** → creates the employee in
  `Provisioning` and raises the `EmployeeHired` **domain event** (carrying the VOs + the transient
  password). `user` is pre-allocated by the caller.
- **`CompleteProvisioning()`** → `Provisioning` → `Active`. Idempotent: a second call when already
  `Active` is a no-op; calling on a `Failed` employee is rejected (terminal).
- **`FailProvisioning(reason)`** → `Provisioning` → `Failed` with the reason (the compensation, FR-007).
  Idempotent on re-delivery; calling on an `Active` employee is rejected (terminal).

### State transitions

```text
            Hire
   (none) ──────────▶ Provisioning
                         │  CompleteProvisioning            FailProvisioning(reason)
                         ├──────────────────────▶ Active    └──────────────────────▶ Failed
   Active / Failed are terminal; re-delivered acks are idempotent no-ops in the same terminal state.
```

## Value objects (organization domain, `Employees/`)

- `EmployeeIdentifier`, `UserIdentifier` — GUIDv7 branded `…Identifier` with implicit `Guid` (EF Core).
- `WorkEmail` — validated email (`Ensure.That(...)`), normalized (trim/lower) for storage.
- `EmployeeName` — non-empty display name.
- `EmployeeRole` — `Employee` | `Administrator`; maps to the contract's `HiredRole`.
- `ProvisioningState` — `Provisioning` | `Active` | `Failed`.
- `ProvisioningFailureReason` — `EmailTaken` | `PasswordRejected` | `ProviderError` (mirrors identity's
  `UserProvisioningFailureReason`, mapped at the consumer edge).

## Intra-context domain event

- **`EmployeeHired`** (domain event, `IDomainEvent`) — raised by `Employee.Hire`, carrying the aggregate's
  value objects **and the transient initial password**. Internal to organization; mapped to the published
  contract at the infrastructure edge (never leaves the domain as-is).

## Application — commands & handlers

Owned `ICommandHandler` abstractions (ADR-0005); committed through the existing `IUnitOfWork`.

- **`HireEmployee(DisplayName, WorkEmail, EmployeeRole, InitialPassword)` → `EmployeeIdentifier`** —
  `HireEmployeeHandler`: get the seeded company; **pre-allocate a `UserIdentifier`**; `Employee.Hire(...)`;
  `employees.AddAsync`; `unitOfWork.SaveChangesAsync` (drains `EmployeeHired` → outbox). Mirrors
  `CreateOfficeHandler`.
- **`CompleteEmployeeProvisioning(EmployeeIdentifier)`** — load → `CompleteProvisioning()` → save (idempotent).
- **`FailEmployeeProvisioning(EmployeeIdentifier, ProvisioningFailureReason)`** — load →
  `FailProvisioning(reason)` → save (the compensation; idempotent).

> `CompleteProvisioning`/`FailProvisioning` resolve the employee by `EmployeeId` (the contracts carry it).
> Repository fetch-that-may-miss returns `Result<Employee>` (`Error.NotFound`), never `T?`.

## Infrastructure mapping

- **Outbound** (`OrganizationIntegrationEventMap`, EXTEND): `Employees.EmployeeHired` domain event →
  `Contracts.Organization.EmployeeHired(EmployeeId, UserId, Email, DisplayName, HiredRole, InitialPassword,
  OccurredAt)`.
- **Inbound consumers** (NEW, map foreign contract → internal command, ADR-0031):
  - `UserRegisteredConsumer`: `Contracts.Identity.UserRegistered` → `CompleteEmployeeProvisioning(EmployeeId)`.
  - `UserProvisioningFailedConsumer`: `Contracts.Identity.UserProvisioningFailed` →
    `FailEmployeeProvisioning(EmployeeId, map(reason))`, where `UserProvisioningFailureReason` →
    `ProvisioningFailureReason`.

## Persistence

- `EmployeeConfiguration` + `EmployeeRepository` (mirrors `Office`); `OrganizationDbContext` gains an
  `Employees` `DbSet`. One **migration** creates the `employees` table (`state`/`role`/`failure_reason`
  as strings or enums; no password column).

## Validation & policy rules (from requirements)

| Rule | Source | Where enforced |
|---|---|---|
| Only an administrator may hire | FR-001 | `POST /employees` `RequireAuthorization`/role |
| Name, well-formed email, role, password required | FR-002 | value objects (`Ensure`) + request binding |
| Hire records the employee in `Provisioning` immediately | FR-003 | `Employee.Hire` + unit of work |
| Login only once provisioning completes | FR-004 | identity activates the `User` on `UserRegistered`; employee `Active` mirrors it |
| Active on success | FR-005 | `CompleteProvisioning` on `UserRegistered` |
| 1:1 link by shared id; pre-allocated `UserId` | FR-006 | `HireEmployeeHandler` allocates `UserId`; carried on every event |
| No half-accounts; failed → `Failed`, never `Active` | FR-007 | `ProvisioningState` invariant + `FailProvisioning` |
| Idempotent under at-least-once delivery | FR-008 | terminal-state no-ops + Wolverine inbox dedup |
| Initial password never persisted | FR-009 | not a field; on the event only |
| Taken email → provisioning failure, no duplicate | FR-010 | identity `email_taken` → `UserProvisioningFailed` → `Failed` (R4) |
| Communicate only by id + events | FR-011 | outbox/inbox; no cross-DB access |
| DefaultAdmin pre-exists, outside this flow | FR-012 | startup seeder (out of scope) |
