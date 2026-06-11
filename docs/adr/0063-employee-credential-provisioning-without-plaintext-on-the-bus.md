# 0063. Provision employee credentials without a plaintext password on the integration bus

- **Status:** Accepted
- **Date:** 2026-06-30
- **Deciders:** Heiko Weiß
- **Relates to:** ADR-0025 (user/employee provisioning saga), ADR-0031 (integration-event contracts), ADR-0014 (microservices, async integration), ADR-0013 (Keycloak/BFF)

## Context and problem statement

Hiring an employee runs the provisioning saga (ADR-0025): an administrator submits an
initial password to organization-api (`HireEmployee`), organization raises `EmployeeHired`
and publishes the integration event `Contracts.Organization.EmployeeHired`, which carries
the **plaintext** `InitialPassword`. Identity consumes it (`EmployeeHiredConsumer` →
`RegisterUser`) and provisions the Keycloak user with that password.

That plaintext credential is therefore **persisted at rest on durable infrastructure** it
should never touch:

- organization's transactional **outbox** table,
- the **broker** (RabbitMQ by default, ADR-0015) — including any dead-letter/retry queues,
- identity's **inbox** table.

Anyone with read access to either database or the broker can read every new employee's
initial password. This is a real data-protection weakness, independent of the architecture
(the saga itself is correct).

## Constraints (what makes this non-trivial)

- **No email/SMTP infrastructure exists** in the system today. A "set your password" email
  link (the textbook fix) is not currently available without first adding SMTP.
- **The dev-seeder depends on a known, shared password.** `apps/dev-seeder` provisions the
  demo dataset (~42 logins) with a single deterministic `EmployeePassword` so every seeded
  account can be logged into. Removing a known credential breaks demo/login UX and the
  manual walkthroughs.
- **Microservices isolation (ADR-0014).** Organization must not call identity synchronously;
  provisioning stays an async saga. There is no distributed transaction to lean on.
- **The event is published language (ADR-0031), drift-gated.** Changing its shape is a
  coordinated producer+consumer change (both live in this repo) plus an OpenAPI/contract
  bump; it is allowed but must be deliberate.
- **Keycloak is the credential store (ADR-0013).** It already supports temporary passwords
  with a forced `UPDATE_PASSWORD` action and `executeActionsEmail` — both available once the
  delivery channel exists.

## Considered options

### Option 1 — Encrypt the secret on the bus
Organization encrypts `InitialPassword` (authenticated symmetric / envelope encryption, key
in configuration alongside the other deployment secrets) before publishing; identity
decrypts just before the Keycloak call. The contract field becomes opaque ciphertext (rename
to `EncryptedInitialPassword`).

- **Pros:** removes plaintext at rest in outbox/broker/inbox — the stated problem. **No SMTP
  needed. No UX change. The dev-seeder keeps deterministic logins** (it encrypts the known
  password). Smallest blast radius; contained to the contract edge + a small crypto helper.
- **Cons:** identity still decrypts to plaintext in memory to set the Keycloak credential, so
  it protects *at-rest-in-transit*, not end-to-end. Introduces a shared key to manage/rotate.
  "Encrypting a password you will immediately use as a password" is a mitigation, not the
  ideal model.

### Option 2 — Keycloak temporary password + forced reset
Provision with `temporary=true` so the employee must change the password at first login
(`UPDATE_PASSWORD` required action).

- **Pros:** limits the blast radius of a leaked initial secret to first login; closer to best
  practice.
- **Cons:** **on its own it does not remove the plaintext from the bus** — a temporary
  password is still transmitted. The employee must still *receive* it (no SMTP → still
  out-of-band). Disrupts the deterministic dev-seeder and the saga-e2e fixture (which already
  has to clear `requiredActions` to log its seeded users in). Best combined with Option 1 or 3.

### Option 3 — Identity owns the secret; nothing crosses the boundary
Organization's event carries **no** password. Identity generates a random temporary secret,
provisions Keycloak with `temporary=true`, and delivers it via an identity-owned channel.

- **Pros:** the credential never crosses a service boundary — architecturally the cleanest;
  fully eliminates the at-rest exposure.
- **Cons:** **needs a delivery channel.** Without SMTP, the admin cannot get the temp secret
  back through the async saga (the hire HTTP response has already returned). Requires either
  SMTP (Option 4) or a new admin-facing "reveal/reset initial credential" endpoint in
  identity. Largest change; breaks the deterministic seeder unless the seeder calls that new
  identity path directly.

### Option 4 — Token/email set-password (target state)
Keycloak `executeActionsEmail` sends the employee a one-time link to set their own password;
no initial secret is ever generated or transmitted.

- **Pros:** the industry-standard model; no credential on the bus, ever.
- **Cons:** **requires SMTP**, which does not exist yet — so this is a *target*, not an option
  available for the next change. Also reshapes the seeder/e2e (no password to log in with).

## Decision

Adopt **Option 1 (encrypt the secret on the bus) now**, and record **Option 4 (email-based
set-password) as the target** once SMTP is provisioned. Rationale: Option 1 directly removes
the at-rest plaintext — the actual reported risk — with no new infrastructure, no UX change,
and no disruption to the dev-seeder or saga-e2e, while staying inside the existing saga and
contract model. Option 2's forced-reset can be layered on top later (cheap) and becomes fully
clean once Option 4's delivery channel exists.

This decision is **accepted**. The implementing change — `Contracts.Organization.EmployeeHired`
gains an opaque `EncryptedInitialPassword` (replacing the plaintext field), an
authenticated-encryption helper with a configured, rotatable key, and the dev-seeder/test
updates — is tracked by issue #197 and lands in its own branch (test-first).

## Consequences

**If Option 1 is accepted**
- `Contracts.Organization.EmployeeHired.InitialPassword` becomes `EncryptedInitialPassword`
  (opaque) — a coordinated producer+consumer change; the OpenAPI/contract artifacts bump.
- A small authenticated-encryption helper and a configured key (per environment, rotatable)
  are introduced; the key is a deployment secret like the others.
- The dev-seeder encrypts its known password — demo logins stay deterministic.
- Integration/e2e tests that assert on `InitialPassword` are updated to the encrypted field.
- Residual: identity still handles plaintext transiently in memory; this is documented as the
  accepted limitation until Option 4.

**General**
- Whichever option is chosen, plaintext credentials in the outbox/broker/inbox stop being an
  accepted state; this ADR is the record of why and of the deferred target.
