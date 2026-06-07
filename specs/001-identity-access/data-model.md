# Phase 1 Data Model: Identity & Access

Scope: the `identity` bounded context only. Credentials are **not** modelled here — they live in
Keycloak (research R1/R2). This model is the account/role projection the context owns and the
source of its domain/integration events.

## Aggregate: `User`

The consistency boundary for an account and its role assignment. One `User` per account.

| Field | Type (value object) | Rules / invariants |
|---|---|---|
| `UserId` | `UserId` (branded GUID) | Identity; immutable; assigned on registration. |
| `Email` | `Email` | Required; syntactically valid; **unique** across the system (FR-009). |
| `DisplayName` | `DisplayName` | Required; non-empty. |
| `Role` | `Role` | `Employee` (always present) with optional `Administrator` elevation (FR-001/FR-002). |
| `KeycloakSubjectId` | `KeycloakSubjectId` (branded GUID) | Link to the Keycloak user; set once provisioning succeeds. |
| `Status` | `UserStatus` | `Provisioning` → `Active`. (No deactivate/delete in MVP — out of scope.) |

**Invariants**
- Every `User` holds the `Employee` role; `Administrator` is an elevation, never a standalone role.
- `Email` is valid and unique; enforced by the aggregate and a DB unique constraint.
- A `User` is `Active` (loginable) only after the Keycloak user exists and the role is assigned.
- All invariants are enforced with `Ensure.That(...)` in the value objects (no primitive obsession).

**State transitions**
```
(none) --RegisterUser--> Provisioning --Keycloak user created + role assigned--> Active
```

## Value Objects

- **`UserId`** — branded GUID identity.
- **`Email`** — trimmed, lower-cased, format-validated; equality by normalized value.
- **`DisplayName`** — non-empty, trimmed.
- **`Role`** — `Employee` base + `IsAdministrator` flag; exposes `Grant Administrator` / capability checks.
- **`KeycloakSubjectId`** — branded GUID referencing the Keycloak subject.
- **`UserStatus`** — enum-like VO: `Provisioning`, `Active`.

## Domain Events

- **`UserRegistered`** — `{ UserId, Email, Role, KeycloakSubjectId, OccurredAt }` — raised when a
  user is fully provisioned (Active).
- **`AdministratorGranted`** — `{ UserId, OccurredAt }` — raised when an account is elevated to
  Administrator (IA-4).

> Login itself happens at Keycloak/BFF, not in this aggregate's write path. A login-audit signal,
> if needed, is recorded from the BFF, not by mutating `User`. Treated as optional for the MVP.

## Cross-context relationships (by ID only — no type references)

- **`User` (identity) 1:1 `Employee` (organization)** by `UserId`. The `Employee` aggregate in the
  organization context references `UserId`; identity never references `Employee`. The 1:1 is
  established by the provisioning saga (ADR-0025), not by a foreign key.

## Persistence notes (infrastructure only)

- EF Core mapping of `User` to a single table with a unique index on `Email` and on
  `KeycloakSubjectId`. Value objects mapped as owned/converted types.
- Integration events published via the transactional outbox (ADR-0005/0012) in the same
  transaction as the `User` write.
- **No** credential/password columns.
