# Feature Specification: Organization Web (Office & Room Admin UI)

**Feature Branch:** `feat/006-organization-web`
**Status:** Draft
**Created:** 2026-06-09
**Updated:** 2026-06-09
**Realizes (frontend of):** `002-office-management` stories OR-1 (create office), OR-2 (edit office),
OR-3 (add room), OR-4 (rename room)

## Summary

The Angular SPA's organization surface: the admin-only screens that let an administrator manage the
company's **offices** and the **rooms** within them. The organization backend already exposes the
office/room management API behind the gateway (`/offices**`, administrator-gated writes, ADR-0013);
this feature adds the **offices list/detail page**, the **create-office**, **edit-office**,
**add-room**, and **rename-room** affordances, the **route guard** that gates the section to
administrators, and the **admin navigation entry** for it.

All backend calls are same-origin relative URLs through the gateway; no tokens ever reach the SPA
(ADR-0013/0030). The typed data-access client is **generated from the organization OpenAPI spec**
and drift-gated in CI (ADR-0036), mirroring identity. The UI is **localized** (DE + EN, Transloco,
no hardcoded strings — ADR-0024), **accessible** (WCAG 2.2 AA baseline, CDK behaviours — ADR-0024),
and built with **standalone, signal-based, zoneless, OnPush** components (ADR-0016/0035).

## User Scenarios & Testing

### Primary User Story
As an administrator, I want to create offices and the rooms inside them from the web app, so that
employees can plan attendance against rooms that actually have places.

### Acceptance Scenarios

1. **See the offices**
   - GIVEN a signed-in administrator on the offices page
   - WHEN the page loads
   - THEN it lists every office with its name, location, derived capacity, and rooms (each room's
     name and capacity)

2. **Create an office**
   - GIVEN an administrator on the offices page
   - WHEN they create an office with a name and a location
   - THEN the new office appears in the list (initially without rooms)

3. **Office name must be unique**
   - GIVEN an office named "Berlin" already exists
   - WHEN an administrator tries to create another office named "Berlin"
   - THEN the action is rejected with a non-blocking, localized "name already taken" message and no
     office is added

4. **Rename an office / change its location**
   - GIVEN an administrator viewing an office
   - WHEN they change its name or location
   - THEN the office reflects the new value

5. **Add a room to an office**
   - GIVEN an administrator viewing an office
   - WHEN they add a room with a name and a capacity (at least 1)
   - THEN the room appears under that office and the office's derived capacity increases by it

6. **Room capacity below 1 is rejected in the UI**
   - GIVEN an administrator adding a room
   - WHEN they enter a capacity below 1 (or a blank name)
   - THEN the form is invalid, the request is not sent, and a localized validation message is shown

7. **Rename a room**
   - GIVEN an administrator viewing a room
   - WHEN they change its name
   - THEN the room reflects the new name

8. **Employee cannot reach the organization admin page**
   - GIVEN a signed-in employee
   - WHEN they navigate to the offices route
   - THEN the admin UI is not shown (they are redirected to the not-authorized view), and the
     navigation entry for it is not offered

9. **Unauthenticated visitor is sent to sign in**
   - GIVEN no session
   - WHEN they navigate to the offices route
   - THEN they are redirected to the BFF sign-in (`/bff/login`) with a return URL back to the route

10. **Language switch localizes the organization screens**
    - WHEN the user switches language
    - THEN all office/room labels, headings, actions, and validation messages render in the chosen
      language (DE or EN)

### Edge Cases
- The offices list is empty → show an empty-state message, not a blank table.
- A create/rename/add request fails (network/5xx) → show a non-blocking error and leave the list
  unchanged.
- A name-conflict response (409) on create-office, add-room, or rename → surface a localized
  "name already taken" message tied to the offending field, not a generic error.
- A not-found response (404) on a mutation (office/room deleted out from under the view) → surface a
  localized "no longer exists" message and refresh the list.
- The session/offices request returns 401 mid-session → treat as signed out (offer sign-in), no error
  dump.

## Requirements

### Functional Requirements
- **FR-001:** A signed-in administrator MUST be able to view the list of offices (`GET /offices`),
  each with name, location, derived capacity, and its rooms (name + capacity), sourced from the
  gateway — never from a token in the SPA.
- **FR-002:** An administrator MUST be able to create an office with a name and a location
  (`POST /offices`); on success the office MUST appear in the list.
- **FR-003:** An administrator MUST be able to change an office's name (`PATCH /offices/{id}/name`)
  and its location (`PATCH /offices/{id}/location`); the displayed office MUST reflect the change.
- **FR-004:** An administrator MUST be able to add a room to an office with a name and a capacity
  (`POST /offices/{id}/rooms`); on success the room MUST appear under the office and the office's
  derived capacity MUST update.
- **FR-005:** An administrator MUST be able to change a room's name
  (`PATCH /offices/{id}/rooms/{roomId}/name`); the displayed room MUST reflect the change.
- **FR-006:** The add-room form MUST reject a capacity below 1 and a blank name client-side (no
  request sent) with a localized validation message; room capacity is set at creation and is not
  editable (per `002` FR-006, deferred post-MVP).
- **FR-007:** A name-conflict (409) from create-office, add-room, or rename MUST be surfaced as a
  localized, field-level "name already taken" message; the list MUST be left unchanged.
- **FR-008:** The organization admin route MUST require the Administrator role; a non-administrator
  MUST be redirected to the not-authorized view and MUST NOT be offered its navigation entry. An
  unauthenticated visitor MUST be redirected to `/bff/login` with a `returnUrl`.
- **FR-009:** All organization-screen text MUST be localized via Transloco (DE + EN); no hardcoded
  user-facing strings (ADR-0024).
- **FR-010:** The organization screens MUST meet the WCAG 2.2 AA baseline — keyboard operable,
  correct roles/names, visible focus, labelled form controls, and an announced result for mutations
  (ADR-0024).
- **FR-011:** No access token or refresh token is ever read, stored, or handled by the SPA
  (ADR-0013); all backend calls are same-origin relative URLs through the gateway (ADR-0030), via a
  client generated from the organization OpenAPI spec and drift-gated in CI (ADR-0036).

### Key Entities (view models)
- **Office** — id, name, location, derived capacity, rooms. A card/row on the offices page.
- **Room** — id, name, capacity. Nested under an office.

## Out of Scope (this feature / deferred)
- Changing a room's capacity and the eviction it would cause (post-MVP, per `002`).
- Deleting offices or rooms (no backlog story).
- Attendance/reservation screens against rooms (`003`/`006`-attendance, a separate feature).
- Occupancy views (`004`, a separate feature).
- Revoking administrator or any identity management (that is `005-identity-web`).

## Review & Acceptance Checklist
- [ ] No implementation details (no component/service mechanics in the spec body)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] Token-free (BFF) posture is explicit
- [ ] Localization + accessibility requirements are present
- [ ] Generated-client / drift-gate posture (ADR-0036) is explicit
- [ ] No open clarification markers remain
