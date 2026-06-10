# Feature Specification: Attendance On-Behalf (Administrator acts for an employee)

**Feature Branch:** `feat/009-attendance-on-behalf`
**Status:** Draft
**Created:** 2026-06-10
**Updated:** 2026-06-10
**Realizes (frontend of):** `003-attendance` story AT-6 (administrator reserves/cancels on behalf of an
employee)

## Summary

The administrator on-behalf surface: an admin-only screen that lets an administrator **reserve a place
for any employee** and **cancel that employee's upcoming reservations**. The attendance backend already
authorises both — `POST /reservations` accepts an administrator-only `onBehalfOf`, and an administrator
may cancel anyone's reservation (`003` FR-011, the owner-or-admin check) — so the gaps are read-only: a
**directory of employees** to pick from, and a way to **see the chosen employee's reservations** to
cancel them. Both come from attendance's own `Employees`/`Reservations` read models (no cross-context
join, ADR-0014).

This builds on `007`: it reuses `@roomy/attendance-data-access` (the catalogue, day helpers, branded
ids, reserve/cancel) and the `007` reserve flow, adding an admin-gated **on-behalf page** to
`@roomy/attendance-feature`. The section is gated to **administrators** via `adminGuard` (ADR-0040).
Token-free through the gateway (ADR-0013/0030); localized (DE + EN, ADR-0024); accessible (WCAG 2.2 AA,
ADR-0024); standalone/signal-based/zoneless/OnPush (ADR-0016/0035); typed client drift-gated (ADR-0036).

## User Scenarios & Testing

### Primary User Story
As an administrator, I want to reserve and cancel places on behalf of an employee, so that I can manage
attendance for colleagues who cannot do it themselves.

### Acceptance Scenarios

1. **Reserve on behalf**
   - GIVEN an administrator on the on-behalf page who has selected an employee
   - WHEN they pick an office, a room, and a bookable day and confirm
   - THEN the reservation is created for that employee and a localized confirmation is shown

2. **See the employee's reservations**
   - GIVEN an administrator who has selected an employee
   - WHEN the page loads that employee's reservations
   - THEN it lists them with office, room, and day, distinguishing upcoming from past

3. **Cancel on behalf**
   - GIVEN an administrator viewing an employee's upcoming reservation
   - WHEN they cancel it
   - THEN the reservation is removed and the place is freed

4. **The same booking rules apply**
   - WHEN an administrator reserves on behalf and the room is full / the employee already has a
     reservation that day / the day is not bookable
   - THEN the action is rejected with the same localized messages as self-service (room full, only one
     per day, only working days within two weeks)

5. **Past reservations cannot be cancelled**
   - GIVEN a past reservation in the employee's list
   - WHEN the administrator views it
   - THEN no cancel action is offered (a server `past_immutable` rejection is surfaced if it occurs)

6. **Employees cannot reach the page**
   - GIVEN a signed-in non-administrator employee
   - WHEN they navigate to the on-behalf route
   - THEN they are redirected to the not-authorized view, and its nav entry is not offered

7. **Unauthenticated visitor is sent to sign in**
   - GIVEN no session
   - WHEN they navigate to the on-behalf route
   - THEN they are redirected to `/bff/login` with a return URL

8. **Language switch localizes the screen** (DE + EN)

### Edge Cases
- No employee selected yet → the reserve form and reservation list are not shown; prompt to pick one.
- The employee has no reservations → empty-state, with the reserve form still available.
- A reserve/cancel request fails (network/5xx) → non-blocking error; the screen stays usable.
- The employee directory is empty → empty-state on the picker.
- A `401` mid-session → treat as signed out (offer sign-in).

## Requirements

### Functional Requirements
- **FR-001:** An administrator MUST be able to choose an employee from a directory of employees
  (`GET /reservations/employees`, administrator-only) sourced from the attendance context.
- **FR-002:** An administrator MUST be able to reserve a place for the chosen employee — office → room →
  bookable day — via `POST /reservations` with `onBehalfOf` set to that employee; the `007` booking
  rules and error surfacing apply unchanged (FR-004 below).
- **FR-003:** An administrator MUST be able to view the chosen employee's reservations
  (`GET /reservations/by-employee/{employeeId}`, administrator-only), distinguishing upcoming from past.
- **FR-004:** An administrator MUST be able to cancel the chosen employee's upcoming reservation
  (`DELETE /reservations/{id}?date=`, which the backend already authorises for an administrator); no
  cancel action is offered for a past reservation, and a `past_immutable` rejection is surfaced.
- **FR-005:** Reserve rejections (`room_full`, `already_reserved_today`, `not_bookable`, `unknown_room`,
  `concurrency_retry_exhausted`) MUST surface the same localized messages as the self-service flow.
- **FR-006:** The on-behalf route MUST require the **administrator** role (`adminGuard`); a
  non-administrator MUST be redirected to the not-authorized view and MUST NOT be offered its nav entry;
  an unauthenticated visitor MUST be redirected to `/bff/login` with a `returnUrl`.
- **FR-007:** All text MUST be localized via Transloco (DE + EN); no hardcoded strings (ADR-0024).
- **FR-008:** The screen MUST meet the WCAG 2.2 AA baseline (labelled controls, announced reserve/cancel
  results, keyboard operable, visible focus) (ADR-0024).
- **FR-009:** No token is ever handled by the SPA (ADR-0013); all calls are same-origin via the gateway
  (ADR-0030), through the drift-gated generated client (ADR-0036). The new reads are administrator-only
  on the server, not merely hidden in the UI.

### Key Entities (view models)
- **Employee** — id, display name. An option in the on-behalf picker.
- **MyReservation** (reused from `007`) — the chosen employee's reservation rows.

## Out of Scope (this feature / deferred)
- Bulk/recurring on-behalf booking; selecting a seat within a room.
- Editing a reservation in place (change is still cancel + re-reserve).
- Any occupancy view (that is `008`); any self-service flow (that is `007`).
- Managing the employee roster itself (hiring/roles — that is identity/organization).

## Review & Acceptance Checklist
- [ ] No implementation details in the spec body
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] The new reads are administrator-gated on the server (not UI-only)
- [ ] Token-free (BFF) + localization + accessibility posture explicit
- [ ] Same booking rules / error surfacing as `007` reused
- [ ] No open clarification markers remain
