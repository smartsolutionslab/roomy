# Feature Specification: Administrator is also an employee

**Feature Branch**: `019-admin-as-employee`

**Created**: 2026-06-11

**Status**: Draft

**Input**: User description: "Provision the seeded DefaultAdmin as an Employee so administrators can create and view their own reservations. Today the DefaultAdmin is created as an identity User only and bypasses the hire saga (ADR-0025), so it has no Employee record in the organization context and is therefore absent from the attendance employee directory. As a result, an administrator gets 404 unknown_employee when creating a reservation (POST /reservations) or viewing their own (/reservations/mine), while normal hired employees work correctly. This violates the stated 1:1 User↔Employee invariant. The fix: the DefaultAdmin must also be provisioned as an Employee, linked to the same UserId, so an administrator can book and view their own reservations like any employee."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Administrator plans their own attendance (Priority: P1)

An administrator is also a person who comes into the office. Like any colleague, they
need to reserve a desk for a working day and see the days they have already booked.
Today this is impossible: when the administrator tries to reserve a desk or open their
own reservations, the system does not recognise them as a member of staff and the action
fails, even though every ordinary employee can do it.

**Why this priority**: This is the whole point of the feature and the only observable
user-facing gap. Without it the administrator cannot use the core attendance feature at
all; with it, the administrator becomes a first-class attendance participant.

**Independent Test**: Sign in as the built-in administrator on a freshly set-up system,
reserve a desk for a future working day, then open "my reservations" — the booking is
created and then listed. Delivers the complete value on its own.

**Acceptance Scenarios**:

1. **Given** a freshly set-up system and the built-in administrator signed in, **When** the administrator opens their own reservations before booking anything, **Then** they see an empty list (not an error).
2. **Given** the administrator signed in, **When** they reserve a desk in an office/room for a bookable future day, **Then** the reservation is created successfully.
3. **Given** the administrator has made a reservation, **When** they open their own reservations, **Then** the reservation they made appears in the list.
4. **Given** the administrator signed in, **When** they perform an administrative action (e.g. manage offices, rooms, or users), **Then** it still succeeds — gaining staff capabilities does not remove administrative ones.

---

### User Story 2 - Every account maps to exactly one staff member (Priority: P2)

The platform guarantees that every user account corresponds to exactly one member of
staff. The built-in administrator account must not be an exception to this rule, so the
data stays consistent and the administrator is resolvable wherever a staff member is
expected.

**Why this priority**: It is the underlying correctness guarantee that makes Story 1
work and keeps the model consistent, but it is not directly observed by an end user, so
it ranks below the user-facing capability.

**Independent Test**: Inspect the built-in administrator after set-up — exactly one staff
record is linked to its account; run set-up again and confirm no duplicate appears.

**Acceptance Scenarios**:

1. **Given** set-up has completed, **When** the built-in administrator's staff records are counted, **Then** there is exactly one, linked to the same account identity.
2. **Given** the administrator's staff record already exists, **When** set-up runs again, **Then** no second staff record is created and no error occurs.

---

### Edge Cases

- **Repeated set-up**: running set-up more than once must not create a duplicate staff record for the administrator, and must not fail.
- **Pre-existing administrator without a staff record**: a system set up before this feature has an administrator account with no staff record; after the feature is in place and set-up runs, the administrator gains exactly one staff record.
- **Past / non-bookable day**: an administrator reserving for a day that is not bookable is rejected with the same rules as any employee (no special-casing for administrators).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The built-in administrator account MUST correspond to exactly one staff (employee) record, linked to the same account identity.
- **FR-002**: An administrator MUST be able to reserve a desk for themselves for a bookable day, with the same rules and outcomes as an ordinary employee.
- **FR-003**: An administrator MUST be able to view their own reservations, seeing an empty list before any booking and their bookings afterwards.
- **FR-004**: The administrator's staff record MUST be discoverable wherever the current user is resolved to a staff member, so attendance actions recognise the administrator.
- **FR-005**: Gaining staff capabilities MUST NOT remove or weaken the administrator's existing administrative capabilities (managing offices, rooms, and users).
- **FR-006**: Set-up MUST be idempotent — running it repeatedly MUST NOT create a duplicate staff record for the administrator and MUST NOT error.
- **FR-007**: The administrator's staff record MUST belong to the same company as other seeded staff.

### Key Entities *(include if feature involves data)*

- **User account**: the sign-in identity, carrying the administrative elevation. One per person.
- **Staff member (employee)**: the office-attendance identity used to reserve desks and list one's own reservations; exactly one per user account, belonging to a company.
- **Reservation**: a desk booking owned by a staff member for a given day.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After initial set-up, an administrator can create a desk reservation and then see it in their own reservations with a 100% success rate (no `unknown_employee` / not-found error on either action).
- **SC-002**: Exactly one staff record exists for the built-in administrator, and repeated set-up runs produce zero duplicates.
- **SC-003**: All existing employee reservation and "my reservations" flows continue to work unchanged.
- **SC-004**: The administrator retains 100% of administrative capabilities after the change.

## Assumptions

- The built-in administrator belongs to the same single seeded company as other staff; no separate company is created for the administrator.
- The administrator's staff display name reuses the administrator account's existing display name.
- A staff member is not tied to a specific office; office and room are chosen per reservation, so no office assignment is needed for the administrator.
- This feature concerns the **built-in / seeded administrator**. Administrators who were first hired as employees and later elevated already have a staff record and are unaffected.
- Letting an administrator reserve *on behalf of* another employee is **out of scope** here; this feature makes the administrator a normal staff member who books for themselves.
- The existing User↔Employee provisioning mechanism (per ADR-0025) is reused; no new cross-context integration pattern is introduced.
