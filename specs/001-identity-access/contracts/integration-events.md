# Integration-Event Contracts: Identity & Access

Cross-context contracts carried over Wolverine with the transactional outbox/inbox
(ADR-0005/0014/0015). Minimal and versioned; payloads use IDs only. These are the primary
contracts of this context — the REST surface (see `identity-api.md`) is a thin read model.

## Consumed

### `EmployeeHired` (from `organization`)
Trigger for provisioning (ADR-0025). Identity reacts by running `RegisterUser`.

| Field | Type | Notes |
|---|---|---|
| `employeeId` | GUID | The organization-side employee identity. |
| `userId` | GUID | Pre-allocated account identity to provision (correlation key). |
| `email` | string | Account email (unique). |
| `displayName` | string | |
| `role` | enum `employee` \| `administrator` | `administrator` implies the employee elevation. |
| `initialPassword` | string (secret) | ≥ 8 chars; set in Keycloak, never persisted by identity. |
| `occurredAt` | timestamp (UTC) | |

## Published

### `UserRegistered`
Emitted when the account is fully provisioned (Keycloak user created + role assigned + record
persisted). Completes the provisioning saga's identity step.

| Field | Type | Notes |
|---|---|---|
| `userId` | GUID | |
| `employeeId` | GUID | Correlation back to the saga. |
| `email` | string | |
| `role` | enum `employee` \| `administrator` | |
| `keycloakSubjectId` | GUID | |
| `occurredAt` | timestamp (UTC) | |

### `UserProvisioningFailed`
Emitted when provisioning cannot complete (e.g. Keycloak rejects, email already taken). Drives saga
compensation in the organization context.

| Field | Type | Notes |
|---|---|---|
| `userId` | GUID | |
| `employeeId` | GUID | |
| `reason` | enum `email_taken` \| `password_rejected` \| `provider_error` | Coarse, non-sensitive. |
| `occurredAt` | timestamp (UTC) | |

## Versioning

Contracts are versioned and additive; breaking a payload requires a new version and a deprecation
window (ADR-0014 "minimal, versioned shared contracts").
