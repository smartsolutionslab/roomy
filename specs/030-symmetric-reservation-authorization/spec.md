# Feature Specification: Enforce reservation authorization symmetrically in the handler

**Feature Branch:** `refactor/030-symmetric-reservation-authorization`
**Status:** Draft
**Created:** 2026-06-27
**Updated:** 2026-06-27
**Realizes:** ADR-0053, ADR-0058 (authorization rules live in the use-case handler, which
receives the decision as a command field — `Actor` + `ActorIsAdmin`)

## Summary

A behaviour-preserving backend de-duplication that removes an inconsistency in the attendance
context. The same authorization rule — *an actor may act for themselves, and only an administrator
may act on behalf of another employee* — is enforced today in two different layers with two
different error codes:

- **Reserve** enforces it in the **handler** (`ReservePlaceHandler`): `employee != actor &&
  !actorIsAdmin` → `Error.Forbidden("not_authorized", …)`.
- **Cancel** enforces it in the **aggregate** (`AttendanceDay.Cancel` / `MayCancel`): not owner and
  not admin → `Error.Forbidden("not_owner", …)`.

Per ADR-0053/0058 the actor-authorization decision belongs in the use-case handler, fed the decision
as command fields. `CancelReservation` already carries `Actor` and `ActorIsAdmin`, yet the decision
still sits inside the aggregate under a second code. This slice pulls the cancel ownership decision
up into `CancelReservationHandler`, so both commands enforce the **same rule in the same place** and
return the **same canonical code**. `AttendanceDay` keeps only its pure domain invariants.

This is behaviour-preserving for the HTTP status codes (403 either way) **except** a deliberate
error-code unification: the cancel path's `"not_owner"` becomes `"not_authorized"`. That is the one
observable change and is called out explicitly below (FR-005).

## User Scenarios & Testing

### Primary story
As a maintainer, I want both reservation commands to enforce the on-behalf authorization rule in the
same layer with the same error code, so the rule is tested where it lives and the two paths cannot
drift in behaviour or in the contract they expose.

### Acceptance Scenarios

1. **Non-admin cancelling another's reservation is forbidden, in the handler**
   - GIVEN a held reservation owned by employee A, and a `CancelReservation` whose `Actor` is a
     non-administrator B (`ActorIsAdmin == false`)
   - WHEN the command is handled
   - THEN it fails with `Error.Forbidden("not_authorized", …)` and **no `ReservationCancelled`
     event is raised and the stream is not saved** — the decision is made in the application layer,
     not by raising-then-rejecting in the aggregate.

2. **Self-cancel still succeeds**
   - GIVEN a held reservation owned by employee A and a `CancelReservation` whose `Actor` is A
     (`ActorIsAdmin == false`)
   - WHEN the command is handled
   - THEN the reservation is cancelled exactly as before (`ReservationCancelled` raised, stream
     saved, `Result.Success()`).

3. **Admin-cancel still succeeds**
   - GIVEN a held reservation owned by employee A and a `CancelReservation` whose `Actor` is a
     different administrator (`ActorIsAdmin == true`)
   - WHEN the command is handled
   - THEN the reservation is cancelled exactly as before.

4. **Pure domain invariants are unchanged and still win where they must**
   - GIVEN a `CancelReservation` for a reservation that does not exist, OR for a day in the past
   - WHEN the command is handled
   - THEN it fails with `Error.NotFound("reservation_not_found", …)` / `Error.Validation
     ("past_immutable", …)` respectively — unchanged. These remain `AttendanceDay` invariants.

5. **Reserve behaviour is unchanged**
   - GIVEN the existing reserve scenarios (self-service, admin on-behalf, non-admin on-behalf)
   - WHEN `ReservePlace` is handled
   - THEN every outcome and code is exactly as today, including `Error.Forbidden("not_authorized",
     …)` for a non-admin on-behalf reservation rejected before the repository is touched.

6. **Both commands share one rule and one code**
   - GIVEN the reserve and cancel handlers
   - THEN the on-behalf/ownership rejection from both returns the identical canonical code,
     `"not_authorized"`, and neither path emits `"not_owner"`.

7. **HTTP behaviour (regression)**
   - WHEN the reserve/cancel endpoints are exercised over the real stack as in the existing
     integration tests
   - THEN every status code matches today (non-admin on-behalf reserve → `403`; non-owner non-admin
     cancel → `403`; self/admin → `204`/`201`); the cancel forbidden body's `code` is now
     `"not_authorized"` instead of `"not_owner"`.

### Edge cases
- Cancel ownership depends on the reservation's owner, which is aggregate state and is unknown to the
  handler from the command alone. The handler therefore resolves the owner from the loaded
  `AttendanceDay` and makes the decision in application code (inside the `MutateAsync` decision
  delegate, which is handler-authored), *before* any `ReservationCancelled` event is raised. This is
  the only structural difference from reserve (which can reject before loading because it has both
  ids on the command); the layer (application) and the code (`"not_authorized"`) are identical.
- An administrator cancelling their own reservation still succeeds (admin OR owner — not XOR).

## Requirements

### Functional
- **FR-001:** `CancelReservationHandler` MUST enforce the actor-authorization rule
  (`ActorIsAdmin || reservationOwner == Actor`) in the application layer, returning
  `Error.Forbidden("not_authorized", …)` when it fails, **before** any `ReservationCancelled` event
  is raised or the stream is persisted.
- **FR-002:** `AttendanceDay.Cancel` MUST no longer accept `actor` / `actorIsAdmin` and MUST no
  longer own `MayCancel` (or any actor-authorization decision). It MUST keep its pure domain
  invariants — reservation-exists (`reservation_not_found`) and not-in-the-past (`past_immutable`) —
  and raise `ReservationCancelled` only once authorization has already been granted by the handler.
- **FR-003:** The handler MUST obtain the reservation's owner from the loaded aggregate (a read of
  existing state), not from a cross-context lookup or a new command field; `CancelReservation`'s
  shape (`Company`, `Reservation`, `Date`, `Actor`, `ActorIsAdmin`) is unchanged.
- **FR-004:** `ReservePlace` / `ReservePlaceHandler` MUST be left behaviourally unchanged; the
  reserve on-behalf rejection MUST continue to return `Error.Forbidden("not_authorized", …)` before
  the repository is touched.
- **FR-005:** `"not_authorized"` is the canonical code for both paths. The cancel path's previous
  `"not_owner"` MUST be replaced by `"not_authorized"` everywhere it is asserted or documented:
  `backend/tests/attendance-integration/ReservationEndpointTests.cs`,
  `backend/tests/attendance/Domain/AttendanceDayCancelTests.cs`, and the attendance contract/spec
  docs that example the cancel 403 (`specs/003-attendance/*`, `specs/007-attendance-web/plan.md`).
  The HTTP **status** (403) is unchanged.
- **FR-006:** No route, HTTP status code, request/response shape, or OpenAPI **schema** MAY change.
  The only wire-observable change is the error-code string in the cancel-forbidden body. No Angular
  client special-cases the `"not_owner"` literal, so no client regeneration is required; any OpenAPI
  example or error catalogue that *quotes* the cancel code MUST be updated to `"not_authorized"`.

### Non-functional
- **NFR-001:** Handlers MUST NOT reference `ClaimsPrincipal` or any ASP.NET type (ADR-0005); the
  authorization decision is taken from the `Actor` / `ActorIsAdmin` primitives already on the
  command.
- **NFR-002:** The dependency rule holds — `domain` owns no authorization-policy decision after this
  slice; `application` owns it. The architecture tests stay green.
- **NFR-003:** All existing quality gates stay green (`dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, the architecture tests, and `pnpm nx affected -t lint`).

## Test-first plan (Red → Green)
- Unit (`attendance/application`, NSubstitute doubles per ADR-0052): `CancelReservationHandler`
  rejects a non-admin cancelling another's reservation with `"not_authorized"` and raises/saves
  nothing; self-cancel and admin-cancel still succeed. Write these against the *new* contract and
  watch them fail (today the rejection comes from the aggregate with `"not_owner"`).
- Unit (`attendance/domain`): `AttendanceDayCancelTests` updated — `Cancel` no longer takes
  actor/admin and only enforces `reservation_not_found` / `past_immutable`; the former
  `"not_owner"` assertion is removed (its responsibility moved to the handler test above).
- Integration (regression, real stack): the existing reserve/cancel endpoint tests stay green with
  one edit — the cancel-forbidden assertion expects `"not_authorized"` (FR-005).

## Out of scope
- Any change to *who* counts as an administrator, to roles, Keycloak, or the BFF session.
- Any change to the reserve rule itself or to `ReservePlace`'s shape (only confirmed unchanged).
- Generalising a shared "actor may act for self or as admin" helper across other contexts — a
  separate slice if it ever earns its keep (avoid the single-use abstraction).
- Past-immutability, room-capacity, one-per-day, and booking-window invariants — untouched.

## Review & Acceptance Checklist
- [ ] Every functional requirement has a test written before its implementation
- [ ] Non-admin cancel-on-behalf is rejected in the handler, before any event/save
- [ ] Self-cancel and admin-cancel still succeed; reserve behaviour unchanged
- [ ] `AttendanceDay.Cancel` no longer owns the actor-authorization decision (pure invariants only)
- [ ] Both commands enforce the same rule and emit the single canonical code `"not_authorized"`
- [ ] Every `"not_owner"` assertion/doc updated; HTTP status unchanged; no client regen
- [ ] All gates green; no suppressions
