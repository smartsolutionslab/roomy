# Feature Specification: Attendance Planning

**Feature Branch:** `003-attendance`
**Status:** Draft
**Created:** 2026-06-05
**Updated:** 2026-06-05
**Covers backlog stories:** US8 (plan attendance), US10 (edit → realized as cancel + reserve), US11 (cancel attendance)

## Summary

Employees plan their office attendance by reserving a place in a specific room of an office for a single day. A day is only bookable if it is a working day (Mon–Fri, Europe/Berlin) within a rolling two-week window. A reservation guarantees a place: the system never lets the number of reservations for a room on a day exceed that room's capacity. Each employee may hold at most one reservation per day. Employees manage only their own reservations; administrators may act on behalf of anyone, and administrators are themselves employees who may also plan their own attendance.

## User Scenarios & Testing

### Primary User Story
As an employee, I want to reserve a place in a specific room of an office for a day, so that I know I will have a desk in that room when I come in.

### Acceptance Scenarios

1. **Successful reservation**
   - GIVEN room "Munich / A1" has capacity 8 and 3 reservations for a working day within the window
   - AND the employee has no reservation that day
   - WHEN the employee reserves a place in "Munich / A1" for that day
   - THEN the reservation is created
   - AND the employee is guaranteed a place in that room

2. **Same-day (spontaneous) reservation**
   - GIVEN today is a working day and the chosen room has remaining capacity for today
   - WHEN the employee reserves a place in that room for today
   - THEN the reservation is created

3. **Room full**
   - GIVEN room "Munich / A1" has capacity 8 and 8 reservations for a day
   - WHEN an employee reserves a place in "Munich / A1" for that day
   - THEN the reservation is rejected
   - AND the employee is informed the room is full

4. **Already reserved that day**
   - GIVEN the employee already holds a reservation in any room for a day
   - WHEN the employee reserves a place in another room for the same day
   - THEN the reservation is rejected
   - AND the employee is informed only one reservation per day is allowed

5. **Past day rejected**
   - WHEN an employee attempts to reserve for a day before today
   - THEN the reservation is rejected

6. **Weekend / non-working day rejected**
   - WHEN an employee attempts to reserve for a Saturday or Sunday
   - THEN the reservation is rejected
   - AND the employee is informed only working days are bookable

7. **Beyond the booking window rejected**
   - GIVEN today is 2026-06-05
   - WHEN an employee attempts to reserve for a day more than 14 calendar days ahead
   - THEN the reservation is rejected
   - AND the employee is informed bookings are limited to the next two weeks

8. **Cancel own reservation**
   - GIVEN the employee holds a reservation for today or a future day
   - WHEN the employee cancels it
   - THEN the reservation is removed
   - AND the place in that room is freed for others

9. **Cancellation frees a place**
   - GIVEN room "Munich / A1" was full for a day
   - WHEN a reserved employee cancels their reservation
   - THEN one place in that room becomes available
   - AND another employee can now reserve it

10. **Administrator acts on behalf**
    - GIVEN an administrator
    - WHEN the administrator reserves or cancels a place for any employee
    - THEN the action succeeds under the same booking rules

11. **Employee cannot modify another's reservation**
    - GIVEN employee X holds a reservation
    - WHEN employee Y attempts to cancel or change it
    - THEN the action is rejected
    - AND employee Y may only view it

12. **Concurrent reservations for the last place**
    - GIVEN a room has exactly one remaining place for a day
    - WHEN two employees attempt to reserve it at the same time
    - THEN exactly one reservation succeeds
    - AND the other is rejected as full
    - AND the room's capacity is never exceeded

13. **Changing room, office, or day**
    - GIVEN the employee holds a reservation
    - WHEN the employee wants a different room, office, or day
    - THEN they cancel the existing reservation and create a new one
    - AND there is no single combined edit step

### Edge Cases
- Cancelling a reservation for a past day is not possible (past is immutable).
- Reserving and then immediately cancelling on the same day is allowed.
- A rejected reservation (room full / past / weekend / outside window / duplicate day) produces a clear error and creates no record.

## Requirements

### Functional Requirements
- **FR-001:** An employee MUST be able to reserve a place in a room of an office for a single bookable day.
- **FR-002:** A day MUST be considered bookable only if it is a working day (Monday–Friday, Europe/Berlin) AND falls within the window of today through today + 14 calendar days (inclusive).
- **FR-003:** A successful reservation MUST guarantee the employee a place in that room on that day.
- **FR-004:** The system MUST reject a reservation when the room has no remaining capacity for that day.
- **FR-005:** The system MUST reject a reservation when the employee already holds a reservation in any room on the same day.
- **FR-006:** The system MUST reject a reservation for any day that is not bookable (past, weekend, or beyond the 14-day window).
- **FR-007:** The number of reservations for a room on a given day MUST never exceed that room's capacity, even under concurrent reservation requests.
- **FR-008:** An employee MUST be able to cancel their own reservation as long as its day has not passed (today or future), which MUST free the place for others.
- **FR-009:** The system MUST NOT allow cancelling or changing a reservation whose day is in the past.
- **FR-010:** Changing the room, office, or day of a reservation MUST be performed as a cancellation followed by a new reservation; no dedicated edit operation exists.
- **FR-011:** An administrator MUST be able to create and cancel reservations on behalf of any employee, subject to FR-002 through FR-007.
- **FR-012:** An employee MUST be able to view, but MUST NOT be able to create, change, or cancel, another employee's reservation.

### Key Entities (conceptual)
- **Reservation** — one employee's guaranteed place in exactly one room on exactly one day. Belongs to exactly one employee.
- **Room capacity** — the number of places available in a room on a day (managed in `002-office-management`).
- **Employee** — the person who reserves (managed in `001-identity-access`).
- **Day** — a calendar day in Europe/Berlin; reservations are limited to working days (Mon–Fri) within a rolling two-week window.

## Resolved Decisions
- A reservation targets a specific **room** (not just an office). Booking flow: office → room → reserve a place.
- Capacity is per **room**; the no-overbooking guarantee is per (room, day).
- At most **one reservation per employee per day**, across all rooms and offices.
- Time zone: Europe/Berlin. Working day: Mon–Fri. Booking window: today through today + 14 calendar days (inclusive).

## Out of Scope (this feature)
- Notifying anyone about reservations or cancellations — only visibility is required.
- Reserving multiple days in a single action.
- Selecting a specific seat within a room (capacity model only — a place in the room).
- Occupancy lists, calendar view, and "my reservations" overview — specified in `004-occupancy`.
- Eviction of reservations when a room's capacity is reduced — capacity changes and the resulting eviction are deferred to post-MVP (planned under `002-office-management`).

## Review & Acceptance Checklist
- [ ] No implementation details (no tech stack, data model, or architecture)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] Reservation clearly targets a room; capacity is per room
- [ ] One-reservation-per-employee-per-day and the no-overbooking guarantee under concurrency are covered
- [ ] Bookable-day rules (working day + two-week window) are covered
