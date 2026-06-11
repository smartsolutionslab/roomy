# 0054. A configured business clock for the company-local "today"

- **Status:** Accepted
- **Date:** 2026-06-11
- **Deciders:** Heiko Weiß

## Context and problem statement

Attendance's booking window is anchored to the company's *local* business day: `BookingWindow`
asks whether a candidate date is a working day within 14 days of **today**, and `AttendanceDay`'s
identity is `CompanyId + Date`. The domain takes `today` as a parameter — the right boundary — but
the `application`/edge layer derives it the same hardcoded way in four places:

```csharp
private static readonly TimeZoneInfo berlinZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
…
var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), berlinZone).DateTime);
```

verbatim in `ReservePlaceHandler`, `CancelReservationHandler`, `ViewOccupancyHandler`, and
`OccupancyEndpoints`. The timezone is a **compile-time literal** in four `static readonly` fields,
not configuration. Two problems follow:

- **One expression, four chances to get it wrong.** Any divergence (forgetting the conversion,
  using a different zone) silently shifts which day is bookable.
- **A latent server-timezone trap.** The explicit `ConvertTime(…, berlinZone)` is in fact the
  *correct* guard: the obvious-looking `TimeProvider.System.GetLocalNow()` would resolve to the
  **host machine's** zone, so "today" would depend on where the service runs. The codebase avoids
  that today only by repeating the conversion by hand — fragile.

How should the company-local "today" be computed once, with the timezone configured rather than
compiled in, without leaking framework or infrastructure concerns into `domain`/`application`?

## Decision drivers

- **One source of "today".** A single, tested place that turns the UTC instant into the company's
  local business date; callers ask, they do not re-derive.
- **Timezone is configuration, not a literal.** It must be settable per environment with a sane
  default, never the host machine's zone.
- **Respect the layers.** The domain keeps receiving `today` as a parameter; the abstraction is an
  `application` port returning a domain `BookingDate`; the conversion lives in `infrastructure`.
- **Keep the time source.** `TimeProvider` stays the instant source, so `FixedTimeProvider`
  (test-support) still controls "now" deterministically.
- **No speculation.** A single configured zone matches today's reality (one company resolved from
  options). Per-company timezone-as-master-data is explicitly deferred until a multi-tenant spec
  needs it.

## Considered options

- **A — Leave the four copies.** Lowest churn; the literal and the duplication remain.
- **B — A static `BusinessDate.Today(TimeProvider, TimeZoneInfo)` helper.** Removes duplication but
  still passes the zone in at every call site and offers no DI seam beyond `TimeProvider`.
- **C — Set a custom `TimeProvider` whose `LocalTimeZone` is the configured zone and call
  `GetLocalNow()`.** Fewer types, but overloads `TimeProvider` (a generic time source) with a
  business-calendar concern and hides the company-local meaning behind a generic call.
- **D — An `IBusinessClock` application port, implemented in infrastructure with a configured zone
  (chosen).** One definition, one DI seam, the zone is configuration, and the type name states the
  intent.

## Decision

**Option D.**

1. **Port (`attendance/application`, `Ports/`):**
   ```csharp
   public interface IBusinessClock
   {
       BookingDate Today { get; }   // the company-local business day
       DateTimeOffset Now { get; }  // the UTC instant, for event timestamps
   }
   ```
   `Today` returns the domain `BookingDate` (`application → domain` is allowed). `Now` exposes the
   same UTC instant handlers already use for `occurredAt`, so a handler can depend on the clock
   alone.

2. **Implementation (`attendance/infrastructure`):**
   ```csharp
   public sealed class BusinessClock(TimeProvider time, TimeZoneInfo zone) : IBusinessClock
   {
       public DateTimeOffset Now => time.GetUtcNow();
       public BookingDate Today =>
           BookingDate.From(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), zone).DateTime));
   }
   ```
   The conversion exists **only here**.

3. **Configuration.** The zone is bound from config (key `Attendance:TimeZone`, default
   `Europe/Berlin`) and resolved to a `TimeZoneInfo` at the composition root, which registers
   `IBusinessClock`. `TimeProvider.System` stays the registered time source.

4. **Call sites.** `ReservePlaceHandler`, `CancelReservationHandler`, and `ViewOccupancyHandler`
   depend on `IBusinessClock` instead of holding a `berlinZone` field and re-deriving `today`;
   `OccupancyEndpoints` injects `IBusinessClock` and reads `Today`. The four `static readonly
   berlinZone` fields and the four duplicated expressions are deleted.

## Consequences

**Positive**
- One definition of "today"; the four copies and four hardcoded zone lookups are gone.
- The timezone is configuration with a default, and can never silently fall back to the host
  machine's zone.
- Deterministic in tests: `FixedTimeProvider` at a chosen UTC instant + the configured zone, with
  a red test that pins a known midnight-crossing (e.g. a late-evening UTC instant resolving to the
  next Berlin day) so the conversion is proven, not assumed.

**Negative / trade-offs**
- A new port + implementation + DI registration in the attendance context; the affected handlers'
  constructors and tests update from `TimeProvider` to `IBusinessClock` where they only needed
  "today" (handlers that still need the raw instant get it from `Now`).
- Single configured zone only — a company in another timezone is **not** modelled yet; that is a
  deliberate deferral (see drivers), not an oversight.
- No wire-contract change: routes, status codes, and bodies are unchanged; no OpenAPI re-emit.

**Follow-ups**
- csharp.md and CLAUDE.md record "company-local "today" comes from `IBusinessClock`; never derive a
  timezone in a handler or endpoint."
- If multi-tenant with mixed timezones becomes real, revisit modelling the zone as `Company`
  master-data fed to attendance by integration event (ADR-0014) — a new decision, not this one.
