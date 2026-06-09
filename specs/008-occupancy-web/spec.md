# Feature Specification: Occupancy Web (Occupancy views & calendar)

**Feature Branch:** `feat/008-occupancy-web`
**Status:** Draft
**Created:** 2026-06-10
**Updated:** 2026-06-10
**Realizes (frontend of):** `004-occupancy` stories OC-1 (room occupancy for a day/week/month),
OC-2 (office-level rollup), OC-3 (calendar with my own bookings highlighted), OC-4 (who is booked per
room for today/tomorrow), OC-6 (past occupancy, read-only)

## Summary

The Angular SPA's occupancy surface: the read-only screens that let any signed-in employee see how full
rooms and offices are, so they can plan when and where to come in. Occupancy is shown per room (e.g.
3/8) and as an office rollup that sums its rooms (e.g. 12/30), as a **list** over a day, week, or month,
and as a **calendar** that highlights the days the viewer holds a reservation. For today and the
following day the view also names who is booked in each room; for any other day only the counts are
shown. Past days are viewable as read-only history.

The occupancy backend already exposes the figures behind the gateway (`GET /occupancy?officeId|roomId&
from&to`, ADR-0013) with the today/tomorrow name policy enforced server-side (`004`); this feature adds
the **occupancy list page** (office/room + range picker), the **calendar page** (month grid, own
bookings highlighted), the **route guard** that gates the section to any signed-in employee, and the
**navigation entry** for it.

This builds on `007-attendance-web`: it reuses `@roomy/attendance-data-access` (the bookable office/room
catalogue `GET /rooms`, the branded view models, and the Europe/Berlin day helpers) and adds occupancy
read methods + a new `@roomy/occupancy-feature` (or `attendance-feature` extension — decided in the
plan). All backend calls are same-origin relative URLs through the gateway; no tokens reach the SPA
(ADR-0013/0030). The UI is localized (DE + EN, Transloco, ADR-0024), accessible (WCAG 2.2 AA, ADR-0024),
and standalone/signal-based/zoneless/OnPush (ADR-0016/0035). The typed client stays generated from the
attendance OpenAPI spec and drift-gated (ADR-0036).

> **Not here:** OC-5 (view all my reservations and cancel upcoming) is already delivered by
> `007-attendance-web`'s my-reservations page; it is out of scope for this feature.

## User Scenarios & Testing

### Primary User Story
As an employee, I want to see how occupied each room and office is for upcoming days, so that I can plan
when and where to come in.

### Acceptance Scenarios

1. **Room occupancy for a single day**
   - GIVEN room "Munich / A1" has capacity 8 and 3 reservations for a day
   - WHEN an employee views that day's occupancy for the room
   - THEN it shows 3 of 8 occupied

2. **Office rollup for a day**
   - GIVEN office "Munich" has rooms totalling capacity 30 and 12 reservations across them for a day
   - WHEN an employee views the office-level occupancy for that day
   - THEN it shows 12 of 30 occupied (the sum of its rooms)

3. **Occupancy for a week or month**
   - GIVEN a chosen office (or room) and a selected week or month
   - WHEN an employee views the occupancy list for that range
   - THEN each day in the range shows its occupied-vs-capacity figure

4. **Names shown only for today and tomorrow**
   - WHEN an employee views occupancy for today or the following day
   - THEN the names of the employees booked in each room that day are shown alongside the counts
   - AND for any other day, only the counts are shown

5. **Calendar with own bookings highlighted**
   - WHEN an employee opens the occupancy calendar for a month
   - THEN each day shows its occupancy figure
   - AND the days on which the employee holds a reservation are visibly highlighted

6. **Past occupancy is viewable, read-only**
   - WHEN an employee views occupancy for a past day
   - THEN the historical occupancy is shown
   - AND nothing can be changed from that view

7. **Any office or room is viewable**
   - GIVEN several offices and rooms
   - WHEN an employee chooses any office or any room
   - THEN they can see its occupancy

8. **Unauthenticated visitor is sent to sign in**
   - GIVEN no session
   - WHEN they navigate to an occupancy route
   - THEN they are redirected to `/bff/login` with a return URL back to the route

9. **Any signed-in employee may use it**
   - GIVEN a signed-in employee (administrator or not)
   - WHEN they navigate to the occupancy routes
   - THEN the occupancy screens are shown (this section is NOT administrator-gated)

10. **Language switch localizes the occupancy screens**
    - WHEN the user switches language
    - THEN all labels, headings, day/month names, and figures' surrounding text render in DE or EN

### Edge Cases
- The catalogue has no offices/rooms yet → empty-state on the picker, not a blank screen.
- A range request fails (network/5xx) → non-blocking error, screen stays usable.
- A range wider than the backend bound (more than 31 days) → the UI MUST NOT request it; month/week/day
  presets keep every request within the bound.
- A day with no reservations → shows 0 of N (and the office rollup 0 of its capacity), not a blank cell.
- An office/room unknown to attendance (`404`) → localized "no longer available" + refresh the picker.
- A `401` mid-session on any occupancy request → treat as signed out (offer sign-in), no error dump.

## Requirements

### Functional Requirements
- **FR-001:** A signed-in employee MUST be able to choose an office or a room and a range (day, week, or
  month) and see each day's occupancy — occupied vs capacity — sourced from `GET /occupancy` through the
  gateway (OC-1).
- **FR-002:** When an office is chosen, the view MUST show the office rollup (sum of its rooms' occupied
  and capacity) alongside the per-room figures (OC-2).
- **FR-003:** For today and the following day the view MUST show the names of who is booked in each room;
  for every other day it MUST show counts only — exactly as the backend returns them, never inferring
  names the response withholds (OC-4, the data-minimisation policy).
- **FR-004:** A calendar view MUST present a month as a grid where each day shows its occupancy figure
  and the days on which the viewer holds a reservation are highlighted, sourced from `GET /occupancy`
  (the month range) and `GET /reservations/mine` (the viewer's days) (OC-3).
- **FR-005:** Past days MUST be viewable as read-only history; the occupancy screens MUST offer no
  mutation (no reserve/cancel) — those live in `007` (OC-6).
- **FR-006:** Any office and any room MUST be selectable from the attendance-owned catalogue
  (`GET /rooms`, reused from `007`); the UI MUST keep every range request within the backend's 31-day
  bound via day/week/month presets.
- **FR-007:** The occupancy routes MUST require an authenticated session (any employee — NOT
  administrator-gated); an unauthenticated visitor MUST be redirected to `/bff/login` with a `returnUrl`.
- **FR-008:** All occupancy-screen text MUST be localized via Transloco (DE + EN); no hardcoded
  user-facing strings, including day and month names (ADR-0024).
- **FR-009:** The occupancy screens MUST meet the WCAG 2.2 AA baseline — keyboard operable, correct
  roles/names, visible focus, a non-colour cue for highlighted days, and a table/grid that reads
  correctly to assistive tech (ADR-0024).
- **FR-010:** No access token or refresh token is ever read, stored, or handled by the SPA (ADR-0013);
  all backend calls are same-origin relative URLs through the gateway (ADR-0030), via the client
  generated from the attendance OpenAPI spec and drift-gated in CI (ADR-0036).

### Key Entities (view models)
- **OccupancyDay** — a date, the office rollup (occupied, capacity), and its rooms each with occupied,
  capacity, full flag, and (today/tomorrow only) the list of occupants. One row in the list / one cell
  in the calendar.
- **OccupancyRange** — the chosen scope (office or room) and the day/week/month span being viewed.
- **MyBookedDays** — the set of dates the viewer holds a reservation, used to highlight the calendar.

## Out of Scope (this feature / deferred)
- **OC-5** (view all my reservations + cancel upcoming) — already delivered by `007-attendance-web`.
- Reserve/cancel/change actions — those are `007`; occupancy is read-only.
- Administrator on-behalf anything (AT-6, deferred).
- Exporting or printing occupancy; per-seat detail (capacity model only).

## Review & Acceptance Checklist
- [ ] No implementation details (no component/service mechanics in the spec body)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] Token-free (BFF) posture is explicit
- [ ] Localization + accessibility requirements are present
- [ ] The today/tomorrow name policy is honoured (names never inferred client-side)
- [ ] Range requests stay within the backend's 31-day bound
- [ ] OC-5 (my reservations) is explicitly out of scope (done in 007)
- [ ] No open clarification markers remain
