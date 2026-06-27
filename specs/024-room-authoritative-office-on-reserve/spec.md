# Feature Specification: Room-authoritative office on reserve

**Feature Branch**: `024-room-authoritative-office-on-reserve`

**Created**: 2026-06-27

**Status**: Draft

**Bounded context**: attendance (Core)

## Summary

When an employee reserves a place, the reservation is recorded against a room **and** the office that
room belongs to. Today the office is taken from the **request body**: the reserve endpoint builds the
office identifier from `request.OfficeId`, the handler pairs it with the room via `RoomReference.From`,
and that factory does **no validation** — it trusts whatever office it is handed. The room directory is
only asked for the room's *capacity*, never for which office actually owns the room.

The consequence is a correctness and integrity defect: a client can submit a valid room paired with an
**office that does not own it**, and the reservation persists with a **mismatched office**. Because the
per-office occupancy rollup is fed by the office carried on the reservation fact (`ReservationPlaced`
carries the room's office), a single mismatched booking attributes a place to the wrong office and
corrupts the office totals that every user sees — without ever overbooking the room, so no existing
invariant catches it.

This feature makes the **room the single authority for its owning office**. The room directory returns
the room's authoritative office alongside its capacity; the reservation's office is derived from that
authoritative value; and the client-supplied office is **removed from the reserve request and command
entirely**. The capacity guarantee is unchanged. As a closely-related, clearly-scoped follow-on, it also
makes the reservation events **symmetric** on office — the cancellation event carries the authoritative
office too, so the occupancy projection never has to re-derive office-from-room on cancel. This is a
**backend change with a wire-contract impact** (the reserve request DTO loses `officeId`), so it also
requires the OpenAPI spec re-emit and Angular client regeneration (ADR-0036).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A reservation is always attributed to the room's true office (Priority: P1)

When an employee reserves a place in a room, the system must record the office that **actually owns that
room**, regardless of any office the caller named. A caller cannot cause a reservation to be booked
against the wrong office.

**Why this priority**: The office on a reservation feeds the per-office occupancy rollup that every
authenticated user reads. A mismatched office silently corrupts those totals — a place counted against
an office that has none of that room — and there is no overbooking, so nothing else rejects it. This is
the headline correctness/integrity fix.

**Independent Test**: Reserve a real room while supplying an office that does **not** own it, and assert
the persisted reservation (and the resulting occupancy attribution) carry the room's **authoritative**
office, not the supplied one — either by rejecting the mismatch or by ignoring the supplied office in
favour of the authoritative one. Reserve the same room with no office influence and assert the same
authoritative office is recorded.

**Acceptance Scenarios**:

1. **Given** a room that belongs to office A, **When** a reserve request is made for that room while
   naming office B (a wrong office), **Then** the reservation is recorded against office A (the room's
   authoritative office) — the caller cannot book it against office B.
2. **Given** a room that belongs to office A, **When** the reservation is placed, **Then** the per-office
   occupancy rollup attributes the place to office A, and office B's totals are unaffected.
3. **Given** a reserve request, **When** it is processed, **Then** the owning office is obtained from the
   room directory (the room's authoritative office), never from the request payload.
4. **Given** a request for a room that does not exist in the directory, **When** it is processed, **Then**
   the reservation is rejected as not found (the existing missing-room behaviour is preserved), and no
   office is invented.

---

### User Story 2 - The reserve contract no longer accepts an office (Priority: P1)

The office must not be an input to reserving. The reserve request and the reserve command must not carry
an office at all, so there is no untrusted office to mismatch and no dead field for clients to populate.

**Why this priority**: Leaving the office on the contract keeps the trust boundary defective (a field
that looks authoritative but is ignored or, worse, still trusted) and invites the same bug to return. The
contract change is part of the same fix and drives the client regeneration.

**Independent Test**: Inspect the reserve request/command shape and assert it has no office field; place a
reservation through the slimmed contract and assert it still records the room's authoritative office and
returns the office on the response.

**Acceptance Scenarios**:

1. **Given** the reserve request body, **When** the contract is inspected, **Then** it carries the room
   and date (and the optional on-behalf-of employee) but **no** office.
2. **Given** the reserve command, **When** it is inspected, **Then** it carries no office field; the
   handler derives the office solely from the room directory result.
3. **Given** a successful reservation, **When** the response is returned, **Then** the office it reports is
   the room's authoritative office (the response still tells the caller which office the booking landed in).

---

### User Story 3 - Capacity enforcement is unchanged (Priority: P1)

Deriving the office from the room must not weaken the no-overbooking guarantee. Reserving still fails when
the room is full, and the one-reservation-per-employee-per-day rule still holds.

**Why this priority**: Capacity and the per-employee-per-day rule are the core attendance invariants
(ADR-0026). The room directory now returns *both* capacity and office; a regression here would be a
direct loss of a core guarantee.

**Independent Test**: Fill a room to capacity and assert the next reserve is rejected as full; reserve
twice for the same employee on the same day and assert the second is rejected — both with the office now
sourced authoritatively.

**Acceptance Scenarios**:

1. **Given** a room at capacity, **When** a further reservation is attempted, **Then** it is rejected as
   room-full (unchanged), with the office still derived from the room.
2. **Given** an employee who already holds a reservation that day, **When** they reserve again, **Then** it
   is rejected as already-reserved (unchanged).

---

### User Story 4 - Cancellation carries the authoritative office too (Priority: P2)

Make the reservation events symmetric on office. The placement event carries the office; the cancellation
event must carry it as well, so the occupancy read side can attribute a cancellation to its office
**without re-deriving office-from-room**.

**Why this priority**: Secondary, but it belongs to the same office-authority story. The current asymmetry
(`ReservationPlaced` has the office, `ReservationCancelled` does not) forces the cancellation projection
to look the office up from the room — the same office-from-room re-derivation this feature is removing on
the write path. Fixing it now keeps the events a complete, self-describing record of the fact.

**Independent Test**: Place then cancel a reservation; assert the cancellation event carries the same
authoritative office as the placement, and that the occupancy rollup decrements the **correct** office
using only the event's data (no room→office lookup on cancel).

**Acceptance Scenarios**:

1. **Given** a live reservation in office A, **When** it is cancelled, **Then** the cancellation event
   carries office A (the same authoritative office recorded at placement).
2. **Given** a cancellation, **When** the occupancy rollup is updated, **Then** it decrements office A's
   total using the office on the event, without looking the office up from the room.

---

### Edge Cases

- **Wrong office supplied (the defect):** a request naming an office that does not own the room must never
  result in a reservation against that office; the authoritative office wins (US1.1). Once the office is
  removed from the contract (US2) there is no supplied office to honour at all.
- **Missing room:** a room absent from the directory is still rejected as not found; no office is fabricated
  (US1.4).
- **Office unknown for an existing room:** if the directory cannot resolve an owning office for a room it
  otherwise knows, reserving is rejected rather than recorded with a null/placeholder office. (Within a
  context whose directory is fed by `OfficeOpened`/`RoomAdded`, a known room implies a known office;
  this guards the inconsistent-feed case.)
- **Existing reservations:** this slice changes how *new* reservations source their office; it does not
  rewrite already-stored reservations. Historical events keep the office they were written with.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The room directory MUST return the room's **authoritative owning office** together with its
  capacity for a given room (a single lookup yielding both), so the office is sourced from the same
  authority as capacity.
- **FR-002**: Reserving a place MUST derive the reservation's office from the room directory's
  authoritative result, and MUST NOT take the office from the request body or any caller-supplied value.
- **FR-003**: A reserve request that names an office which does not own the room MUST NOT produce a
  reservation against that office; the room's authoritative office MUST be recorded instead (mismatch is
  rejected or ignored in favour of the authoritative office).
- **FR-004**: The reserve request DTO MUST NOT include an office field, and the reserve command MUST NOT
  include an office field; the office exists only as a derived, authoritative value inside the handler.
- **FR-005**: When the room is not found in the directory, reserving MUST fail as not found (unchanged
  behaviour) and MUST NOT record any office.
- **FR-006**: Reserving MUST continue to enforce room capacity (reject when full) and the
  one-reservation-per-employee-per-day rule, with the office now sourced authoritatively.
- **FR-007**: The per-office occupancy rollup MUST attribute a placed reservation to the room's
  authoritative office; a mismatched-office request MUST NOT shift any count to an unrelated office.
- **FR-008**: The reservation **cancellation** event MUST carry the same authoritative office recorded at
  placement, making the placement and cancellation events symmetric on office.
- **FR-009**: The occupancy projection MUST attribute a cancellation to its office using the office carried
  on the cancellation event, without re-deriving the office from the room.
- **FR-010**: The reserve **response** MUST report the room's authoritative office (so the caller still
  learns which office the booking landed in, now that the caller no longer supplies it).
- **FR-011**: The wire-contract change (reserve request losing its office field, and the response office now
  being authoritative) MUST be reflected in the emitted OpenAPI spec and the regenerated Angular client,
  with no drift (ADR-0036).

> Each FR is covered by an acceptance scenario above: FR-001/002/003 ↔ US1.1–1.3; FR-004 ↔ US2.1–2.2;
> FR-005 ↔ US1.4; FR-006 ↔ US3.1–3.2; FR-007 ↔ US1.2; FR-008/009 ↔ US4.1–4.2; FR-010 ↔ US2.3; FR-011 is
> verified by the drift gate over the regenerated client.

### Key Entities *(include if feature involves data)*

- **Room (directory entry)**: the attendance-side view of an organization room — its capacity **and** its
  authoritative owning office. Fed by `RoomAdded`/`OfficeOpened` from organization (ADR-0038); this slice
  relies on the office it already carries rather than introducing a new feed.
- **Reservation (fact)**: a placed reservation records employee, room, date, and the **room's authoritative
  office**. Placement and cancellation events both carry that office after this change.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For every reservation placed via the API, the recorded office equals the room's authoritative
  owning office in **100%** of cases, including requests that previously could supply a wrong office.
- **SC-002**: A request that attempts to pair a room with a non-owning office results in **zero**
  reservations attributed to the non-owning office; the per-office rollup remains exact.
- **SC-003**: The reserve request and command expose **no** office input field; the only office in play is
  the authoritative one derived from the room.
- **SC-004**: Capacity and per-employee-per-day rejections behave identically to before the change (no
  regression), verified by the existing reserve tests passing with the office sourced authoritatively.
- **SC-005**: Cancellation events carry the authoritative office, and the occupancy projection updates the
  correct office on cancel using only event data — **no** room→office lookup on the cancel path.
- **SC-006**: The generated Angular client and emitted OpenAPI spec are in sync (drift gate green) after
  the reserve contract change.

## Assumptions

- **Room implies office in the directory**: a room known to the attendance directory has a known owning
  office, because both arrive from organization's `RoomAdded`/`OfficeOpened` feeds (ADR-0038). The
  unknown-office-for-known-room case is guarded (edge cases) but is not the expected state.
- **Single authoritative source**: the room directory is the authority for room→office within attendance;
  no other component should supply a room's office on the write path.
- **No historical rewrite**: already-stored reservation events are not migrated; the change governs new
  reservations and cancellations.

## Out of Scope

- Backfilling or correcting any historically mis-attributed reservations written before this change.
- Changing how the room directory is populated (the `RoomAdded`/`OfficeOpened` feeds and the organization
  published language are unchanged, ADR-0031/0038).
- The aggregate granularity or the occupancy projection mechanism themselves (ADR-0026/0038) — only the
  office *source* on reserve and the office *carried* on cancel change.
- Any change to the cancel **request** contract (cancellation already identifies the reservation; only the
  emitted event gains the office).

## Impact

- **Wire contract (breaking on the request side)**: the reserve request DTO drops `officeId`; the reserve
  response's office becomes the authoritative office. Per ADR-0036 this requires the build-time OpenAPI
  spec re-emit and `ng-openapi-gen` client regeneration, gated by the CI drift check (FR-011, SC-006).
- **Application port**: the room directory's capacity lookup becomes a capacity-**and**-office lookup
  (FR-001); the reserve handler derives the office from it instead of from the command (FR-002, FR-004).
- **Domain events**: `ReservationCancelled` gains the authoritative office to match `ReservationPlaced`
  (FR-008); the occupancy projection's cancel path reads the office from the event (FR-009).

## Review & Acceptance Checklist

- [ ] No implementation details leak into the requirements (no class/method names in the FRs)
- [ ] Every functional requirement is testable and maps to an acceptance scenario
- [ ] The "authoritative office wins over a supplied office" guarantee (FR-002/FR-003) is unambiguous
- [ ] The office is removed from both request and command (FR-004), not merely ignored
- [ ] Capacity and per-employee-per-day invariants are explicitly preserved (FR-006)
- [ ] The cancel-event symmetry is scoped as a secondary scenario, not the headline (US4)
- [ ] The wire-contract change and client regeneration are called out (FR-011, Impact)
- [ ] No open clarification markers remain
