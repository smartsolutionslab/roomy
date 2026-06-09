# Feature Specification: Hire Employee

**Feature Branch**: `008-hire-employee`

**Created**: 2026-06-09

**Status**: Draft

**Covers backlog stories**: completes US4 (create employees) as the **organization-led** hiring entry point and the User↔Employee provisioning saga (ADR-0025).

## Summary

An administrator hires a colleague by providing their details — display name, work email, role
(Employee or Administrator), and an initial password. Hiring **records the employee immediately** and
**provisions their login account** so the colleague can sign in **once provisioning completes**
(eventual consistency, not within the hiring request). Hiring is owned by the organization side; the
login credentials are provided by the identity side; the two converge by a shared identifier. The
feature guarantees **no half-accounts**: if the account cannot be provisioned, the employee is not left
usable — it is marked failed and the failure is observable, with retry possible. This is **backend
only**; the admin UI for hiring is a separate feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Hire a colleague and have their account provisioned (Priority: P1)

An administrator hires a new colleague by supplying their display name, work email, role, and an
initial password. The employee is recorded right away in a *provisioning* state, and their login
account is created in the background. Shortly afterwards the colleague can sign in with the work email
and the initial password, and the employee shows as *active*.

**Why this priority**: This is the core capability — without it, employees cannot be created with a
working login, and the dormant provisioning consumers stay unused. It is the MVP and the whole reason
the saga exists.

**Independent Test**: Hire a colleague with valid details; assert the employee exists in a provisioning
state immediately, and that — once provisioning completes — the colleague can sign in and the employee
is active. Fully testable end-to-end with only this story implemented.

**Acceptance Scenarios**:

1. **Given** an authenticated administrator, **When** they hire a colleague with a display name, an
   unused work email, the Employee role, and an initial password, **Then** the employee is recorded in
   a *provisioning* state and account provisioning is initiated.
2. **Given** an employee whose provisioning has completed, **When** the colleague signs in with the work
   email and the initial password, **Then** the sign-in succeeds and the employee is *active*.
3. **Given** a colleague who has just been hired but whose provisioning has **not** yet completed,
   **When** they attempt to sign in, **Then** the sign-in does not succeed (the account is not usable
   until provisioning completes).
4. **Given** a non-administrator, **When** they attempt to hire a colleague, **Then** the action is
   refused.

---

### User Story 2 - No half-accounts when provisioning fails (Priority: P2)

When the login account cannot be provisioned — for example the work email is already in use by an
existing account, or the credential system is unavailable beyond the retry window — the system must not
leave a usable half-account. The employee is marked *provisioning failed* (not active, cannot sign in),
the failure reason is observable, and no orphaned login account remains.

**Why this priority**: The saga's correctness guarantee. Without it, a failed hire could leave an
employee with no usable login, or a login with no employee — exactly the inconsistency the
cross-service design must prevent.

**Independent Test**: Hire a colleague whose work email is already in use; assert the employee ends in a
*provisioning failed* state with a visible reason, no active employee exists for that email, and no
usable login account was created. Repeat with the credential system unavailable and confirm retries
occur before the employee is marked failed.

**Acceptance Scenarios**:

1. **Given** a hire whose work email is already in use by an existing account, **When** provisioning is
   attempted, **Then** provisioning fails, the employee is marked *provisioning failed* with the reason,
   and no second login account is created.
2. **Given** a hire whose provisioning fails transiently (the credential system is briefly unavailable),
   **When** provisioning is retried, **Then** it eventually completes and the employee becomes *active*
   without the administrator re-submitting.
3. **Given** a hire whose provisioning is abandoned after exhausting retries, **When** the saga gives up,
   **Then** the employee is left in a *provisioning failed* state (never *active*), so no usable
   half-account persists.

---

### User Story 3 - Hiring is safe to repeat (idempotent) (Priority: P3)

Hiring the same colleague is safe against duplicates: re-processing the same hire (e.g. a retried
message or a double submission of the same request) does not create a second employee or a second login
account. A deliberate hire of a **different** person with an already-used work email is treated as the
"email in use" failure (US2), not a silent duplicate.

**Why this priority**: At-least-once delivery and retries are inherent to the cross-service design;
without idempotency they would produce duplicate accounts. It protects the integrity of the first two
stories but is not itself user-visible value.

**Independent Test**: Re-deliver the same hire twice; assert exactly one employee and one login account
result. Submit a new hire reusing an existing work email; assert it is rejected as "email in use", not
duplicated.

**Acceptance Scenarios**:

1. **Given** a hire that has already been recorded, **When** the same hire is processed again, **Then**
   no second employee and no second login account are created.
2. **Given** the provisioning step is delivered more than once, **When** it is re-processed, **Then**
   exactly one login account exists for the employee.

---

### Edge Cases

- **Email already in use**: a hire whose work email belongs to an existing account fails provisioning
  and the employee is marked failed (US2), because email ownership lives on the credential side and
  cannot be reserved atomically at hire time.
- **Credential system unavailable**: provisioning is retried (at-least-once) and converges to success,
  or — if abandoned — leaves the employee failed; the administrator is never silently left with a
  dangling employee.
- **Missing or invalid details**: a hire missing a display name, a well-formed work email, a role, or an
  initial password is rejected at hire time, before any employee is recorded.
- **Convergence window**: between hire and provisioning completion there is a brief window in which the
  employee exists but cannot yet sign in; this window is expected and observable via the employee's
  state.
- **Seeded administrator**: the bootstrap `DefaultAdmin` account is provisioned at system startup and
  does **not** go through this interactive hiring flow.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Only an authenticated **administrator** MUST be able to hire an employee.
- **FR-002**: Hiring MUST require a display name, a well-formed work email, a role (**Employee** or
  **Administrator**), and an initial password; a hire missing any of these MUST be rejected before any
  employee is recorded.
- **FR-003**: On a valid hire, the system MUST record the employee immediately in a **provisioning**
  state and initiate provisioning of the login account.
- **FR-004**: The colleague MUST be able to sign in **only once provisioning has completed** — not
  within the hiring request and not while the employee is still provisioning or has failed (eventual
  consistency, re-casting `001` scenario 4's "log in immediately").
- **FR-005**: On successful provisioning, the employee MUST become **active**, signing in with the work
  email and the initial password under the assigned role.
- **FR-006**: The employee and its login account MUST be linked one-to-one by a shared identifier; the
  identifier MUST be the correlation key used across the hiring and credential sides.
- **FR-007**: If provisioning fails (e.g. the work email is already in use) or is abandoned after
  retries, the system MUST mark the employee **provisioning failed** with an observable reason, MUST NOT
  mark it active, and MUST NOT leave a usable login account without an active employee or an active
  employee without a usable login account (no half-accounts).
- **FR-008**: Provisioning MUST be retryable and tolerant of at-least-once delivery: transient failures
  are retried until they converge, and re-processing the same hire or provisioning step MUST NOT create
  a duplicate employee or duplicate login account (idempotent).
- **FR-009**: The initial password MUST be treated as a transient secret used only to set the
  credential; it MUST NOT be persisted on the employee or in the hiring record.
- **FR-010**: A hire whose work email is already in use by an existing account MUST resolve to a
  provisioning failure for that employee (US2), never a silent duplicate or a takeover of the existing
  account.
- **FR-011**: The hiring and credential sides MUST communicate only by shared identifiers and
  asynchronous notifications — neither reads or writes the other's data store directly.
- **FR-012**: The seeded `DefaultAdmin` MUST already exist (provisioned at startup) and is outside this
  flow; the first interactive hire MUST be possible without any prior interactive hire.

### Key Entities *(include if feature involves data)*

- **Employee**: a hired colleague within a company — display name, work email, assigned role, the
  company they belong to, the identifier of their login account, and a **provisioning state**
  (*provisioning*, *active*, *provisioning failed* with a reason). Owned by the organization side.
- **Login account**: the colleague's credential — work email, password, and role — by which they sign
  in. Owned by the identity side; linked to exactly one employee by the shared identifier.
- **Hire request**: the administrator's intent to hire — display name, work email, role, and initial
  password — which, when accepted, produces an Employee in the *provisioning* state.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An administrator can hire a colleague by supplying name, work email, role, and initial
  password in a single action, and the employee is recorded immediately (no multi-step wizard).
- **SC-002**: Under normal operation, a hired colleague can sign in within the provisioning convergence
  window (target: a few seconds), measured from hire acceptance to first successful sign-in.
- **SC-003**: 100% of hires reach a terminal, consistent outcome — either *active* (with a usable login)
  or *provisioning failed* (with no usable login) — with **zero** half-accounts (no active employee
  without a login, no login without an active employee).
- **SC-004**: Re-processing the same hire never produces more than one employee or more than one login
  account (0 duplicates across repeated delivery).
- **SC-005**: A hire targeting an already-used work email never takes over or duplicates the existing
  account — it always resolves to a *provisioning failed* employee.

## Assumptions

- **Single company (v1)**: with one seeded company (single-tenant first release), the hired employee is
  assigned to that company implicitly; explicit company selection is deferred until multi-company.
- **Compensation = mark failed, not hard-delete**: an abandoned/failed provisioning leaves the employee
  in a *provisioning failed* state (retained, visible, re-actionable) rather than deleting it, so the
  administrator can see what happened and re-hire; this realizes ADR-0025's "removes or flags" as
  *flags failed*.
- **Email uniqueness is owned by the credential side**: work-email uniqueness is enforced where login
  accounts live; the hiring side cannot reserve an email atomically, so duplicate emails surface as a
  provisioning failure rather than a hire-time rejection.
- **Re-hiring after failure**: a colleague whose hire failed may be hired again (a new hire); resolving
  the underlying cause (e.g. freeing the email) is outside this feature.
- **Identity credential side already exists**: the downstream account-provisioning capability (creating
  the login from a hire and acknowledging success/failure) is already built and is activated by this
  feature; this feature builds the **hiring side** and completes the round-trip.
- **Roles**: the only roles are Employee (base) and Administrator (elevation), consistent with `001`.
- **No UI**: the administrator-facing hiring screen is out of scope here; this spec is the backend
  capability and its observable states.

## Out of Scope

- The administrator-facing **UI** for hiring (a later web feature).
- **Editing or off-boarding** an employee (changing details, deactivating, deleting) — hiring only.
- **Multi-company** assignment and company selection (single-tenant v1).
- **Self-service** sign-up — hiring is administrator-initiated only.
- Bootstrapping the seeded `DefaultAdmin` (handled at startup, ADR-0025 follow-up).

## Review & Acceptance Checklist

- [ ] No implementation details (no event names, services, or storage mechanics in the requirements)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] The eventual-consistency login criterion (FR-004) is unambiguous
- [ ] The no-half-accounts guarantee (FR-007) and compensation policy are explicit
- [ ] Idempotency under at-least-once delivery (FR-008) is covered
- [ ] No open clarification markers remain
