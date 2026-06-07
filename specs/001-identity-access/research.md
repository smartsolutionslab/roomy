# Phase 0 Research: Identity & Access

All items below were resolved before design. Format: Decision / Rationale / Alternatives.

## R1 — Authentication mechanism

**Decision:** Email + password via **Keycloak** (self-hosted OIDC). The YARP gateway is the
OIDC client and holds the session (BFF pattern); the SPA never sees tokens.

**Rationale:** ADR-0013 is Accepted and the constitution says an ADR wins over a conflicting
spec. Keycloak gives password policy, account storage, and a standards-based session for free,
and matches the target stack (ADR-0017 Azure, Aspire-composed locally).

**Alternatives considered:** Custom email+password with a `PasswordHash` in the identity DB
(spec 001's original wording) — rejected: it contradicts ADR-0013 and reinvents credential
storage, hashing, and session management. The spec was amended to conform.

## R2 — Ownership of credentials vs. account/role data

**Decision:** Keycloak owns credentials (verification, hashing, password policy). The identity
service owns the **account/role record** and the link to the Keycloak subject (`KeycloakSubjectId`),
stored in its own PostgreSQL database. No password material is ever stored in the identity DB.

**Rationale:** Single source of truth per concern; keeps the identity DB free of secrets;
satisfies database-per-service (ADR-0014). The app needs an account/role projection it can query
without round-tripping Keycloak for every request.

**Alternatives considered:** Treat Keycloak as the sole store (no identity DB) — rejected: the
domain needs an aggregate to enforce role invariants and to be the outbox source for integration
events; Keycloak is an adapter, not the domain.

## R3 — Account provisioning flow

**Decision:** Provisioning is the **organization-led `HireEmployee` saga** (ADR-0025). On
`EmployeeHired`, the identity service runs `RegisterUser` (create the Keycloak user with the
chosen role + initial password, persist the account/role record), then emits `UserRegistered`.
On failure it emits `UserProvisioningFailed` for the saga to compensate. Login becomes possible
once provisioning completes (eventual consistency).

**Rationale:** ADR-0025 (Accepted). Hiring is the real-world trigger and lives in the supporting
context; identity is the downstream credential/account provider. No distributed transaction.

**Alternatives considered:** Identity-led creation (admin calls identity directly) — rejected per
ADR-0025. Synchronous cross-service creation — rejected: no distributed transactions (ADR-0014).

## R4 — DefaultAdmin seeding

**Decision:** At identity-service startup, seed a `DefaultAdmin` from configuration (email +
initial password from config/secret, never hard-coded) — create the Keycloak user with the
Administrator role and persist the account/role record if absent. Idempotent on restart.

**Rationale:** FR-004 — the system must be administrable from first start, before any saga runs.

**Alternatives considered:** Seeding only in Keycloak realm import — rejected: the identity DB
projection would then be missing; seeding in one place that writes both keeps them consistent.

## R5 — Role model

**Decision:** Two realm roles — `employee` (held by every account) and `administrator`
(elevation). The identity `User` aggregate is the source of truth for the assignment and pushes
it to Keycloak; the role is carried in the token and mapped by the BFF for authorization.

**Rationale:** Mirrors the spec's "every account is an employee; Administrator is an elevation"
(FR-001/FR-002). Keeps authorization claims standards-based.

**Alternatives considered:** Per-permission claims — rejected as over-engineered for the MVP
(YAGNI / Principle VII).

## R6 — No account enumeration on failed login

**Decision:** Rely on Keycloak's generic login failure (it does not distinguish unknown-user from
wrong-password) and surface a single generic error through the BFF.

**Rationale:** FR-008 / edge case — failures must not reveal whether an account exists.

## R7 — Logout

**Decision:** Logout clears the BFF session and performs Keycloak end-session (OIDC RP-initiated
logout). Subsequent actions require a fresh login.

**Rationale:** FR-011.

## R8 — Email uniqueness

**Decision:** Enforced in both places — a unique constraint on `Email` in the identity DB and
Keycloak's unique-email realm setting — with the identity aggregate as the authoritative guard.

**Rationale:** FR-009.
