# Integration Events: Hire Employee (008)

The saga round-trip (ADR-0025/0031). **All three contracts already exist**; this feature wires
organization to **publish** the first and **consume** the other two. No contract changes.

## Published by organization — `EmployeeHired` (`SmartSolutionsLab.Roomy.Contracts.Organization`)

Emitted when an administrator hires a colleague. Drained from the `EmployeeHired` **domain event** by
`OrganizationUnitOfWork` at commit and staged in the transactional outbox (ADR-0037), so it commits
atomically with the `Employee` write.

`EmployeeHired(Guid EmployeeId, Guid UserId, string Email, string DisplayName, HiredRole Role, string InitialPassword, DateTimeOffset OccurredAt)`

- `UserId` is **pre-allocated** by organization — the 1:1 correlation key (FR-006).
- `InitialPassword` is the transient secret identity uses to set the credential; it is never persisted by
  either side.
- **Consumer (already built):** identity's `EmployeeHiredConsumer` → `RegisterUser`.

## Consumed by organization — identity's acks (`SmartSolutionsLab.Roomy.Contracts.Identity`)

Mapped at organization's infrastructure edge to internal commands (ADR-0031); the application never sees
these foreign contracts.

### `UserRegistered`
`UserRegistered(Guid UserId, Guid EmployeeId, string Email, AccountRole Role, Guid KeycloakSubjectId, DateTimeOffset OccurredAt)`
- Emitted when identity has fully provisioned the account (Keycloak user created, persisted Active).
- **New consumer:** `UserRegisteredConsumer` → `CompleteEmployeeProvisioning(EmployeeId)` ⇒ employee `Active`.

### `UserProvisioningFailed`
`UserProvisioningFailed(Guid UserId, Guid EmployeeId, UserProvisioningFailureReason Reason, DateTimeOffset OccurredAt)`
- Emitted when the account cannot be provisioned (`EmailTaken` / `PasswordRejected` / `ProviderError`).
- **New consumer:** `UserProvisioningFailedConsumer` → `FailEmployeeProvisioning(EmployeeId, reason)` ⇒
  employee `Failed` (the compensation, FR-007/FR-010).

## Delivery guarantees

- Publish is **transactionally outboxed** (commits with the employee write); consume is through the
  durable **inbox** (the state transition + dedup commit together, ADR-0012).
- **At-least-once**: re-delivery is safe — `CompleteProvisioning`/`FailProvisioning` are idempotent
  no-ops in their terminal state, and the inbox de-duplicates (FR-008).

## Saga flow

```text
admin → POST /employees → Employee(Provisioning) ──EmployeeHired──▶ identity: RegisterUser
                                                                       │ provision (Keycloak)
        employee Active ◀──CompleteProvisioning──◀──UserRegistered────┤ success
        employee Failed ◀──FailProvisioning─────◀──UserProvisioningFailed (EmailTaken/…) ─ failure
```
