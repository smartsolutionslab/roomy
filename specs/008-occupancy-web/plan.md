# Implementation Plan: Occupancy Web (Occupancy views & calendar)

**Branch**: `feat/008-occupancy-web` | **Date**: 2026-06-10 | **Spec**: `specs/008-occupancy-web/spec.md`

## Summary

Add the occupancy SPA surface — a read-only **occupancy list** (pick an office or room and a day / week /
month range; see each day's occupied-vs-capacity per room and the office rollup; names only for
today/tomorrow) and an **occupancy calendar** (a month grid where each day shows its figure and the
viewer's own booked days are highlighted) — to the Angular app. It realizes `004-occupancy` stories
OC-1, OC-2, OC-3, OC-4, OC-6. **OC-5 (view my reservations + cancel) is already shipped by `007`** and is
out of scope.

The backend is fully in place on `main` (`004`): `GET /occupancy?officeId|roomId&from&to` returns each
day's office rollup + per-room figures, with occupant names only for today/tomorrow (server-enforced),
range bounded to 31 days; `GET /reservations/mine` gives the viewer's days for the highlight; `GET
/rooms` (from `007`) is the office/room catalogue. **No backend change and no gateway change** —
`/occupancy/{**}` and `/reservations/{**}` and `/rooms/{**}` routes all already exist.

This **extends** `007`'s frontend libs rather than adding new ones (D-OcW2): occupancy read methods +
view models go into `@roomy/attendance-data-access`, and the two pages go into
`@roomy/attendance-feature`. No new ADR — applies ADR-0036 (codegen), ADR-0035 (lib structure),
ADR-0040 (shared guards), ADR-0013/0016/0024/0030.

## Technical Context

**Language/Version**: TypeScript / Angular 22 — standalone, signal-based, zoneless, OnPush, `inject()`,
signal `input()/output()`, no `NgModule` (ADR-0016/0027).

**Primary Dependencies**: `@angular/router` (functional guards), `@angular/common/http`, `@jsverse/
transloco` (ADR-0024), Angular CDK for accessible behaviours (ADR-0024). Reuses the generated client and
`AttendanceGateway` from `007`. Tests: `@testing-library/angular` + `@testing-library/user-event` on
`vitest-analog` (ADR-0035).

**Backend surface (through the gateway, same-origin, ADR-0030) — all already implemented:**
- `GET /occupancy?officeId|roomId&from&to` → `[{ date, office:{ officeId,name,occupied,capacity,isFull },
  rooms:[{ roomId,name,occupied,capacity,isFull,occupants?:[{ employeeId,name }] }] }]`. `occupants` is
  present only for today/tomorrow; range bounded to 31 days; past allowed; `422 range_too_large` /
  `422 unknown_scope` / `404 unknown_office|unknown_room`.
- `GET /reservations/mine` → the viewer's reservations (for the calendar highlight).
- `GET /rooms` → the bookable office/room catalogue (the scope picker).

**Project Type**: extensions to `@roomy/attendance-data-access` (type:data-access) and
`@roomy/attendance-feature` (type:feature), both `context:attendance`, lazy-loaded by `apps/web`.

**Constraints**: no tokens (ADR-0013); no hardcoded strings (ADR-0024); WCAG 2.2 AA incl. a non-colour
cue for highlighted days and a screen-reader-correct grid/table (ADR-0024); zoneless + OnPush + signals;
generated, drift-gated client (ADR-0036); never infer names the response withholds (FR-003); keep every
request within the 31-day bound (FR-006).

## Constitution Check

| Principle | Verdict | Notes |
|---|---|---|
| I. Spec-Driven & Test-First | ✅ | `spec.md` has testable AC (1–10); data-access + component specs precede implementation (RED→GREEN). |
| II. Clean Architecture & DDD | ✅ (frontend) | `OccupancyDay`/`OccupancyRoom`/`Occupant` typed view models at the data-access boundary; reuses `007`'s branded ids. No backend/domain change. |
| III. Context Isolation | ✅ | Read-only; the SPA talks only to the gateway. Everything stays `context:attendance`; reuses `attendance-data-access`. |
| IV. No Framework in the Core | n/a | Frontend-only feature. |
| V. Decisions Recorded | ✅ | **No new ADR** — applies ADR-0036/0035/0040/0013/0016/0024/0030. The lib-placement call is recorded as D-OcW2 here. |
| VI. Green Before Done | ✅ | `pnpm nx affected -t lint test build` on the touched projects. |
| VII. Small, Single-Purpose Changes | ✅ | Phased; data-access extension, the list page, the calendar page, and the wiring are separate commits. |

**Gate: PASS.**

## Key decisions

### D-OcW1 — Reuse the `007` typed client; extend it with an occupancy range read
`AttendanceGateway` already has a single-day `occupancyForOffice` (returns `RoomAvailability` for the
reserve picker). Add a richer **`occupancy(scope, from, to)`** that returns the full `OccupancyDay[]`
(office rollup + per-room figures + today/tomorrow occupants), accepting an office **or** room scope, and
map the generated `OccupancyDayResponse` to branded view models. Keep the existing `occupancyForOffice`
untouched (the reserve flow depends on its narrow shape).

### D-OcW2 — Extend `@roomy/attendance-feature`, do not add an occupancy lib
Occupancy is the read side of the **attendance** context (`004` is a projection inside attendance, not a
fourth service). ADR-0035's `libs/<context>/<type>` is one feature lib per context, so the pages live in
`@roomy/attendance-feature` (`occupancy/` folder) beside `reserve/` and `my-reservations/`. Rejected: a
separate `occupancy-feature` (would duplicate the context tag and break the `<context>/<type>` layout).

### D-OcW3 — Day/week/month presets keep requests within the 31-day bound; the calendar is a month read
The list offers **day / week / month** presets that compute `from`/`to` (Europe/Berlin) within the
backend's 31-day cap (a month ≤ 31 days). The calendar renders one **month** grid from a single
`occupancy(scope, monthStart, monthEnd)` call, and overlays `GET /reservations/mine` to highlight the
viewer's days. Range maths live in a tested `occupancy-range.ts` helper beside `007`'s `bookable-day.ts`.

### D-OcW4 — Names come from the server, never inferred
Each room's `occupants` is rendered only when the response carries it (today/tomorrow); for any other day
the UI shows counts only and never derives names (FR-003). Past ranges are read-only — no action controls.

## Project Structure (this feature)

```text
libs/attendance/data-access/src/lib/
├─ occupancy.ts          # OccupancyDay/OccupancyRoom/Occupant view models + toOccupancyDays mapping (+spec)
├─ occupancy-range.ts    # day/week/month -> {from,to} (Europe/Berlin), month grid weeks (+spec)
└─ attendance-gateway.ts # + occupancy(scope, from, to): Observable<OccupancyDay[]>  (+spec extension)
   src/index.ts          # export the new view models + range helpers

libs/attendance/feature/src/lib/
├─ occupancy/occupancy-page.ts/.html/.css + occupancy-page.spec.ts        # FR-001..FR-003, FR-005, OC-1/2/4/6
├─ occupancy/occupancy-calendar.ts/.html/.css + occupancy-calendar.spec.ts # FR-004, OC-3
└─ attendance.routes.ts # + occupancy + calendar routes (authGuard)

apps/web/src/app/app.html          # + Occupancy nav entry (any signed-in user)
apps/web/public/i18n/{en,de}.json  # + occupancy.* namespace (FR-008), DE/EN parity
```

## Phasing (see tasks.md)

1. **Data-access** — `OccupancyDay` view models + `toOccupancyDays` mapping + `occupancy-range` helpers +
   `AttendanceGateway.occupancy(scope, from, to)`; vitest specs.
2. **Occupancy list** (OC-1/2/4/6, FR-001..FR-003/FR-005) — office/room + day/week/month picker, per-room
   + rollup figures, today/tomorrow names, past read-only; error/empty states.
3. **Occupancy calendar** (OC-3, FR-004) — month grid, occupancy per day, own-bookings highlight (with a
   non-colour cue), month navigation.
4. **Wiring & polish** — routes (authGuard), `Occupancy` nav entry, `occupancy.*` DE/EN i18n, WCAG pass.

## Notes
- Tests fail before implementation (verify RED) — `@testing-library/angular` + `vitest-analog`.
- No backend, gateway, or ADR change; reuses `007`'s catalogue, gateway, branded ids, and day helpers.
- The OpenAPI spec already covers `/occupancy`; the generated client is unchanged, so no new drift gate.
