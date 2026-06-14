# Quickstart & Validation: Occupancy Views (004)

How to prove this slice works end-to-end. Details live in `data-model.md` and `contracts/`; this is
the run/validate guide. Tests are written **before** implementation (constitution I), grouped by user
story (US6 lists, US9 my-reservations, US7 calendar).

## Prerequisites

- 003 (attendance reserve/cancel/day-view) on `main`: the `AttendanceDay` aggregate, event store, and
  the `Rooms`/`Employees` read models with their `RoomAdded`/`EmployeeHired` consumers.
- Organization (002) emitting the **existing** `OfficeOpened` + `EmployeeHired` (DisplayName) — no
  producer change is needed; 004 consumes more of the published language (research R6).
- Local stack via Aspire: `dotnet run --project backend/apps/apphost` (Postgres, RabbitMQ, Keycloak, gateway,
  identity-api, organization-api, **attendance-api**, db-migrator).
- The 004 migration (`Reservations`, `Offices`, `Employees.DisplayName`) applied by the db-migrator
  before `attendance-api` starts (ADR-0033, `WaitForCompletion`).

## Layered validation (the test pyramid — `docs/testing-strategy.md`)

### 1. Projection unit tests (read model from events) — the core
Drive `ReservationProjection` directly (Shouldly), no HTTP:

- `ReservationPlaced` ⇒ a `Reservations` row exists for `(ReservationId, Room, Date, Employee)`.
- `ReservationCancelled` ⇒ the row is gone; a re-place re-adds it (idempotent, total mapping).
- Counts: N placed in one room/day ⇒ `COUNT(*) == N`; a cancel decrements (FR-001/008).

### 2. Persistence/integration tests (real Postgres via the sibling test host)
Per the Aspire-Postgres integration pattern (CI has Docker). This is the **correctness-critical**
layer (ADR-0038):

- **Inline atomicity (FR-010):** after `ReservePlace` succeeds, the `Reservations` row is committed in
  the **same** transaction as the event — read-back in a fresh scope shows it immediately; a forced
  failure of the append leaves **no** read-model row.
- **Reserve-after-conflict (research R4, scenario 12):** force a save-time concurrency conflict, then
  let the bounded retry succeed; assert the `Reservations` rows match the committed stream exactly
  (no leaked row from the losing attempt) — i.e. the change-tracker reset works.
- **Rebuild (research R5):** truncate `Reservations`, replay all company-day streams ⇒ the read model
  is re-derived identically.
- `OfficeOpenedConsumer` upserts the `Offices` row; the extended `EmployeeHiredConsumer` persists
  `DisplayName`.

### 3. Query unit tests (the application boundary)
Drive `ViewOccupancy` / `ViewMyReservations` over a seeded read model with an injected `TimeProvider`:

- **Room figure (scenario 1):** room capacity 8 + 3 rows ⇒ `3/8`.
- **Office rollup (scenario 2):** rooms Σcapacity 30 + 12 rows ⇒ `12/30`; rollup includes a 0-room.
- **Range (scenario 3):** each day in `[from, to]` returns its own figure; past dates allowed
  (scenario 8).
- **Names policy (scenario 4, FR-007):** today and tomorrow include `occupants` with display names;
  any other day omits them — pin "today" via the fake `TimeProvider` (Europe/Berlin).
- **Full (scenario 9):** occupied == capacity ⇒ `isFull: true`.
- **My reservations (scenario 6):** past + future rows all returned with office/room/day.

### 4. API/contract tests (WebApplicationFactory through the host)
Assert the `contracts/attendance-api.md` surface:

- `GET /occupancy?officeId&from&to` and `?roomId` shapes; `occupants` present only for today/tomorrow;
  bad/missing scope and an over-long range ⇒ 400; `unknown_office`/`unknown_room` ⇒ 404.
- `GET /reservations/mine` returns the caller's reservations; `unknown_employee` ⇒ 404.
- Any authenticated user may view any office/room (FR-005); only `GET` exists (read-only, FR-006).

### 5. Architecture tests (`backend/tests/architecture`)
The attendance projects are already referenced (003). Confirm the read side respects the dependency
rule: query handlers/ports in `application` reference no EF/Wolverine type; the projection and read
models live in `infrastructure`; `domain` is untouched.

### 6. Frontend tests (`vitest-analog` + `@testing-library/angular`)
- `data-access`: the generated client + SignalStore facades expose occupancy figures and
  my-reservations as signals.
- `ui`: occupancy-figure renders `3/8`; full-badge appears when `isFull`; calendar-cell shows the day's
  figure and an own-day highlight.
- `feature`: the list page renders per-day rows (US6); my-reservations lists past+future with cancel only
  on future (US9); the calendar highlights the viewer's days (US7, FR-003). Transloco DE+EN keys, no
  hardcoded strings; CDK a11y (keyboard + roles), WCAG 2.2 AA.

## Manual smoke (through the gateway)

1. Log in via the BFF; reserve a place for **today** and one for **next week** (003 endpoints).
2. `GET /occupancy?officeId=…&from=<today>&to=<today+6>` ⇒ today shows `occupants` with names; later
   days show counts only; the office rollup sums its rooms.
3. Fill a room to capacity ⇒ that room reads `isFull: true` before any booking attempt (scenario 9).
4. `GET /reservations/mine` ⇒ both reservations listed with office/room/day; cancel the future one,
   re-fetch occupancy ⇒ the count dropped immediately (FR-010).
5. Open the SPA: occupancy **list**, **calendar** (own days highlighted), and **my reservations** pages.

## Definition of done for the slice

- Every scenario (1–9) + edges has a test written **before** its code, now green.
- Full gate suite passes on affected projects:
  `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`,
  `pnpm nx affected -t lint test build`, and the **OpenAPI drift gate** (ADR-0036).
- ADR-0038 is referenced by the projection code; `CLAUDE.md` active-plan pointer updated; no analyzer
  or test suppressions.
