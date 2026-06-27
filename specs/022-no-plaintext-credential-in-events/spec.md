# Feature Specification: No plaintext credential in events

**Feature Branch**: `022-no-plaintext-credential-in-events`

**Created**: 2026-06-27

**Status**: Draft

**Covers**: a security + primitive-obsession finding in the User↔Employee provisioning saga (ADR-0025) —
the initial login credential travels as a cleartext `string` through the hiring fact, so it is written
into durable event storage and republished on the wire.

## Summary

Hiring an employee in **organization** raises a hiring fact that carries the employee's **initial
password as a raw `string`**. That fact is persisted (event store / transactional outbox per ADR-0037)
and mapped onto an integration event that is republished cross-service (ADR-0031), where the
**identity** context consumes it to create the login account in Keycloak (ADR-0025). As a result a
**cleartext credential lands in durable, replayable event payloads and on the message bus** — readable by
anyone with database, broker, or log access, and retained for the lifetime of those stores. The same raw
`string` is also the **only primitive-obsession hole** in an otherwise value-object-pure domain: the
hiring fact and the aggregate's hire/retry operations take a bare `string` where every other domain
concept is a typed value object.

This feature removes the credential from every **stored and transmitted** event payload while
**preserving provisioning behaviour**: a newly hired employee, and the seeded `DefaultAdmin`, must still
end up with a working login they can sign in with using the expected initial password. The credential
must reach the identity side **out of band** — generated on the identity side, or carried as a one-time,
non-persisted secret — never embedded in a fact that organization stores or republishes. This is
**backend only**.

This change alters a cross-context contract and the provisioning saga's credential flow, so it is
**architectural**: an ADR recording the chosen mechanism (out-of-band generation vs. one-time transient
secret) and its security rationale MUST be written **before** implementation, per CLAUDE.md golden rule 4.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The initial credential never lands in a stored or published event (Priority: P1)

When an employee is hired (including the seeded admin), the initial login credential must not be written
into any durable event payload (event store, outbox) or placed onto the message bus (integration event,
consumer inbox). A reader with database, broker, or log access must not be able to recover the password
from a hiring or provisioning record.

**Why this priority**: This is the security defect. A cleartext credential in a replayable, long-retained
store is a standing exposure independent of any single breach — it widens the blast radius of database,
broker, or log access and violates the project's value-object-pure, secret-handling expectations. Nothing
else in this feature matters if the credential is still on disk and on the wire.

**Independent Test**: Hire an employee through the normal flow, then inspect every artifact the flow
produces — the persisted domain-event/outbox rows, the integration message captured at the broker, and
the consumer inbox — and assert the configured initial password value appears in **none** of them, while
provisioning still completes.

**Acceptance Scenarios**:

1. **Given** an employee is hired, **When** the hiring fact is persisted, **Then** no stored event /
   outbox payload contains the initial credential in any readable or recoverable form.
2. **Given** an employee is hired, **When** the provisioning notification is published cross-service,
   **Then** the transmitted integration event (and any consumer inbox record of it) contains no initial
   credential.
3. **Given** provisioning is re-driven for an existing employee (retry / reconvergence, per
   `021-resilient-admin-provisioning`), **When** the re-drive emits its fact, **Then** that fact likewise
   carries no credential and the credential is **not** read back from the stored employee.

---

### User Story 2 - A hired employee still gets a working login (Priority: P1)

Removing the credential from the payload must not break provisioning. A newly hired employee, and the
seeded `DefaultAdmin`, must still converge to an active login account and be able to sign in with the
expected initial password.

**Why this priority**: The whole point of the saga is that hiring yields a usable account. A fix that
secures the payload but leaves people unable to log in is a regression of the core bootstrap and hiring
guarantees (ADR-0025, `008-hire-employee`, `021-resilient-admin-provisioning`).

**Independent Test**: Run the end-to-end hire flow and the admin seed against the real stack; assert the
account is created in the identity provider and the employee/admin can authenticate with the expected
initial password — exactly as before this change.

**Acceptance Scenarios**:

1. **Given** an employee is hired, **When** provisioning completes, **Then** a login account exists and
   the employee can sign in with the expected initial password.
2. **Given** the seeded `DefaultAdmin` on a fresh environment, **When** provisioning completes, **Then**
   the admin can sign in with the configured initial password (eventual consistency, ADR-0025).
3. **Given** the identity provider rejects the credential by policy, **When** provisioning runs, **Then**
   that terminal failure is still reported as today (no silent success, no infinite retry), unchanged by
   this feature.

---

### User Story 3 - The domain stops modelling the credential as a raw primitive (Priority: P2)

The hiring fact and the aggregate's hire/retry operations must no longer carry the initial credential as
a bare `string` captured into persisted aggregate state or a raised event — closing the lone
primitive-obsession hole in the domain.

**Why this priority**: It is a code-quality and consistency defect rather than a live exposure (US1
covers the exposure), but it is the root shape that *let* the credential leak into the event in the first
place. Closing it makes the secure flow the only expressible flow, so the leak cannot silently return.

**Independent Test**: Inspect the domain's hire/retry surface and the raised hiring fact; assert neither
accepts nor carries a raw-`string` credential that is persisted into aggregate state or a stored/raised
event — the concept is either absent from the domain or represented as a transient, non-persisted value.

**Acceptance Scenarios**:

1. **Given** the hiring aggregate operation, **When** an employee is hired, **Then** no raw-`string`
   credential is stored in the aggregate's persisted state.
2. **Given** the raised hiring fact, **When** it is constructed, **Then** it does not include a
   raw-`string` credential field that is serialized into the stored/published payload.

---

### Edge Cases

- **Re-drive must not depend on stored credentials**: because the employee never persists the credential,
  re-driving provisioning (`021`) MUST obtain it the same out-of-band way as the first attempt — never by
  reading it back from a stored employee or a prior stored event.
- **Password-policy rejection preserved**: the existing terminal `password_rejected` path (and the
  `email_taken` terminal path) MUST behave exactly as today, including how it is surfaced and not retried.
- **Logs are payloads too**: the credential MUST NOT be written to application logs or traces as a
  side effect of the new flow (a secured event payload but a logged secret is not a fix).
- **Historical data already on disk**: stored events / outbox rows written *before* this change may still
  contain the old cleartext credential; purging or migrating that historical data is a separate
  data-cleanup concern (see Out of Scope).
- **Replay / re-projection**: replaying the new credential-free events MUST still drive provisioning
  correctly; it MUST NOT require a credential that is no longer present in the payload.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A hiring fact persisted by the organization context (event store and/or transactional
  outbox) MUST NOT contain the initial credential in any readable or recoverable form.
- **FR-002**: An integration event published cross-service for a hiring/provisioning, and any consumer
  inbox record of it, MUST NOT contain the initial credential.
- **FR-003**: The initial credential MUST reach the credential side (identity) only as a **transient,
  non-persisted** value at provisioning time, or be **generated on the identity side** — it MUST NOT
  round-trip through organization's stored aggregate state or any stored/published event.
- **FR-004**: Hiring an employee MUST still result in a working login account; the hired employee MUST be
  able to sign in with the expected initial password once provisioning completes.
- **FR-005**: The seeded `DefaultAdmin` MUST still provision to a usable login and sign in with the
  configured initial password (eventual consistency, ADR-0025).
- **FR-006**: Re-driving provisioning (retry / reconvergence, `021`) MUST still yield a working login
  **without** reading the credential from a persisted employee record or a stored event.
- **FR-007**: The domain's hire and retry operations and the raised hiring fact MUST NOT carry the
  initial credential as a raw-`string` value that is captured into persisted aggregate state or a
  stored/serialized event payload.
- **FR-008**: The credential MUST NOT be written to application logs or traces as a side effect of the
  provisioning flow.
- **FR-009**: The existing terminal-failure handling for an unacceptable credential (policy rejection)
  and a taken email MUST remain unchanged (reported, not silently swallowed, not retried indefinitely).

### Key Entities *(include if feature involves data)*

- **Initial credential (one-time secret)**: the bootstrap password used **once** to create the login
  account. After this change it has no home in organization's stored state and appears in no stored or
  published event; it exists only transiently at provisioning time (or is produced on the identity side).
- **Employee (hiring fact)**: the organization-side record/event that triggers provisioning by ID. After
  this change it carries the identifiers and profile needed to provision, but **not** the credential.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Inspecting every persisted event / outbox / inbox payload produced by a hire and by a
  re-drive yields **zero** occurrences of the initial credential value.
- **SC-002**: A captured broker / wire payload for a hire contains **zero** occurrences of the initial
  credential value.
- **SC-003**: After a hire (and after the admin seed) completes, the employee/admin can sign in with the
  expected initial password — login success rate unchanged from before this feature.
- **SC-004**: The existing provisioning test suites (admin seeding, hire-employee saga end to end, and
  provisioning retry) remain green with no test weakened to accommodate the change.
- **SC-005**: No domain hire/retry operation or raised hiring fact exposes a raw-`string` credential that
  is persisted or serialized (the primitive-obsession hole is closed).

## Assumptions

- **Out-of-band credential delivery exists or can be added**: the identity side can either generate the
  initial credential itself or receive it through a transient channel that is never persisted or
  serialized into the event store, outbox, inbox, or broker. The chosen mechanism is decided in the ADR.
- **Initial password remains configured for the admin**: the seeded `DefaultAdmin`'s initial password
  continues to come from configuration (`DefaultAdmin:InitialPassword`); only its *path to identity*
  changes so it is no longer embedded in a stored/published event.
- **Eventual login**: "can sign in" means once provisioning completes, consistent with ADR-0025.
- **Architectural change requires an ADR first**: this alters a cross-context contract and the saga's
  credential flow, so an ADR (mechanism + security rationale) is written before implementation.

## Out of Scope

- Purging or migrating credentials already written to historical event-store / outbox / inbox rows
  before this change (a separate data-cleanup / retention concern).
- Changing the password policy, credential strength, or the admin's configured password value.
- Any frontend change — this is backend only.
- Reworking the broader provisioning saga beyond the credential's path (covered by ADR-0025 and `021`).

## Review & Acceptance Checklist

- [ ] No implementation details (no event names, services, or storage mechanics in the requirements)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] The "no credential in any stored or published payload" guarantee (FR-001/FR-002) is unambiguous
- [ ] Provisioning-behaviour preservation (FR-004/FR-005/FR-006) is explicit
- [ ] The primitive-obsession remediation (FR-007) is captured
- [ ] The required ADR is noted before implementation
- [ ] No open clarification markers remain
