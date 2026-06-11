# Phase 1 Data Model: Administrator is also an employee

No new entities, aggregates, or schema changes. The feature makes the seeded administrator flow
through the **existing** `Employee` lifecycle, so the data shapes are unchanged — only an additional
row (the admin) appears in each store. Documented here for traceability.

## Entities touched (all existing)

### Employee (organization — aggregate root)
- **Identity**: `EmployeeIdentifier` (GUIDv7), refs `CompanyIdentifier`, refs `UserIdentifier`.
- **Fields**: `EmployeeName`, `WorkEmail`, `EmployeeRole` (`Employee` | `Administrator`), provisioning state.
- **New instance**: the DefaultAdmin, hired with `EmployeeRole.Administrator`, belonging to the seeded `Company`.
- **Invariant reinforced**: 1:1 with the identity `User` via `UserIdentifier`; exactly one admin Employee.

### User (identity — aggregate root)
- Already created today; now created **via the saga** (`RegisterUser` from `EmployeeHired`) instead of the
  removed identity-side seeder. Carries the Administrator role and the `KeycloakSubjectIdentifier`.
- Its `UserIdentifier` equals the organization `Employee.UserId` (minted once in `HireEmployeeHandler`).

### Employee read model (attendance — projection)
- Row `{ EmployeeId, UserId, DisplayName }` inserted by attendance's `EmployeeHiredConsumer`.
- This is the row whose absence caused `unknown_employee`; the admin now has one.

### Keycloak user (identity provider)
- Provisioned by `RegisterUser`, carrying the `roomy_user_id` attribute = the admin's `UserIdentifier`
  (ADR-0058), so `CurrentUser.UserId()` resolves the admin at the attendance edge.

## Identifier flow (one `UserId` end to end)

```
HireEmployeeHandler: user = UserIdentifier.New()
  → Employee.UserId (organization)
  → EmployeeHired.UserId (integration event)
     → identity RegisterUser → User.Identifier  +  Keycloak roomy_user_id attribute
     → attendance employees.user_id (directory)
```

The administrator additionally retains the `Administrator` realm role (carried as `HiredRole.Administrator`),
so admin capabilities are unchanged (FR-005).

## State / lifecycle

Reuses the existing hire saga states (organization `Employee` provisioning → identity `User` active),
eventual consistency per ADR-0025. No new states introduced.
