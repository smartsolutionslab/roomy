# Feature Specification: Attendance Web (Reserve, View, Cancel — self-service)

**Feature Branch:** `feat/007-attendance-web`
**Status:** Draft
**Created:** 2026-06-09
**Updated:** 2026-06-09
**Realizes (frontend of):** `003-attendance` stories AT-1 (reserve a place), AT-2 (book today
spontaneously), AT-3 (guaranteed place / no overbooking — surfaced), AT-4 (cancel my own), AT-5
(change = cancel + re-reserve)

## Summary

The Angular SPA's attendance surface for an **employee acting on their own behalf**: the screens that
let a signed-in employee **reserve a place** in a room of an office for a bookable day, **see their own
reservations** (past and upcoming), **cancel** an upcoming one, and **change** a reservation by
cancelling and re-reserving. The attendance backend already exposes the reservation surface behind the
gateway (`/reservations**`, `/occupancy**`, ADR-0013); this feature adds the **reserve flow**
(office → room → day → reserve), the **my-reservations page** (list + cancel + change), the **route
guard** that gates the section to any signed-in employee, and the **navigation entry** for it.

Because attendance exposes no catalogue of bookable offices/rooms yet, this feature also adds a small
**`GET /rooms`** read endpoint on `attendance-api` (the bookable office/room catalogue, sourced from
attendance's own `Offices`/`Rooms` read models — never a cross-context join, ADR-0014) so the picker is
served entirely from the attendance context. The typed data-access client is **generated from the
attendance OpenAPI spec** and drift-gated in CI (ADR-0036), mirroring identity/organization.

All backend calls are same-origin relative URLs through the gateway; no tokens ever reach the SPA
(ADR-0013/0030). The UI is **localized** (DE + EN, Transloco, no hardcoded strings — ADR-0024),
**accessible** (WCAG 2.2 AA baseline, CDK behaviours — ADR-0024), and built with **standalone,
signal-based, zoneless, OnPush** components (ADR-0016/0035).

## User Scenarios & Testing

### Primary User Story
As an employee, I want to reserve a place in a specific room of an office for a day from the web app,
so that I know I will have a desk when I come in — and I want to see and cancel my own reservations.

### Acceptance Scenarios

1. **Reserve a place**
   - GIVEN a signed-in employee on the reserve screen
   - WHEN they pick an office, then a room, then a bookable day, and confirm
   - THEN the reservation is created and they are shown a localized success confirmation

2. **Book today spontaneously** (AT-2)
   - GIVEN today is a working day and the chosen room has a remaining place for today
   - WHEN the employee reserves a place in that room for today
   - THEN the reservation is created

3. **Room shown as full / reservation rejected as full** (AT-3)
   - GIVEN the chosen room has no remaining place for the chosen day
   - WHEN the employee views the room step
   - THEN the room shows as full (no remaining places) and cannot be submitted
   - AND IF a reserve request is nonetheless rejected as full (lost the last place), a localized
     "room is full" message is shown and no reservation is created

4. **Already reserved that day**
   - GIVEN the employee already holds a reservation in any room for the chosen day
   - WHEN they try to reserve another place that day
   - THEN the action is rejected with a localized "only one reservation per day" message and no
     reservation is created

5. **Only bookable days can be chosen**
   - WHEN the employee chooses a day
   - THEN only working days (Mon–Fri, Europe/Berlin) within today through today + 14 calendar days are
     selectable; a past day, weekend, or day beyond the window cannot be chosen
   - AND IF the server still rejects a day as not bookable, a localized "only working days within the
     next two weeks" message is shown

6. **See my reservations** (AT-4 precondition)
   - GIVEN a signed-in employee with past and upcoming reservations
   - WHEN they open the my-reservations page
   - THEN every reservation is listed with its office, room, and day, ordered by day, distinguishing
     upcoming from past

7. **Cancel an upcoming reservation** (AT-4)
   - GIVEN the employee viewing an upcoming reservation
   - WHEN they cancel it
   - THEN the reservation is removed from the list and its place is freed, with a localized confirmation

8. **Past reservations cannot be cancelled** (AT-4)
   - GIVEN a reservation whose day is in the past
   - WHEN the employee views it
   - THEN no cancel action is offered for it
   - AND IF a cancel is nonetheless rejected as past-immutable, a localized "past reservations cannot be
     changed" message is shown

9. **Change a reservation = cancel + re-reserve** (AT-5)
   - GIVEN the employee holds a reservation and wants a different room, office, or day
   - WHEN they choose to change it
   - THEN they are taken to cancel the existing reservation and create a new one; there is no single
     combined edit step

10. **Concurrent loss of the last place**
    - GIVEN a room has exactly one remaining place
    - WHEN the employee's reserve loses the race
    - THEN a localized message is shown (room full, or a retryable "please try again") and no
      reservation is created

11. **Unauthenticated visitor is sent to sign in**
    - GIVEN no session
    - WHEN they navigate to an attendance route
    - THEN they are redirected to the BFF sign-in (`/bff/login`) with a return URL back to the route

12. **Any signed-in employee may use it**
    - GIVEN a signed-in employee (administrator or not)
    - WHEN they navigate to the attendance routes
    - THEN the reserve and my-reservations screens are shown (this section is NOT administrator-gated)

13. **Language switch localizes the attendance screens**
    - WHEN the user switches language
    - THEN all labels, headings, actions, day names, and validation/error messages render in the chosen
      language (DE or EN)

### Edge Cases
- The catalogue has no offices/rooms yet → show an empty-state on the reserve screen, not a blank form.
- The my-reservations list is empty → show an empty-state message with a link to reserve.
- A reserve/cancel request fails (network/5xx) → show a non-blocking error and leave the screen usable.
- The room is unknown to attendance (`404 unknown_room`, the catalogue is stale) → surface a localized
  "that room is no longer available" message and refresh the catalogue.
- A reserve returns `409 concurrency_retry_exhausted` → treat as a retryable, non-blocking error
  ("please try again"), distinct from `room_full`.
- A `401` mid-session on any attendance request → treat as signed out (offer sign-in), no error dump.

## Requirements

### Functional Requirements
- **FR-001:** A signed-in employee MUST be able to reserve a place by choosing an office, then a room,
  then a bookable day, and confirming (`POST /reservations`); on success a localized confirmation MUST
  be shown.
- **FR-002:** The reserve flow MUST source its office/room catalogue from the attendance context
  (`GET /rooms`) and MUST show each room's remaining places for the chosen day (`GET /occupancy`), so a
  full room is visibly unbookable before submitting (AT-3).
- **FR-003:** The day chooser MUST allow only bookable days — working days (Mon–Fri, Europe/Berlin)
  within today through today + 14 calendar days (inclusive); a past day, weekend, or out-of-window day
  MUST NOT be selectable (client-side, no request sent).
- **FR-004:** A reserve rejection MUST be surfaced as a localized, non-blocking message tied to its
  cause: `room_full` ("room is full"), `already_reserved_today` ("only one per day"), `not_bookable`
  ("only working days within two weeks"), `unknown_room` ("no longer available", refresh catalogue),
  `concurrency_retry_exhausted` ("please try again", retryable). No reservation is created on rejection.
- **FR-005:** A signed-in employee MUST be able to view their own reservations (`GET /reservations/mine`)
  — past and upcoming — each with office, room, and day, distinguishing upcoming from past.
- **FR-006:** An employee MUST be able to cancel an upcoming reservation (`DELETE /reservations/{id}?date=`);
  on success it MUST be removed from the list and a localized confirmation shown.
- **FR-007:** No cancel action MUST be offered for a reservation whose day is in the past; a server
  `past_immutable` rejection MUST be surfaced as a localized message and leave the list unchanged.
- **FR-008:** Changing the room, office, or day of a reservation MUST be performed as a cancel followed
  by a new reservation; the UI MUST NOT offer a single combined edit step (AT-5).
- **FR-009:** The attendance routes MUST require an authenticated session (any employee — NOT
  administrator-gated); an unauthenticated visitor MUST be redirected to `/bff/login` with a `returnUrl`.
- **FR-010:** All attendance-screen text MUST be localized via Transloco (DE + EN); no hardcoded
  user-facing strings, including day names and validation/error messages (ADR-0024).
- **FR-011:** The attendance screens MUST meet the WCAG 2.2 AA baseline — keyboard operable, correct
  roles/names, visible focus, labelled form controls, and an announced result for reserve/cancel
  (ADR-0024).
- **FR-012:** No access token or refresh token is ever read, stored, or handled by the SPA (ADR-0013);
  all backend calls are same-origin relative URLs through the gateway (ADR-0030), via a client generated
  from the attendance OpenAPI spec and drift-gated in CI (ADR-0036).

### Key Entities (view models)
- **BookableOffice** — id, name, and its bookable rooms. The first picker step.
- **BookableRoom** — id, name, capacity, and (for the chosen day) remaining places / full flag. The
  second picker step.
- **MyReservation** — reservation id, office name, room name, day, and whether the day is upcoming
  (cancellable) or past (read-only). A row on the my-reservations page.

## Out of Scope (this feature / deferred)
- **Administrator on-behalf** reserve/cancel (AT-6) — deferred; needs an employee-directory read surface
  that does not exist yet. The issue stays open for a later slice.
- **Occupancy views** — room/office occupancy lists, the calendar, who-is-booked-today/tomorrow, and
  past-occupancy history (OC-1..OC-6, `004-occupancy`) are a separate frontend slice (`008`).
- **The all-company day view** (`GET /reservations?date=`, viewing everyone's reservations for a day) —
  belongs with the occupancy slice; this feature is self-service only.
- Reserving multiple days in one action, or selecting a specific seat within a room (capacity model).
- Changing a room's capacity (`OR-5`, post-MVP).

## Review & Acceptance Checklist
- [ ] No implementation details (no component/service mechanics in the spec body)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] Token-free (BFF) posture is explicit
- [ ] Localization + accessibility requirements are present
- [ ] Generated-client / drift-gate posture (ADR-0036) is explicit
- [ ] The new `GET /rooms` catalogue endpoint (attendance-owned, no cross-context join) is justified
- [ ] AT-6 (on-behalf) and occupancy (008) are explicitly out of scope
- [ ] No open clarification markers remain
