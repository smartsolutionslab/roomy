# Feature Specification: Encapsulate the AttendanceDay write-retry in the repository

**Feature Branch:** `refactor/016-attendanceday-mutate`
**Status:** Draft
**Created:** 2026-06-11
**Updated:** 2026-06-11
**Realizes:** ADR-0055 (optimistic-concurrency retry moves into `IAttendanceDayRepository.MutateAsync`)

## Summary

A behaviour-preserving refactor. The optimistic-concurrency retry loop
(`load → decide → save → reload on conflict → exhausted`) is duplicated verbatim in
`ReservePlaceHandler` and `CancelReservationHandler`, and it puts `MaxAttempts`, the conflict
contract, and the `concurrency_retry_exhausted` error in the application layer. This slice adds
`MutateAsync` to `IAttendanceDayRepository`, moves the loop into an `OptimisticWrite` helper in
attendance infrastructure (holding `MaxAttempts` and the exhaustion error), and collapses both
handlers to a single declarative call. `Load`/`Save` stay on the interface for the read handler and
the repository's integration tests. No route, status code, response body, or error code changes.

## User Scenarios & Testing

### Primary story
As a maintainer, I want the reload-and-retry policy defined once in the repository, so that a
command handler expresses only *what* to apply and never owns concurrency or attempt counts.

### Acceptance Scenarios

1. **A write that succeeds first time**
   - GIVEN a decision that succeeds and a save with no conflict
   - WHEN `MutateAsync` runs
   - THEN it loads once, saves once, and returns the decision's value.

2. **A decision that fails is not saved or retried**
   - GIVEN a decision that returns a failure (e.g. `room_full`, `reservation_not_found`)
   - WHEN `MutateAsync` runs
   - THEN it returns that error immediately, without calling save, without retrying.

3. **A concurrency conflict reloads and re-decides**
   - GIVEN the first save reports `concurrency_conflict` and the second succeeds
   - WHEN `MutateAsync` runs
   - THEN it loads again, re-applies the decision to the freshly loaded day, saves, and succeeds.

4. **Exhausting the attempts returns a retryable conflict**
   - GIVEN every save reports `concurrency_conflict`
   - WHEN `MutateAsync` runs
   - THEN after `MaxAttempts` it returns `Error.Conflict("concurrency_retry_exhausted", …)`, having
     attempted the save `MaxAttempts` times.

5. **A real Postgres conflict auto-retries and succeeds (integration)**
   - GIVEN two writers mutate the same `AttendanceDay`, the first committing between this writer's
     load and save
   - WHEN this writer uses `MutateAsync`
   - THEN the stale save conflicts once, the day is reloaded, the decision re-applies on the new
     state, and the write commits — no error surfaces to the caller.

6. **Handlers are declarative (no retry in application)**
   - GIVEN `ReservePlaceHandler` and `CancelReservationHandler`
   - THEN neither declares `MaxAttempts`, a retry loop, or `concurrency_retry_exhausted`; each calls
     `MutateAsync` with its decision; `ReservePlace` still runs its on-behalf guard and capacity
     lookup first.

7. **HTTP behaviour is unchanged (regression)**
   - WHEN the reserve/cancel endpoints are exercised over the real stack
   - THEN every status code, body, and error code matches today's behaviour, including
     `concurrency_retry_exhausted` on exhaustion.

### Edge cases
- A no-op decision (no uncommitted events) still saves successfully (`SaveAsync` returns success for
  an empty change set) and returns the decision's value — unchanged from today.
- `MutateAsync` does **not** swallow non-conflict failures: any error other than
  `concurrency_conflict` from a save (today these throw rather than return) is not retried.

## Requirements

### Functional
- **FR-001:** `IAttendanceDayRepository` MUST expose
  `Task<Result<T>> MutateAsync<T>(CompanyIdentifier, BookingDate, Func<AttendanceDay, Result<T>>, CancellationToken)`
  and a non-generic `Task<Result> MutateAsync(…, Func<AttendanceDay, Result>, …)`, and MUST keep
  `LoadAsync`/`SaveAsync`.
- **FR-002:** `MutateAsync` MUST: load the day; apply `decide`; on a decision failure return the
  error without saving or retrying; otherwise save; on a `concurrency_conflict` reload and
  re-`decide`; after `MaxAttempts` saves all conflict, return
  `Error.Conflict("concurrency_retry_exhausted", …)`.
- **FR-003:** The loop, `MaxAttempts` (3), and the `concurrency_retry_exhausted` error MUST live in
  attendance infrastructure (an `OptimisticWrite` helper); `AttendanceDayRepository.MutateAsync` MUST
  be a thin adapter over its own `LoadAsync`/`SaveAsync`.
- **FR-004:** `ReservePlaceHandler` and `CancelReservationHandler` MUST call `MutateAsync` and MUST
  NOT contain a retry loop, `MaxAttempts`, or `concurrency_retry_exhausted`. `ReservePlace` keeps its
  on-behalf guard and capacity lookup ahead of the call.
- **FR-005:** No route, status code, response body, or error code MAY change; no OpenAPI re-emit, no
  Angular client regeneration.

### Non-functional
- **NFR-001:** `application` MUST NOT reference `MaxAttempts`, the conflict code, or any persistence
  concurrency concept (ADR-0005); the repository owns them.
- **NFR-002:** `OptimisticWrite`'s control flow MUST be unit-tested with fake delegates (no
  database). A real conflict-then-retry MUST be covered by one integration test against Postgres
  (no in-memory provider — ADR-0012).
- **NFR-003:** All existing quality gates stay green.

## Test-first plan (Red → Green)
- Unit (`backend/tests/attendance-infrastructure`, fake delegates): `OptimisticWrite` — success-saves-once;
  decide-failure-no-save; conflict-then-success reloads+re-decides; exhaustion returns
  `concurrency_retry_exhausted` after `MaxAttempts`; the non-generic overload mirrors these.
- Unit (`backend/tests/attendance`, substitute `IAttendanceDayRepository`): handlers now assert wiring — the
  handler hands a working decision to `MutateAsync` (verified by invoking the captured closure), and
  `ReservePlace` short-circuits on the on-behalf guard / `unknown_room` without calling `MutateAsync`.
  The old retry/exhaustion/loser-on-reload handler tests are removed (their coverage moves to
  `OptimisticWrite`).
- Integration (`backend/tests/attendance-integration`): keep the existing repository concurrency tests; add
  one `MutateAsync` two-writer conflict-then-retry-succeeds against real Postgres.

## Out of scope
- Backoff/jitter or configurable attempt counts (rejected/deferred in ADR-0055).
- Generalising `OptimisticWrite` beyond attendance (only this context has the pattern).
- Any change to the event store, the concurrency mechanism, or `AttendanceDay`'s domain rules.

## Review & Acceptance Checklist
- [ ] Every functional requirement has a test written before its implementation
- [ ] No retry loop / `MaxAttempts` / `concurrency_retry_exhausted` remains in any handler
- [ ] `OptimisticWrite` control flow unit-tested without a database
- [ ] One real-Postgres conflict-then-retry integration test passes
- [ ] Wire contract unchanged; no OpenAPI re-emit, no client regen
- [ ] All gates green; no suppressions
