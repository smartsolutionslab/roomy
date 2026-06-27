# Feature Specification: Retry only on a concurrency conflict in OptimisticWrite

**Feature Branch:** `refactor/033-optimistic-write-error-handling`
**Status:** Draft
**Created:** 2026-06-27
**Updated:** 2026-06-27
**Realizes:** ADR-0055 (the optimistic-concurrency retry loop is owned by the AttendanceDay repository),
ADR-0039 (event-sourced write model; bounded optimistic-retry on a version conflict)

## Summary

A behaviour-preserving robustness fix in attendance infrastructure. The `OptimisticWrite` helper
(`OptimisticWrite.cs:15-26`) drives the reload-decide-save loop for the event-sourced `AttendanceDay`
aggregate, but it decides whether to retry by inspecting only `saved.IsSuccess` — **any** failed save,
regardless of error code, triggers a silent reload-and-retry and, after the attempt budget is spent,
returns the generic `concurrency_retry_exhausted` conflict. This is correct *today only by accident*:
`AttendanceDayRepository.SaveAsync` returns exactly one failure code, `concurrency_conflict`
(`AttendanceDayRepository.cs:40-44`). The moment `SaveAsync` returns any other error, the caller would
be handed a **misleading conflict code** and put through three pointless reload-decide cycles for a
failure that has nothing to do with concurrency.

This slice tightens the loop so it retries **only** on the specific concurrency-conflict code and
propagates any other save error **immediately, unchanged**. It also aligns the helper with the rest of
the infrastructure: `await` becomes `ConfigureAwait(false)`, and cancellation is honoured between
attempts. The conflict path (success, decide-fails-no-save, conflict-then-retry, exhaustion) is
unchanged; only the latent, currently-unreachable non-conflict path is corrected. No route, status
code, response body, or OpenAPI schema changes — no client regeneration.

## User Scenarios & Testing

### Primary story
As a maintainer, I want `OptimisticWrite` to retry only on a genuine concurrency conflict and surface
every other save error verbatim, so that a future non-conflict failure from `SaveAsync` is reported
truthfully and instantly instead of being relabelled as a conflict after three wasted reloads.

### Acceptance Scenarios

1. **A conflict still retries and exhausts (unchanged)**
   - GIVEN a `save` delegate that always returns `Error.Conflict("concurrency_conflict", …)`
   - WHEN `OptimisticWrite.ExecuteAsync` runs
   - THEN it reloads and re-decides up to `MaxAttempts` times and finally returns
     `Error.Conflict("concurrency_retry_exhausted", …)`, having called `save` exactly `MaxAttempts`
     times — identical to today.

2. **A conflict that clears on retry still succeeds (unchanged)**
   - GIVEN a `save` delegate that returns `concurrency_conflict` once, then success
   - WHEN `ExecuteAsync` runs
   - THEN it loads twice, decides twice, saves twice, and returns the decision value.

3. **A non-conflict save error propagates immediately**
   - GIVEN a `save` delegate that returns a failure whose code is **not** `concurrency_conflict`
     (e.g. `Error.Unexpected`/any other code)
   - WHEN `ExecuteAsync` runs
   - THEN it returns *that exact error* (same code, message, and type) after a **single** save, with
     **no** reload and **no** re-decide, and it is **not** relabelled `concurrency_retry_exhausted`.

4. **A decision failure is still returned without saving or retrying (unchanged)**
   - GIVEN a `decide` delegate that fails (e.g. `room_full`)
   - WHEN `ExecuteAsync` runs
   - THEN it returns that error after one load, with zero saves and no retry.

5. **Cancellation is observed between attempts**
   - GIVEN a `CancellationToken` that is cancelled after the first conflicting save
   - WHEN `ExecuteAsync` is mid-loop
   - THEN it stops before the next reload and surfaces cancellation rather than continuing to the
     attempt budget.

6. **Existing concurrency integration tests stay green (regression)**
   - WHEN the real-Postgres concurrency tests in `attendance-integration`
     (`AttendanceDayRepositoryTests`) exercise a true last-place race
   - THEN a concurrent conflict still auto-retries, re-decides against fresh state, and reaches the
     same outcome and error codes as today.

### Edge cases
- A save failure whose code happens to be `concurrency_conflict` but a different `ErrorType` —
  retry keys on the **code** (`"concurrency_conflict"`), matching how `SaveAsync` constructs it
  (`Error.Conflict("concurrency_conflict", …)`); the code is the single source of the retry decision.
- The non-generic `ExecuteAsync` overload (which delegates to the generic one) inherits the same
  retry-only-on-conflict, propagate-otherwise behaviour with no separate logic.
- Cancellation requested before the first attempt is honoured before any load.

## Requirements

### Functional
- **FR-001:** `OptimisticWrite.ExecuteAsync` MUST retry (reload + re-decide) **only** when the save
  result is a failure whose error code is the concurrency-conflict code (`"concurrency_conflict"`),
  the code `AttendanceDayRepository.SaveAsync` returns on `EventStoreConcurrencyException`.
- **FR-002:** When a save fails with **any other** error code, `ExecuteAsync` MUST return that error
  unchanged (same `Code`, `Message`, `Type`) immediately, performing no further load, decide, or
  save, and MUST NOT substitute `concurrency_retry_exhausted`.
- **FR-003:** The conflict path MUST be unchanged: a persistent conflict retries up to `MaxAttempts`
  (3) and then returns `Error.Conflict("concurrency_retry_exhausted", …)`; a conflict that clears
  on a later attempt returns the decision value; a `decide` failure returns immediately without
  saving or retrying.
- **FR-004:** `ExecuteAsync` MUST `await … .ConfigureAwait(false)` on every awaited call (load and
  save), consistent with `AttendanceDayRepository` and `EfCoreEventStore`.
- **FR-005:** `ExecuteAsync` MUST accept a `CancellationToken` and observe cancellation between
  attempts (before reloading for a retry), so a cancelled write does not run out the attempt budget.
  `AttendanceDayRepository.MutateAsync` MUST thread its existing `cancellationToken` through to the
  helper.
- **FR-006:** The retry policy MUST remain owned by attendance infrastructure (ADR-0055): no
  retry/conflict-classification logic leaks into `application` or `domain`; `MaxAttempts` and the
  `concurrency_retry_exhausted` error stay in `OptimisticWrite`.
- **FR-007:** No route, status code, response body, or OpenAPI schema MAY change; no Angular client
  regeneration is required.

### Non-functional
- **NFR-001:** The helper MUST stay pure (delegate-driven, no DB), so the retry control flow remains
  unit-testable with fake delegates (ADR-0055).
- **NFR-002:** All existing quality gates stay green (`dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, the architecture tests, and `nx affected` lint); no
  suppressions, no skipped tests.

## Test-first plan (Red → Green)
- Unit (`attendance-infrastructure`, `OptimisticWriteTests`):
  - **New (Red):** a non-conflict save error propagates immediately — single save, no reload/redecide,
    same code/message/type, not relabelled `concurrency_retry_exhausted`.
  - **New (Red):** cancellation requested mid-loop stops before the next reload.
  - **Unchanged (stay green):** success-once; decide-fails-no-save; conflict-then-success;
    exhaustion → `concurrency_retry_exhausted`; the non-generic overload mirrors both. These guard the
    behaviour-preserving claim for the conflict path.
- Integration (regression, real Postgres): `AttendanceDayRepositoryTests` concurrency cases stay green
  unchanged — the contract that the conflict behaviour did not move.

## Out of scope
- Adding new `SaveAsync` failure codes or changing what `EfCoreEventStore`/`AttendanceDayRepository`
  emit — this slice only makes `OptimisticWrite` robust to them; the non-conflict path stays latent.
- Backoff, jitter, or a configurable attempt budget (rejected in ADR-0055 option D); retry stays a
  fixed, immediate 3×.
- Generalising `OptimisticWrite` for a second event-sourced context (ADR-0055 follow-up).
- Any change to the domain, application handlers, gateway, or wire contract.

## Review & Acceptance Checklist
- [ ] Every functional requirement has a test written before its implementation
- [ ] A non-conflict save error propagates immediately, unchanged, and is never relabelled a conflict
- [ ] Retry fires only on the `concurrency_conflict` code; exhaustion still yields
      `concurrency_retry_exhausted`
- [ ] `ConfigureAwait(false)` on every awaited call; cancellation observed between attempts
- [ ] Retry policy stays in attendance infrastructure (ADR-0055); no leak to application/domain
- [ ] Existing unit and Postgres concurrency integration tests stay green
- [ ] Wire contract unchanged; no OpenAPI re-emit, no client regen
- [ ] All gates green; no suppressions
