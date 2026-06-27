# Feature Specification: Linear occupancy projection, consistent capacity-0 `IsFull`, single office lookup

**Feature Branch:** `refactor/032-occupancy-projection-perf-and-isfull`
**Status:** Draft
**Created:** 2026-06-27
**Updated:** 2026-06-27
**Realizes:** ADR-0038 (occupancy read side: inline projection; counts computed per read)

## Summary

A mostly behaviour-preserving cleanup of the attendance occupancy read path, with one deliberate
edge-case fix. Three findings in the same flow:

- **(a) Performance.** `ViewOccupancyHandler.BuildDay` iterates every room and `BuildRoom` re-scans
  the *entire* occupant list with `occupants.Where(o => o.Room == room && o.Date == date)` inside the
  per-date loop. Cost is `days × rooms × occupants` for output that is a single linear grouping. The
  occupants must be grouped **once** by `(Room, Date)` and looked up in constant time, making the
  build linear in the occupant set. Output is identical, number for number.
- **(b) Behaviour bug.** Room `IsFull` is `booked.Count >= room.Capacity.Value` with no zero guard,
  while office `IsFull` is `capacityTotal > 0 && occupiedTotal >= capacityTotal`. A **capacity-0
  room** reports `IsFull = true`; a capacity-0 office reports `false`. The two units disagree on the
  same edge. They must be made consistent and the chosen semantics documented: a 0-capacity unit is
  **not bookable**, so it is **never `IsFull`**. This changes the room result for the capacity-0 edge
  only.
- **(c) Minor.** `OccupancyReadModel.ResolveScopeAsync` reads the same office row twice for the
  office scope — `Offices.AnyAsync(...)` to check existence, then `OfficeNameAsync` issues a second
  `SingleOrDefaultAsync` for the name. The office row must be fetched **once**. All occupancy joins
  stay within the attendance database (no cross-service join). Behaviour-preserving.

No query shape, port signature, route, status code, or response body changes; no OpenAPI re-emit and
no Angular client regeneration.

## User Scenarios & Testing

### Primary story

As a maintainer, I want the occupancy view built in linear time with the room/office "full" rule
agreeing on the capacity-0 edge and the office looked up once, so the read path is cheap, correct,
and the full-rule cannot drift between room and office again.

### Acceptance Scenarios

1. **Identical occupancy figures after the perf refactor**
   - GIVEN any company, scope, and date range with reservations across multiple rooms and days
   - WHEN occupancy is viewed before and after the grouping change
   - THEN every produced `OccupancyView` is identical — same rooms, same `Occupied`/`Capacity`
     counts, same office rollup, same `Occupants` lists on today/tomorrow and `null` elsewhere, in
     the same order.

2. **Linear grouping, not per-(room × date) re-scan**
   - GIVEN occupants are grouped once by `(Room, Date)`
   - WHEN a day is built
   - THEN each room reads its bookings from the pre-built lookup; the full occupant list is **not**
     re-filtered per room per date.

3. **Capacity-0 room is never full (behaviour change)**
   - GIVEN a room whose capacity is 0
   - WHEN its `RoomOccupancy` is built (with 0 occupants, which is the only possible count)
   - THEN `IsFull` is `false` — matching the office rule for a 0-total office.

4. **Capacity-0 and full-capacity office rollup unchanged**
   - GIVEN an office whose rooms sum to capacity 0, and separately an office at capacity
   - WHEN the rollup is built
   - THEN the 0-total office is `IsFull = false` (as today) and the at-capacity office is
     `IsFull = true` (as today) — the office rule is unchanged; only the room rule is aligned to it.

5. **A full room with positive capacity is still full**
   - GIVEN a room with capacity `N > 0` and `N` occupants
   - WHEN its `RoomOccupancy` is built
   - THEN `IsFull` is `true`, exactly as today (the fix touches only the capacity-0 case).

6. **Office row read once**
   - GIVEN an office-scoped occupancy request for a known office
   - WHEN the scope is resolved
   - THEN the office row (existence + name) is obtained in a single query; the separate
     existence-check-then-name double read is gone. An unknown office still returns
     `Error.NotFound("unknown_office", …)`.

### Edge cases
- A room with capacity 0 and (impossibly, but defensively) a non-zero booking count → still
  `IsFull = false` under the "0-capacity is not bookable" rule; the count is reported as-is.
- An office whose only rooms are capacity 0 → office `Occupied = 0`, `Capacity = 0`,
  `IsFull = false` (unchanged).
- A date with no reservations → every room `Occupied = 0`, `Occupants` `null` for non-name days;
  the missing `(Room, Date)` lookup key yields an empty booking set, not an error.

## Requirements

### Functional
- **FR-001:** `ViewOccupancyHandler` MUST group the occupant records once by `(Room, Date)` and build
  each `RoomOccupancy` from that grouping, so build cost is linear in the occupant set rather than
  `days × rooms × occupants`. The produced views MUST be byte-for-byte equivalent to today's output
  for every scenario except the capacity-0 room `IsFull` change in FR-002.
- **FR-002:** Room and office `IsFull` MUST use the **same** capacity-0 semantics: a unit with total
  capacity 0 is not bookable and MUST report `IsFull = false`. Room `IsFull` becomes
  `Capacity > 0 && Occupied >= Capacity`, matching the existing office rule. The chosen semantics
  MUST be documented at the rule (a short note that 0 capacity ⇒ not bookable ⇒ never full).
- **FR-003:** A positive-capacity room/office MUST keep its current `IsFull` result (`Occupied >=
  Capacity`); only the capacity-0 result for rooms changes.
- **FR-004:** `OccupancyReadModel` MUST fetch the office row once when resolving an office scope —
  existence and name from a single query — removing the `AnyAsync`-then-`SingleOrDefaultAsync` pair.
  Unknown office MUST still yield `Error.NotFound("unknown_office", …)`; unknown room MUST still
  yield `AttendanceReadModelErrors.UnknownRoom()`.
- **FR-005:** All occupancy reads MUST stay within the attendance database; no cross-service join or
  direct access to another context's store is introduced (ADR-0014/0038).
- **FR-006:** No query shape, port signature (`IOccupancyReadModel.GetAsync`), route, status code,
  response body, or OpenAPI schema MAY change; no client regeneration is required.

### Non-functional
- **NFR-001:** The handler stays free of infrastructure/framework types (ADR-0005); grouping is
  in-memory over the `OccupancyData` the port already returns.
- **NFR-002:** All existing quality gates stay green (`dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, architecture tests, `nx affected` lint).

## Test-first plan (Red → Green)
- Unit (`attendance/application`, `ViewOccupancyTests`): a multi-room, multi-day fixture asserting
  the produced views match the expected numbers (pins FR-001 equivalence); a **new** capacity-0 room
  case asserting `IsFull = false` (Red against today's `true`, pins FR-002); a positive-capacity
  full-room case still `true` (FR-003); office-rollup cases for 0-total (`false`) and at-capacity
  (`true`) (FR-004 office side unchanged). Shouldly assertions, NSubstitute for the read-model port
  (ADR-0052).
- Integration (`attendance-integration`, `OccupancyReadModelTests`/`OccupancyEndpointTests`): the
  existing tests stay green unchanged — they are the contract that the read figures and HTTP
  behaviour did not move; the office-scope path additionally confirms a known office still resolves
  and an unknown office still 404s after collapsing the double query.

## Out of scope
- The Option D per-(room, day) counter table (ADR-0038 deferred) — counts stay computed per read.
- Any move to asynchronous projection or read-your-writes changes.
- The today/tomorrow name policy, scope resolution rules, or capacity sourcing from organization
  events — untouched.
- Any change to `RoomOccupancy` / `OfficeOccupancy` / `OccupancyView` shapes or the wire contract.

## Review & Acceptance Checklist
- [ ] Every functional requirement has a test written before its implementation
- [ ] Occupancy figures are identical after the grouping refactor (same numbers, same order)
- [ ] Room and office `IsFull` agree on capacity-0; the semantics are documented at the rule
- [ ] The capacity-0 room change is pinned by a new test that was Red before the fix
- [ ] The office row is fetched once; unknown office/room errors unchanged
- [ ] No cross-service join; port signature and wire contract unchanged; no client regen
- [ ] All gates green; no suppressions
</content>
</invoke>
