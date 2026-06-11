# Feature Specification: A configured business clock for the company-local "today"

**Feature Branch:** `refactor/015-business-clock`
**Status:** Draft
**Created:** 2026-06-11
**Updated:** 2026-06-11
**Realizes:** ADR-0054 (an `IBusinessClock` port; the booking timezone becomes configuration)

## Summary

Attendance's booking window is anchored to the company's *local* business day, but the
application/edge layer derives "today" with the same hardcoded expression in four places —
`DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), berlinZone).DateTime)` —
each holding its own `static readonly` `Europe/Berlin` literal. This slice introduces a single
`IBusinessClock` port (implemented in attendance infrastructure) that computes the company-local
`Today` from `TimeProvider` and a **configured** timezone (default `Europe/Berlin`), and routes the
four call sites through it. No wire contract changes.

## User Scenarios & Testing

### Primary story
As a maintainer, I want "today" computed once from a configured timezone, so that the booking
window cannot drift between call sites and never silently falls back to the host machine's zone.

### Acceptance Scenarios

1. **Company-local today**
   - GIVEN a fixed UTC instant and a configured zone
   - WHEN `IBusinessClock.Today` is read
   - THEN it returns the `BookingDate` of that instant **in the configured zone**.

2. **Midnight crossing (the conversion is proven, not assumed)**
   - GIVEN a fixed UTC instant late in the UTC evening that is already past midnight in the
     configured zone (e.g. `22:30Z` summer → `00:30` next day in Berlin)
   - WHEN `Today` is read
   - THEN it returns the **next** calendar day — demonstrating the zone conversion, not a raw UTC
     date.

3. **Timezone is configuration with a default**
   - GIVEN no `Attendance:TimeZone` configured → the clock uses `Europe/Berlin`
   - GIVEN `Attendance:TimeZone` set to another IANA zone → the clock uses that zone
   - THEN `Today` reflects the configured zone in both cases.

4. **The four call sites stop deriving today**
   - GIVEN `ReservePlaceHandler`, `CancelReservationHandler`, `ViewOccupancyHandler`, and
     `OccupancyEndpoints`
   - THEN none holds a `berlinZone` field or the `ConvertTime(...).DateTime` expression; each reads
     `Today` (and, where it needs the instant, `Now`) from `IBusinessClock`.

5. **Booking behaviour is unchanged (regression)**
   - WHEN the attendance integration tests run on the real stack with their `FixedTimeProvider`
   - THEN bookability, occupancy, and reservation results match today's behaviour for the same
     instant and zone.

### Edge cases
- A misconfigured / unknown `Attendance:TimeZone` value → startup fails fast with a clear error
  (an invalid zone is a deployment fault, not a silent fallback).
- DST boundary days resolve via `TimeZoneInfo.ConvertTime`, which already accounts for the offset
  in effect at the instant — covered by the midnight-crossing test in both summer and winter.

## Requirements

### Functional
- **FR-001:** `attendance/application` MUST define `IBusinessClock` with `BookingDate Today` (the
  company-local business day) and `DateTimeOffset Now` (the UTC instant for event timestamps).
- **FR-002:** `attendance/infrastructure` MUST implement it over `TimeProvider` and a configured
  `TimeZoneInfo`, performing the UTC→local→`BookingDate` conversion in exactly one place.
- **FR-003:** The booking timezone MUST be configuration (`Attendance:TimeZone`, default
  `Europe/Berlin`), resolved at the composition root; `TimeProvider.System` stays the registered
  time source.
- **FR-004:** `ReservePlaceHandler`, `CancelReservationHandler`, `ViewOccupancyHandler`, and
  `OccupancyEndpoints` MUST obtain `today` (and the instant where needed) from `IBusinessClock`; the
  four `berlinZone` fields and the four duplicated expressions MUST be removed.
- **FR-005:** No route, status code, response body, or OpenAPI schema MAY change.

### Non-functional
- **NFR-001:** `IBusinessClock` returns a domain `BookingDate`; the port lives in `application`, the
  conversion in `infrastructure` — `domain` keeps receiving `today` as a parameter (ADR-0005, the
  dependency rule).
- **NFR-002:** Deterministic under test via `FixedTimeProvider` + an explicit zone; no reliance on
  the host machine's local timezone anywhere.
- **NFR-003:** All existing quality gates stay green.

## Test-first plan (Red → Green)
- Unit (`attendance/infrastructure`): `BusinessClock` with `FixedTimeProvider` — same-day instant,
  the midnight-crossing instant (summer **and** winter for DST), and a non-default zone.
- Unit: an invalid configured zone fails fast at resolution.
- Integration (regression, real stack): the existing reservation/occupancy tests stay green for the
  same fixed instant and zone.

## Out of scope
- Per-company / multi-tenant timezone as `Company` master-data (deferred in ADR-0054).
- Any change to the booking window length, working-day rule, or `AttendanceDay` identity.
- Identity/organization time usage (those use `TimeProvider` only for event timestamps; unaffected).

## Review & Acceptance Checklist
- [ ] Every functional requirement has a test written before its implementation
- [ ] No `berlinZone` literal or `ConvertTime(...).DateTime` expression remains outside `BusinessClock`
- [ ] Timezone is configuration with a default; invalid zone fails fast
- [ ] Midnight-crossing test proves the conversion (summer + winter)
- [ ] Wire contract unchanged; no OpenAPI re-emit
- [ ] All gates green; no suppressions
