# 0055. Optimistic-concurrency retry lives in the AttendanceDay repository, not the handlers

- **Status:** Accepted
- **Date:** 2026-06-11
- **Deciders:** Heiko Weiß

## Context and problem statement

`AttendanceDay` is an event-sourced aggregate persisted with optimistic concurrency: `SaveAsync`
appends to the stream at an expected version, and a version clash surfaces as
`Error.Conflict("concurrency_conflict")` (the event store throws `EventStoreConcurrencyException`,
which the repository catches, clears the change tracker, and maps). Because the aggregate spans the
**whole company's day** (ADR-0026), write contention is real, so a write must reload and retry on a
conflict.

Today that retry is hand-written **in the application handlers**, identically in two places —
`ReservePlaceHandler` and `CancelReservationHandler`:

```csharp
private const int MaxAttempts = 3;
…
for (var attempt = 1; attempt <= MaxAttempts; attempt++)
{
    var day = await attendanceDays.LoadAsync(company, bookingDate, cancellationToken);
    var decision = day.Reserve(/* … */);          // or day.Cancel(/* … */)
    if (decision.IsFailure) return decision.Error;
    var saved = await attendanceDays.SaveAsync(day, cancellationToken);
    if (saved.IsSuccess) return decision.Value;
}
return Error.Conflict("concurrency_retry_exhausted", "…");
```

Two problems: the loop is **duplicated verbatim**, and it puts persistence concerns —
`MaxAttempts`, the conflict-detection contract, the `concurrency_retry_exhausted` error — **in the
application layer**, which should express *what decision to apply*, not *how many times to reload
under contention*. Optimistic concurrency is a property of how `AttendanceDay` is stored.

## Decision drivers

- **Concurrency control is a persistence concern.** The handler should not know the aggregate is
  event-sourced, that writes use optimistic concurrency, or that we retry three times.
- **One definition.** The reload-decide-save-retry policy should exist once, not per use case.
- **Declarative handlers.** A command handler should read as "apply this decision to the day,"
  with retry transparent.
- **Deterministically testable.** Exhaustion and the decide-fails-no-save path must be unit-tested
  without a database (the repo rules are Postgres-only, no in-memory provider — ADR-0012); a real
  conflict-then-retry is proven once against Postgres.
- **No behaviour change.** Same outcomes, same error codes, same HTTP responses.

## Considered options

- **A — Keep the loop in each handler.** Lowest churn; the duplication and the layer leak remain.
- **B — A shared application-layer helper** (`OptimisticConcurrency.ExecuteAsync(load, decide,
  save, …)`) the handlers call. Removes duplication but `MaxAttempts` and the loop stay in
  `application`, and the handler still orchestrates load/save — the concern hasn't moved layer.
- **C — A `MutateAsync` on the repository (chosen).** The repository owns reload-decide-save-retry;
  handlers pass only the decision. The concern moves to where persistence lives; `Load`/`Save`
  remain for the read handler and the repository's own integration tests.
- **D — A resilience library (Polly) / backoff+jitter.** Rejected: this is in-process
  reload-on-conflict, not transient-fault handling; immediate retry is correct here, and a new
  dependency + ADR is unwarranted. Backoff is a separate decision if contention ever demands it.

## Decision

**Option C.**

1. **`IAttendanceDayRepository` (domain) gains two `MutateAsync` overloads**, keeping `Load`/`Save`:
   ```csharp
   Task<Result<TResult>> MutateAsync<TResult>(CompanyIdentifier company, BookingDate date,
       Func<AttendanceDay, Result<TResult>> decide, CancellationToken cancellationToken);
   Task<Result> MutateAsync(CompanyIdentifier company, BookingDate date,
       Func<AttendanceDay, Result> decide, CancellationToken cancellationToken);
   ```
   Contract: load the day, apply `decide`; if the decision fails, return its error **without
   saving or retrying**; otherwise save, and on a concurrency conflict **reload and re-decide**;
   after the attempt budget is exhausted, return `concurrency_retry_exhausted`. Retry is invisible
   to the caller.

2. **The loop lives once in attendance infrastructure**, in an `OptimisticWrite` helper that holds
   `MaxAttempts` (3) and the `concurrency_retry_exhausted` error and takes `load`/`decide`/`save`
   delegates. `AttendanceDayRepository.MutateAsync` is a thin adapter binding those delegates to its
   own `LoadAsync`/`SaveAsync`. The helper is pure (no DB), so its control flow — success, decide
   fails → no save, conflict → reload + re-decide, exhaustion — is unit-tested with fake delegates.

3. **The handlers collapse to one call.** `MaxAttempts`, the loop, and `concurrency_retry_exhausted`
   leave `application`:
   ```csharp
   // ReservePlaceHandler, after the on-behalf guard + capacity lookup:
   return await attendanceDays.MutateAsync(company, bookingDate,
       day => day.Reserve(employee, room, capacity.Value, today, occurredAt), cancellationToken);

   // CancelReservationHandler — the whole body after computing today/occurredAt:
   return await attendanceDays.MutateAsync(company, bookingDate,
       day => day.Cancel(reservation, actor, actorIsAdmin, today, occurredAt), cancellationToken);
   ```

## Consequences

**Positive**
- The retry policy has one home; the duplicated loop is gone.
- Handlers are declarative and free of persistence/concurrency detail (ADR-0005 spirit).
- Deterministic, fast unit tests for the retry control flow (no DB); one integration test proves a
  real Postgres conflict auto-retries and succeeds.
- No wire change: same status codes, same `concurrency_retry_exhausted` / domain errors, no OpenAPI
  re-emit.

**Negative / trade-offs**
- The retry/exhaustion tests move out of the handler unit tests (which mocked `Load`/`Save`) into
  `OptimisticWrite` helper tests; the handler tests shrink to wiring + guard + capacity, exercising
  the decision closure the handler hands to `MutateAsync`.
- `MutateAsync` takes a `Func<AttendanceDay, Result<T>>` — a delegate on the domain repository
  interface. Justified: it expresses "apply this decision atomically," with retry an implementation
  detail; the alternative (leaking the loop into every handler) is worse.
- Retry stays a fixed, immediate 3× (deliberately — see option D); revisit with backoff only if a
  hot day-aggregate proves to thrash.

**Follow-ups**
- csharp.md and CLAUDE.md record "mutate an event-sourced aggregate via the repository's
  `MutateAsync`; never hand-roll the reload-retry loop in a handler."
- If a second event-sourced context grows the same need, generalise `OptimisticWrite` then — not
  pre-emptively (only attendance has it today).
