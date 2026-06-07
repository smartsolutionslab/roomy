# Feature Specification: Office & Room Management

**Feature Branch:** `002-office-management`
**Status:** Draft
**Created:** 2026-06-05
**Updated:** 2026-06-05
**Covers backlog stories:** US2 (create office), US3 (edit office); plus room management (rooms carry capacity)

## Summary

Administrators manage the company's offices and the rooms within them. An office has a name and a location and contains one or more rooms. Each room has a name and a capacity (number of places). Capacity lives on the room, not the office — an office's capacity is simply the sum of its rooms. A room's capacity is set when the room is created; changing capacity, and the reservation eviction a reduction would cause, is deferred to after the MVP.

## User Scenarios & Testing

### Primary User Story
As an administrator, I want to create offices and the rooms inside them, so that employees can plan attendance against the rooms that actually have places.

### Acceptance Scenarios

1. **Create an office**
   - GIVEN an administrator
   - WHEN they create an office with a name and a location
   - THEN the office exists (initially without rooms)

2. **Add a room to an office**
   - GIVEN an existing office
   - WHEN an administrator adds a room with a name and a capacity (places)
   - THEN the room exists in that office and is available for attendance planning

3. **Rename an office**
   - WHEN an administrator changes an office's name
   - THEN the office's name is updated

4. **Change an office's location**
   - WHEN an administrator changes an office's location
   - THEN the office's location is updated

5. **Rename a room**
   - WHEN an administrator changes a room's name
   - THEN the room's name is updated

6. **Room capacity below 1 rejected**
   - WHEN an administrator adds a room with a capacity below 1
   - THEN the room is not created and the capacity is rejected

7. **Non-administrator cannot manage offices or rooms**
   - GIVEN an employee
   - WHEN they attempt to create or edit an office or a room
   - THEN the action is rejected as not authorized

### Edge Cases
- Two offices MUST NOT share the same name within the company.
- Two rooms MUST NOT share the same name within the same office.

## Requirements

### Functional Requirements
- **FR-001:** An administrator MUST be able to create an office with a name and a location.
- **FR-002:** An administrator MUST be able to change an office's name.
- **FR-003:** An administrator MUST be able to change an office's location.
- **FR-004:** An administrator MUST be able to add a room to an office with a name and a capacity (number of places).
- **FR-005:** An administrator MUST be able to change a room's name.
- **FR-006:** A room's capacity MUST be set when the room is added and MUST NOT be changeable in the MVP (changing capacity is deferred — see Out of Scope).
- **FR-007:** A room's capacity MUST be a positive whole number (at least 1).
- **FR-008:** An office's capacity is the sum of its rooms' capacities (derived; never set directly on the office).
- **FR-009:** A user without the Administrator role MUST NOT be able to create or change offices or rooms.
- **FR-010:** Office names MUST be unique within the company; room names MUST be unique within their office.

### Key Entities (conceptual)
- **Office** — has a name and a location; contains one or more rooms. Belongs to the seeded company.
- **Room** — has a name and a capacity (number of places per day, fixed at creation in the MVP). Belongs to exactly one office.
- **Capacity** — the number of places available in a room on a day.

## Resolved Decisions
- Capacity lives on the **room**; an office's capacity is the **sum of its rooms** (derived).
- Editable office attributes in the MVP: name and location. Editable room attribute: name. Room capacity is fixed at creation.
- Minimum room capacity: 1.
- Names: offices unique within the company, rooms unique within their office.
- Company: the single seeded company; all offices belong to it.

## Out of Scope (this feature / deferred to post-MVP)
- **Changing a room's capacity (increase or decrease).**
- **Eviction of reservations on a capacity reduction** (LIFO removal of the latest-created reservations), and the administrator confirmation step that would precede it.
- Capacity 0 / temporarily "closing" a room or office.
- Deleting offices or rooms (no backlog story).
- Attributes beyond office name/location and room name/places.
- The reservation/booking rules themselves — specified in `003-attendance`.

## Review & Acceptance Checklist
- [ ] No implementation details (no aggregate, persistence, or event mechanics)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] Capacity clearly sits on the room; office capacity is derived
- [ ] Post-MVP scope (capacity changes + eviction) is clearly separated
- [ ] No open clarification markers remain
