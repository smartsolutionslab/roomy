# Quickstart & Validation: Attendance Planning (003)

How to prove this slice works end-to-end. Details live in `data-model.md` and `contracts/`;
this is the run/validate guide. Tests are written **before** implementation (constitution I).

## Prerequisites

- 002 (organization Office/Room) merged, **emitting `OfficeOpened` + `RoomAdded`** (PR #113 +
  the publish addition) — see the dependency note in `plan.md`.
- Local stack via Aspire: `dotnet run --project backend/apps/apphost` (Postgres, RabbitMQ, Keycloak,
  gateway, identity-api, **attendance-api**, db-migrator).
- The attendance schema (events table + `Rooms`/`Employees` read models) applied by the
  db-migrator before `attendance-api` starts (ADR-0033, `WaitForCompletion`).

## Layered validation (the test pyramid — `docs/testing-strategy.md`)

### 1. Domain unit tests (no infrastructure) — the invariants
Drive the `AttendanceDay` aggregate directly with explicit `capacity` and `today` (Shouldly):

- Reserve into a room with spare capacity ⇒ `ReservationPlaced` raised (scenario 1–2).
- 8th reservation into capacity-8 room ⇒ `room_full` (scenario 3).
- Second reservation same day (any room) ⇒ `already_reserved_today` (scenario 4).
- Past / Saturday / `today+15` ⇒ `not_bookable` (scenarios 5–7).
- Cancel own future reservation ⇒ `ReservationCancelled`; freed place re-bookable (8–9).
- Cancel another's reservation as a non-admin ⇒ `not_authorized` (11); as admin ⇒ ok (10).
- Cancel a past-day reservation ⇒ `past_immutable` (FR-009).
- `BookingWindow.IsBookable` truth table: Mon–Fri within `[today, today+14]`.

### 2. Persistence/integration tests (real Postgres via the sibling test host)
Per the Aspire-Postgres integration pattern (CI has Docker):

- Append→read round-trips a stream; `AttendanceDay.Rehydrate` reconstructs reservations.
- **Last-place race (scenario 12):** two concurrent `Reserve` calls on a capacity-1 room ⇒
  exactly one `ReservationPlaced`; the other reloads and gets `room_full`; the
  `(stream_id, version)` unique constraint is never violated (FR-007).
- `RoomAddedConsumer` / `OfficeOpenedConsumer` / `EmployeeHiredConsumer` upsert their read
  models from a published event.

### 3. API/contract tests (WebApplicationFactory through the host)
Assert the `contracts/attendance-api.md` surface: status-code + `code` mapping for each
outcome, admin `onBehalfOf` authorization, owner-only cancel, and the `GET /reservations`
replay.

### 4. Architecture tests (`backend/tests/architecture`)
After adding the three attendance projects as `ProjectReference`s to
`Roomy.ArchitectureTests` (CLAUDE.md — otherwise the rules pass vacuously): the dependency rule
and "no framework in domain/application" hold for `Roomy.Attendance.*`.

## Manual smoke (through the gateway)

```
# as the seeded admin, after an office+room exist (capacity 2):
POST /reservations { officeId, roomId, date: <next weekday> }        -> 201
POST /reservations { officeId, roomId, date: <same>, onBehalfOf: B } -> 201   (admin, room now full)
POST /reservations { officeId, roomId, date: <same> } as employee C  -> 409 room_full
GET  /reservations?date=<same>                                       -> 2 rows
DELETE /reservations/{first}                                         -> 204
POST /reservations { officeId, roomId, date: <same> } as employee C  -> 201   (place freed)
POST /reservations { officeId, roomId, date: <last Saturday> }       -> 422 not_bookable
```

## Definition of done for the slice
- Every acceptance scenario (1–13) + edge cases has a test written **before** its code, now green.
- `pnpm nx affected -t lint test build`, `dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes` all green.
- ADR-0039 (event-sourced write model) authored **before** the write-model code.
- New context projects registered in `Roomy.ArchitectureTests`; CLAUDE.md context table +
  SPECKIT pointer updated.
