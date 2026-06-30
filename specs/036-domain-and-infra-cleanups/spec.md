# Feature Specification: Domain and infrastructure cleanups (a basket of small fixes)

**Feature Branch:** `refactor/036-domain-and-infra-cleanups`
**Status:** Implemented
**Created:** 2026-06-27

## Summary

A basket of small, independent, low-risk cleanups gathered into one slice. Each item below is a
self-contained correction with its own acceptance scenario and its own functional requirements; any
one item could be reverted without touching the others. Five of the six are behaviour-preserving
tidy-ups; one (item 3) surfaces a genuine published-language staleness bug whose full fix needs an
integration-event contract change, so this slice takes only the safe, in-context part of it (an
idempotency short-circuit) and records the event work as an explicit follow-up.

No public route, status code, response body, or OpenAPI schema changes anywhere in this slice, so no
Angular client regeneration is required.

The items, each confirmed against source:

1. **`User.IsEmployee` is a constant masquerading as logic.** `backend/libs/identity/domain/Users/User.cs:24`
   is `public bool IsEmployee => true;` — it can never be `false`. Remove it (and its dead callers).
2. **`AttendanceDay.Apply` silently ignores unknown events on replay.**
   `backend/libs/attendance/domain/AttendanceDays/AttendanceDay.cs:92-108` is a `switch` with no
   `default:` arm; an unrecognised event during `LoadFromHistory` replay is swallowed. Add a `default`
   arm that throws.
3. **`Office.Rename` / `Office.RelocateTo` mutate published fields with no change event and no
   idempotency guard.** `backend/libs/organization/domain/Offices/Office.cs:38,40`. Add a same-value
   short-circuit (parity with `User.GrantAdministrator`) and record the missing integration events as a
   scoped-out follow-up.
4. **Fetch-or-NotFound repository boilerplate is duplicated.** The
   `SingleOrDefaultAsync(...) → if null Error.NotFound → return` shape is copied across four repository
   files. Extract a shared `SingleOrNotFoundAsync` query extension.
5. **`TimeProvider.System` is `TryAddSingleton`-registered in four places.** Register it once in a
   shared registration and drop the duplicates.
6. **`OrganizationDbContext.ConfigureContext` does not call `base`.**
   `backend/libs/organization/infrastructure/Persistence/OrganizationDbContext.cs:16-21`, unlike the
   identity / attendance / event-store contexts. Add the base call.

All six reproduce as described; none were dropped.

## User Scenarios & Testing

### Primary story

As a maintainer, I want a handful of small correctness and tidiness defects fixed in one reviewable
slice — each independently testable and revertible — so the domain and infrastructure carry no
dead logic, no silent replay footguns, and no copy-pasted boilerplate, without changing any
externally observable behaviour.

### Acceptance Scenarios

1. **(Item 1) `IsEmployee` is gone, every user is still an employee by construction**
   - GIVEN the `User` aggregate
   - WHEN the type is inspected
   - THEN no `IsEmployee` member exists, and no production or test code references it; the always-true
     fact that a `User` is an employee is expressed by the type itself, not a constant property.

2. **(Item 2) An unknown event during replay throws instead of being ignored**
   - GIVEN an `AttendanceDay` rehydrated via `LoadFromHistory`
   - WHEN the stream contains an event type the aggregate does not recognise
   - THEN `Apply` throws (e.g. `InvalidOperationException`/`ArgumentOutOfRangeException`) naming the
     unhandled event type, rather than silently skipping it
   - AND replay of the two known events (`ReservationPlaced`, `ReservationCancelled`) still reconstructs
     state exactly as before.

3. **(Item 3) Renaming / relocating to the same value is a no-op; the missing events are recorded as a follow-up**
   - GIVEN an `Office`
   - WHEN `Rename` is called with the office's current name, or `RelocateTo` with its current location
   - THEN the field is unchanged and no work is done (parity with `User.GrantAdministrator`'s
     `if (Role.IsAdministrator) return;` guard)
   - AND a renaming / relocation to a *different* value still updates the field as today
   - AND a decision note in this spec records that `Office.Name`/`Location` are part of the published
     language (carried by `OfficeOpened`, consumed by attendance's `OfficeOpenedConsumer` into the
     `offices` read model) and therefore a full fix needs `OfficeRenamed` / `OfficeRelocated`
     integration events — explicitly **out of scope** here as a contract change.

4. **(Item 4) The fetch-or-NotFound shape lives in one place**
   - GIVEN the organization and identity repositories
   - WHEN a single-entity lookup misses
   - THEN it returns `Error.NotFound` with the **same code and message string as today**, produced by a
     single shared `SingleOrNotFoundAsync` extension rather than open-coded `null` checks in each method.

5. **(Item 5) `TimeProvider.System` is registered once**
   - GIVEN any host composition that resolves `TimeProvider` today (identity-api, organization-api,
     attendance-api)
   - WHEN the service provider is built
   - THEN `TimeProvider` resolves to the singleton `TimeProvider.System` exactly as before
   - AND the `TryAddSingleton(TimeProvider.System)` line appears in only one shared registration, not in
     the four context registration extensions.

6. **(Item 6) `OrganizationDbContext` calls its base configuration**
   - GIVEN `OrganizationDbContext.ConfigureContext`
   - WHEN the model is built
   - THEN it calls `base.ConfigureContext(modelBuilder)` (as identity / attendance / event-store
     contexts do) before applying its own entity configurations
   - AND the produced model and all existing organization persistence behaviour are unchanged.

### Edge cases
- (Item 2) A future second known event type added to the `switch` keeps the `default` arm as the only
  unhandled-event path — the arm guards *unrecognised* types, not every newly added one.
- (Item 4) `CompanyRepository.GetSeededAsync` uses `FirstOrDefaultAsync` **without a predicate**
  ("first seeded company"), which does not match the predicate-based `SingleOrNotFoundAsync` signature;
  it is handled per FR-004c rather than forced through the new extension.
- (Item 5) `db-migrator` and `dev-seeder` register only the persistence extensions; moving the single
  registration MUST keep their behaviour identical (they either continue to resolve `TimeProvider` or
  never needed it — no composition that resolves it today may stop resolving it).

## Requirements

### Functional

**Item 1 — remove the constant `IsEmployee`**
- **FR-001a:** The `IsEmployee` property MUST be removed from `User`
  (`backend/libs/identity/domain/Users/User.cs:24`).
- **FR-001b:** Its only references — the three assertions in
  `backend/tests/identity/Domain/Users/UserTests.cs:29,57,90` — MUST be removed; no other production
  code references it (confirmed). No replacement property is introduced.

**Item 2 — fail loudly on an unknown replayed event**
- **FR-002a:** `AttendanceDay.Apply` MUST gain a `default:` arm that throws an exception identifying the
  unhandled event type, so an unrecognised event in a replayed stream cannot be silently ignored.
- **FR-002b:** Replay of the existing `ReservationPlaced` and `ReservationCancelled` events MUST be
  unchanged. (The shared `EventSourcedAggregate.LoadFromHistory`/`Raise` both route through `Apply`, so
  the guard also protects newly raised events.)

**Item 3 — idempotent office mutation; events flagged as follow-up**
- **FR-003a:** `Office.Rename(OfficeName)` MUST short-circuit (return without mutating) when the supplied
  name equals the current `Name`; `Office.RelocateTo(Location)` MUST short-circuit when the supplied
  location equals the current `Location` — mirroring `User.GrantAdministrator`'s idempotency guard.
- **FR-003b:** A rename / relocation to a *different* value MUST still update the field exactly as today.
  No new domain or integration event is raised in this slice (none is raised today either, so this is
  behaviour-preserving).
- **FR-003c:** This spec MUST record the decision that `Office.Name` and `Office.Location` are published
  language (`OfficeOpened` carries both; attendance's `OfficeOpenedConsumer` projects `Name` into its
  `offices` read model) and that without change events those consumer copies go stale after a rename or
  relocation. Designing and publishing `OfficeRenamed` / `OfficeRelocated` (a contracts change touching
  the attendance consumer) is **out of scope** and left to a dedicated follow-up spec.

**Item 4 — extract the fetch-or-NotFound boilerplate**
- **FR-004a:** A single shared extension
  `Task<Result<T>> SingleOrNotFoundAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, Error notFound, CancellationToken cancellationToken)`
  MUST be added to the shared infrastructure-persistence library (EfCore area), wrapping
  `SingleOrDefaultAsync(predicate, …)` and returning `notFound` when the result is `null`.
- **FR-004b:** The five predicate-based single-entity fetches MUST be rewritten to use it, preserving
  each existing `Error.NotFound` **code and message verbatim**:
  - `OfficeRepository.GetByIdentifierAsync` (`office.not_found`)
  - `EmployeeRepository.GetByIdentifierAsync` (`employee.not_found`) and `GetByWorkEmailAsync`
    (`employee.not_found`)
  - `UserRepository.GetByIdentifierAsync` (`user.not_found`) and `GetByKeycloakSubjectAsync`
    (`user.not_found`)
- **FR-004c:** `CompanyRepository.GetSeededAsync` uses `FirstOrDefaultAsync` without a predicate and so
  does not fit the predicate signature. It MUST either (a) be left untouched, or (b) be covered by a
  sibling parameterless `FirstOrNotFoundAsync` overload — the implementer chooses, but MUST NOT force it
  through `SingleOrNotFoundAsync` (which would change first→single semantics). Its `company.not_seeded`
  error is unchanged either way.

**Item 5 — register `TimeProvider.System` once**
- **FR-005a:** The duplicated `services.TryAddSingleton(TimeProvider.System)` calls — at
  `AttendanceInfrastructureServiceCollectionExtensions.cs:33` (`AddAttendancePersistence`) and `:53`
  (`AddAttendanceUseCases`), `OrganizationInfrastructureServiceCollectionExtensions.cs:27`
  (`AddOrganizationPersistence`), and `IdentityInfrastructureServiceCollectionExtensions.cs:38`
  (`AddIdentityUseCases`) — MUST be reduced to a single registration in a shared registration extension
  (the shared `AddRoomyDbContext`/infrastructure-persistence registration is the natural candidate).
- **FR-005b:** Every host composition that resolves `TimeProvider` today MUST still resolve the
  singleton `TimeProvider.System`; no composition that resolves it today may stop resolving it. The
  change is behaviour-preserving (`TryAddSingleton` is already idempotent).

**Item 6 — call the base context configuration**
- **FR-006a:** `OrganizationDbContext.ConfigureContext` MUST call `base.ConfigureContext(modelBuilder)`
  before applying its `Company`/`Office`/`Employee` configurations, matching `IdentityDbContext`,
  `AttendanceDbContext`, and `EventStoreDbContext`.
- **FR-006b:** Because the current `RoomyDbContext.ConfigureContext` base is empty, the produced model
  and all organization persistence behaviour MUST be unchanged today; the call exists so future shared
  base configuration is not silently skipped.

### Non-functional
- **NFR-001:** Items 1, 2, 4, 5, and 6 are behaviour-preserving; item 3 changes only same-value calls
  (today a redundant write, after this a no-op) and introduces no new event. No public route, status
  code, response body, or OpenAPI schema changes; no Angular client regeneration.
- **NFR-002:** All existing quality gates stay green: `dotnet build -warnaserror`, `dotnet test`
  (unit + integration + architecture), `dotnet format --verify-no-changes`, and
  `pnpm nx affected -t lint`.
- **NFR-003:** No analyzer suppression, no `eslint-disable`, no `[Skip]`/`[Ignore]`, no deleted test to
  make a gate pass (golden rule 3 / DoD).
- **NFR-004:** The shared `SingleOrNotFoundAsync` extension lives in infrastructure-persistence and
  takes no domain dependency; the dependency rule and architecture tests remain satisfied.

## Test-first plan (Red → Green)

Each item gets its own failing test(s) before its fix; the items are independent so their tests can be
written and verified in any order.

- **Item 1:** Remove the three `IsEmployee` assertions and the member together; the compile failure on
  any lingering reference is the red bar, a clean build the green bar. (No behavioural test remains —
  the fact is now structural.)
- **Item 2:** Unit (`attendance/domain`): rehydrate an `AttendanceDay` via `LoadFromHistory` with an
  unrecognised event object and assert it throws naming the type (red before the `default` arm exists);
  a companion test asserts replay of `ReservationPlaced` + `ReservationCancelled` still reconstructs the
  same `Reservations`.
- **Item 3:** Unit (`organization/domain`, extend `OfficeTests`): `Rename`/`RelocateTo` with the current
  value leaves the field unchanged and does nothing; with a new value still updates (the existing
  `Rename_changes_the_name` / `RelocateTo_changes_the_location` tests stay green).
- **Item 4:** Unit/integration over the repositories (or directly over `SingleOrNotFoundAsync`): a miss
  yields the exact existing `Error.NotFound` code/message; a hit returns the entity. Existing repository
  integration tests are the regression contract.
- **Item 5:** Registration test (per host / per extension): after building the provider, `TimeProvider`
  resolves to `TimeProvider.System` as a singleton; assert the registration is present from each current
  composition (extends the existing `*InfrastructureRegistrationTests`).
- **Item 6:** Assert `OrganizationDbContext` builds its model unchanged (existing organization
  persistence integration tests stay green) with the `base.ConfigureContext` call in place.

## Out of scope
- `OfficeRenamed` / `OfficeRelocated` integration events and the attendance consumer / read-model
  updates that would keep the projected office name and location fresh (item 3 follow-up — a contracts
  change, its own spec).
- Any change to repository method signatures, error codes, or the `Result`/`Error` types.
- Reworking `CompanyRepository.GetSeededAsync` semantics beyond the optional sibling overload in
  FR-004c.
- Any change to roles, the `User`/`Office` aggregates' behaviour beyond the named items, or to
  `RoomyDbContext`'s (currently empty) base configuration content.

## Review & Acceptance Checklist
- [x] Each of the six items has at least one test written before its fix (or, for item 1, a structural
      removal verified by a clean build)
- [x] `IsEmployee` and its three test references are gone; no reference remains
- [x] `AttendanceDay.Apply` throws on an unknown replayed event; known-event replay unchanged
- [x] `Office.Rename`/`RelocateTo` short-circuit on the same value; different values still update; the
      missing-events follow-up is recorded, not implemented
- [x] `SingleOrNotFoundAsync` extracted; the five predicate fetches use it with identical error
      codes/messages; `GetSeededAsync` handled per FR-004c
- [x] `TimeProvider.System` registered in exactly one shared place; every current composition still
      resolves it
- [x] `OrganizationDbContext.ConfigureContext` calls `base`; model and behaviour unchanged
- [x] Wire contract unchanged; no OpenAPI re-emit, no client regen
- [x] All gates green; no suppressions, no skipped/deleted tests
