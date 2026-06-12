# Feature Specification: Resilient default-admin provisioning

**Feature Branch**: `021-resilient-admin-provisioning`

**Created**: 2026-06-12

**Status**: Draft

**Covers backlog stories**: the ADR-0025 follow-up flagged by `008-hire-employee` (the bootstrap
`DefaultAdmin` provisioned at startup, out of the interactive hiring flow).

## Summary

The seeded `DefaultAdmin` is provisioned at organization-service startup through the User↔Employee
provisioning saga (ADR-0025). On a cold environment the saga's first step can be lost or fail before the
downstream credential side is ready, leaving the admin employee recorded but **stuck in *provisioning*** —
no login account, unable to sign in. Today the startup seeder is idempotent on *existence* only: once the
admin row exists it never acts again, so a stuck admin **never recovers** on any subsequent startup.

This feature makes admin seeding **converge on a working account**: on each startup the seeder ensures the
admin reaches *active*, re-driving provisioning when the admin exists but is not yet active. Seeding a
healthy (active) admin stays a no-op, and seeding into an empty system still hires the admin. This is
**backend only**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A stuck default admin heals itself (Priority: P1)

The seeded administrator must be able to sign in. If a previous startup left the admin recorded but not
provisioned (no usable login), the next startup re-drives provisioning so the admin becomes active without
any manual database surgery or credential reset.

**Why this priority**: Without it, a single cold-start race permanently locks the only bootstrap
administrator out of the product — there is no in-product path to recover, because every code path that
could create the admin is gated on the admin already existing.

**Independent Test**: Start the service with the admin employee present in a *provisioning* state; assert
the seeder re-initiates provisioning (a fresh provisioning request is emitted for that same admin), and
that — once provisioning completes — the admin is *active* and can sign in.

**Acceptance Scenarios**:

1. **Given** an admin employee recorded but not yet *active* (stuck *provisioning*), **When** the service
   starts and the seeder runs, **Then** provisioning is re-initiated for that same admin (reusing its
   existing identifiers), so it can converge to *active*.
2. **Given** an admin employee in a *failed* state, **When** the seeder runs, **Then** provisioning is
   re-initiated (the admin returns to *provisioning* and is retried), rather than being left failed.
3. **Given** provisioning has been re-initiated and the credential side is now reachable, **When** it
   completes, **Then** the admin becomes *active* and can sign in with the configured initial password.

---

### User Story 2 - A healthy admin and an empty system are unaffected (Priority: P1)

Re-driving must be safe to run on every startup: an already-active admin must not be disturbed, and a
system with no admin must still get one.

**Why this priority**: The seeder runs on every startup. Re-provisioning an active admin, or failing to
seed an empty system, would be a regression of the existing bootstrap guarantee.

**Independent Test**: Run the seeder against (a) an *active* admin and assert nothing is re-initiated and
the admin stays active; (b) an empty system and assert the admin is hired as an administrator.

**Acceptance Scenarios**:

1. **Given** an *active* admin employee, **When** the seeder runs, **Then** provisioning is **not**
   re-initiated and the admin remains *active* (idempotent no-op).
2. **Given** no admin employee exists, **When** the seeder runs, **Then** the admin is hired as an
   administrator with the configured details (unchanged bootstrap behaviour).

---

### Edge Cases

- **Repeated startups**: re-driving runs every startup; for an active admin it is a no-op, so repeated
  restarts do not accumulate duplicate provisioning requests or duplicate accounts.
- **Initial password is transient**: re-driving supplies the configured initial password again to the
  provisioning step; it is never read back from the stored employee (the employee never persists it).
- **Partially-provisioned admin**: this feature targets the observed case where no credential account was
  created. A credential account that exists but is not linked to an *active* admin (a partial provision)
  is **not** fully reconciled here — robust idempotency of the credential side under re-drive is a
  follow-up.
- **Cold-start delivery race (root cause)**: the loss of the admin's first provisioning request on a cold
  boot is not prevented here; convergence is achieved by re-driving on the next startup once the
  downstream side is ready. Hardening first-boot delivery is out of scope.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: On startup the admin seeder MUST ensure the seeded administrator converges to *active*, not
  merely that an admin record exists.
- **FR-002**: When the admin exists but is **not** *active* (*provisioning* or *failed*), the seeder MUST
  re-initiate provisioning for that same admin, reusing its existing identifiers (the same employee and
  its correlation identifier), so the credential side links to the existing employee rather than creating
  a divergent one.
- **FR-003**: Re-initiating provisioning MUST reuse the configured initial password and MUST NOT read a
  password from the stored employee (the employee never persists the password).
- **FR-004**: When the admin is already *active*, the seeder MUST NOT re-initiate provisioning and MUST
  leave the admin unchanged (idempotent no-op).
- **FR-005**: When no admin exists, the seeder MUST hire the admin as an administrator with the configured
  details (unchanged behaviour).
- **FR-006**: Re-initiating provisioning MUST be safe to repeat across restarts: it MUST NOT create a
  second employee for the admin, and once *active* it MUST stop re-initiating.
- **FR-007**: The seeder MUST drive provisioning only through the existing saga (asynchronous
  notifications by shared identifier); it MUST NOT write the credential side's store directly.

### Key Entities *(include if feature involves data)*

- **Employee (admin)**: the seeded administrator on the organization side — with a **provisioning state**
  (*provisioning*, *active*, *failed*) and the correlation identifier of its login account. Re-driving
  acts on this existing record.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From a state where the admin is recorded but not *active*, the admin converges to *active*
  and can sign in after at most one service restart (once the downstream side is reachable), with **no**
  manual database or credential intervention.
- **SC-002**: Running the seeder against an *active* admin makes **zero** provisioning re-initiations and
  leaves the admin *active*.
- **SC-003**: Across repeated restarts, exactly **one** admin employee exists (no duplicates), regardless
  of how many times provisioning was re-driven.
- **SC-004**: A fresh environment yields a signed-in-capable admin without any operator step beyond
  starting the system (possibly across the convergence of one restart).

## Assumptions

- **Heal on next startup**: convergence is achieved by re-driving on the next startup; same-boot
  reconciliation (a background poller) is intentionally out of scope for this slice.
- **Downstream becomes reachable**: the credential side and its messaging topology become reachable on a
  subsequent startup (e.g. durable queues persist), so a re-driven request is delivered.
- **Single seeded admin**: one bootstrap `DefaultAdmin`, consistent with `001`/`008`.

## Out of Scope

- Same-boot reconciliation via a background reconciler/poller.
- Full idempotent reconciliation of a partially-provisioned admin (an existing-but-unlinked credential
  account).

> **Note (scope expansion):** surfacing and retrying silently-swallowed provisioning failures in the
> credential-side consumer — originally deferred — was folded into this slice and is recorded in
> **ADR-0060** (no-swallow consumer, terminal-vs-transient split, bounded retry-with-cooldown).

## Resolved follow-up — why the admin did not converge (#189)

End-to-end verification initially showed the admin still not converging, which looked like a
startup-window durable-inbox problem. The real, pre-existing cause turned out to be **competing
consumers on a shared queue**: `EmployeeHired` is consumed by both identity (provision the account) and
attendance (directory), but Wolverine's conventional routing named both listeners' queue after the
message type alone, so they **shared one queue and competed** — when attendance won the admin's single
saga message, identity never provisioned it. Two fixes resolved it (issue #189): per-service listener
queues so each subscriber gets its own copy (**ADR-0061**), and `organization-api` waiting for Keycloak
so the startup seeder does not publish before identity has declared its queue binding. With those, the
seeded admin provisions on first boot and signs in (verified end to end); this slice's re-drive remains
the safety net for any residual stuck state.

## Review & Acceptance Checklist

- [ ] No implementation details (no event names, services, or storage mechanics in the requirements)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] The idempotent-no-op guarantee for an active admin (FR-004) is unambiguous
- [ ] The reuse-existing-identifiers guarantee (FR-002) is explicit
- [ ] No open clarification markers remain
