# Feature Specification: Occupancy Views

**Feature Branch:** `004-occupancy`
**Status:** Draft
**Created:** 2026-06-05
**Updated:** 2026-06-05
**Covers backlog stories:** US6 (occupancy as lists), US7 (occupancy as calendar), US9 (view own reservations)

## Summary

Employees view how full rooms and offices are, so they can plan when and where to come in. Occupancy is shown per room (e.g. 3/8) and as an office rollup that sums its rooms (e.g. 12/30), as lists (per day, week, or month) and as a calendar. For today and the following day the view also shows who is booked in each room; for other days only the counts are shown. Employees can view all of their own reservations, and the calendar highlights their own bookings. Occupancy lists and the calendar are read-only; past days can be viewed as history.

## User Scenarios & Testing

### Primary User Story
As an employee, I want to see how occupied each room and office is for upcoming days, so that I can plan when and where to come in.

### Acceptance Scenarios

1. **Room occupancy for a single day**
   - GIVEN room "Munich / A1" has capacity 8 and 3 reservations for a day
   - WHEN an employee views that day's occupancy for the room
   - THEN it shows 3 of 8 occupied

2. **Office rollup**
   - GIVEN office "Munich" has rooms totalling capacity 30 and 12 reservations across them for a day
   - WHEN an employee views the office-level occupancy for that day
   - THEN it shows 12 of 30 occupied (the sum of its rooms)

3. **Occupancy for a week or month**
   - GIVEN a room (or office) and a selected week or month
   - WHEN an employee views the occupancy list for that range
   - THEN each day in the range shows its occupied-vs-capacity figure

4. **Names shown only for today and tomorrow**
   - WHEN an employee views occupancy for today or for the following day
   - THEN the names of the employees booked in each room that day are shown alongside the counts
   - AND for any other day, only the counts are shown

5. **Calendar view with own bookings highlighted**
   - WHEN an employee opens the occupancy calendar
   - THEN each day shows its occupancy
   - AND the days on which the employee holds a reservation are highlighted

6. **View own reservations**
   - GIVEN an employee with reservations in the past and the future
   - WHEN they view their own reservations
   - THEN all of them are listed (past, today, and future) with office, room, and day
   - AND future reservations can be cancelled while past ones cannot

7. **Any office or room is viewable**
   - GIVEN several offices and rooms
   - WHEN an employee views occupancy
   - THEN they can see the occupancy of any room and any office

8. **Past occupancy is viewable, read-only**
   - WHEN an employee views occupancy for a past day
   - THEN the historical occupancy is shown
   - AND nothing can be changed from that view

9. **Full room is recognizable**
   - GIVEN a room at full capacity (e.g. 8 of 8)
   - WHEN an employee views that day
   - THEN the room is clearly distinguishable as full before they attempt to book

### Edge Cases
- A room with no reservations on a day shows 0 of its capacity; its office rollup excludes nothing.
- Occupancy reflects the data at the time the view is opened; it does not update live.

## Requirements

### Functional Requirements
- **FR-001:** An employee MUST be able to view a room's occupancy for a selected day, week, or month, shown as occupied places against the room's capacity (e.g. 3/8).
- **FR-002:** An employee MUST be able to view an office-level rollup, equal to the sum of its rooms' occupancy against the sum of their capacities (e.g. 12/30).
- **FR-003:** An employee MUST be able to view occupancy in a calendar, which MUST highlight the days on which the viewer holds a reservation.
- **FR-004:** An employee MUST be able to view all of their own reservations (past, present, and future), each with its office, room, and day. Future reservations may be cancelled from this list and past ones may not (cancellation behaviour is defined in `003-attendance`).
- **FR-005:** Occupancy MUST be viewable for any room and any office by any authenticated user.
- **FR-006:** Occupancy lists and the calendar MUST be read-only.
- **FR-007:** Occupancy MUST be shown as occupied places against capacity. For today and the following day, the view MUST additionally show the names of the employees booked in each room; for all other days only the counts are shown (data minimisation).
- **FR-008:** A room at full capacity (and an office whose rooms are all full) MUST be distinguishable as full.
- **FR-009:** Occupancy MUST be viewable for past days as well, read-only.
- **FR-010:** Views MUST reflect the latest data at the moment they are opened; live/real-time updates are out of scope for the MVP.

### Key Entities (conceptual)
- **Occupancy** — for a room on a day, occupied places against the room's capacity; for an office, the sum across its rooms. Derived from reservations; not stored as its own thing.
- **Reservation** — defined in `003-attendance`; counted here per room, listed in "my reservations", and highlighted on the calendar.
- **Room / Office** — defined in `002-office-management`; provide the capacity figures.

## Resolved Decisions
- Occupancy is shown **per room** (e.g. 3/8) **and as an office rollup** (sum of rooms, e.g. 12/30).
- Read-only applies to occupancy lists and the calendar; "my reservations" additionally surfaces cancellation of future reservations (per `003-attendance`).
- Any authenticated user can view occupancy for any room and office.
- Names of booked employees are shown only for **today and the following day**, per room (data minimisation); other days show counts only.
- "My reservations" lists **all** of the employee's reservations, including past ones as history, with office, room, and day.
- The calendar **highlights the viewer's own reservations**.
- **Past** occupancy is viewable, read-only.
- No live/push updates in the MVP — views refresh when opened.

## Out of Scope (this feature)
- Live / real-time occupancy updates (push).
- Creating, changing, or cancelling reservations from the occupancy lists or calendar — reservation behaviour is `003-attendance`.

## Review & Acceptance Checklist
- [ ] No implementation details (no UI framework, query, or projection mechanics)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] Per-room occupancy and office rollup are both covered
- [ ] Read-only scope (lists + calendar) vs. "my reservations" cancellation is unambiguous
- [ ] Name visibility is limited to today and the following day, per room
- [ ] No open clarification markers remain
