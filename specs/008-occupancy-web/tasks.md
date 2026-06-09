---
description: "Task list for Occupancy Web — occupancy views & calendar (008)"
---

# Tasks: Occupancy Web (Occupancy views & calendar)

**Input**: Design documents in `specs/008-occupancy-web/` (plan.md, spec.md)

**Tests**: REQUIRED. Each acceptance criterion becomes a failing `@testing-library/angular` /
`vitest-analog` spec (ADR-0035) before the code exists (RED→GREEN).

**Organization**: Grouped by phase / user story; each story is independently testable. Extends `007`'s
`@roomy/attendance-data-access` and `@roomy/attendance-feature` — no new libs, no backend change.

## Story label map

| Label | Story | Scenarios | Priority |
|---|---|---|---|
| US1 | Occupancy list (room + office rollup, day/week/month, names today/tomorrow, past read-only) | 1, 2, 3, 4, 6, 7 | P1 (MVP) |
| US2 | Occupancy calendar (month grid, own bookings highlighted) | 5 | P2 |
| US3 | Auth gate + nav + localization + a11y | 8, 9, 10 | P1/P2 |

## Format: `[ID] [P?] [Story] Description with file path`
- **[P]**: parallelizable (different files, no incomplete dependency)
- All backend calls are relative URLs through the gateway; no tokens in the SPA (ADR-0013/0030).

---

## Phase 1: Data-access — occupancy read on `@roomy/attendance-data-access`

- [ ] T001 [P] [US1] `occupancy-range.ts` — Europe/Berlin range maths: `rangeFor('day'|'week'|'month',
  anchor) -> { from, to }` (each ≤ 31 days, FR-006) and `monthGrid(anchor) -> weeks[][]` (calendar
  cells, leading/trailing days to fill weeks). Spec `occupancy-range.spec.ts`: week = Mon–Sun span,
  month = 1st–last, grid aligns weekdays.
- [ ] T002 [P] [US1] `occupancy.ts` — `OccupancyDay`/`OccupancyRoom`/`Occupant` view models +
  `toOccupancyDays(OccupancyDayResponse[])` mapping (coerce `number | string`; `occupants` undefined when
  the response withholds it — never inferred, FR-003). Spec `occupancy.spec.ts`.
- [ ] T003 [US1] Extend `AttendanceGateway` with `occupancy(scope: { officeId?: OfficeId; roomId?: RoomId },
  from: string, to: string): Observable<OccupancyDay[]>` over the generated `viewOccupancy`; export the
  new view models + range helpers from `src/index.ts`. Spec extension: office scope and room scope call
  `/occupancy` with the right params and map the response (HttpTestingController).

---

## Phase 2: Occupancy list — `occupancy-page` (US1)

- [ ] T004 [US1] RED: `occupancy/occupancy-page.spec.ts` — pick an office (rollup + per-room figures,
  scenarios 1–2); switch to a room scope (scenario 7); day/week/month presets each render every day's
  occupied/capacity (scenario 3); today shows occupant names, a future day shows counts only (scenario
  4); a past range renders read-only with no action controls (scenario 6); empty catalogue + request
  error states.
- [ ] T005 [US1] GREEN: `occupancy/occupancy-page.ts/.html/.css` — scope picker from `listBookableOffices`,
  day/week/month preset selector, `occupancy(scope, from, to)` fetch, per-room + office-rollup figures,
  today/tomorrow names rendered only when present. `today` an input defaulting to `todayInBerlin()`.
- [ ] T006 [US1] Error/empty handling: `404 unknown_office|unknown_room` → localized "no longer
  available" + refresh the catalogue; network/5xx → non-blocking error; empty catalogue → empty-state.
  Extend the spec.

---

## Phase 3: Occupancy calendar — `occupancy-calendar` (US2, FR-004)

- [ ] T007 [US2] RED: `occupancy/occupancy-calendar.spec.ts` — a month grid (from `monthGrid`) where each
  in-month day shows its occupancy figure (from one `occupancy(scope, monthStart, monthEnd)` call) and the
  viewer's booked days (from `myReservations`) are highlighted with a non-colour cue (scenario 5); month
  navigation re-queries.
- [ ] T008 [US2] GREEN: `occupancy/occupancy-calendar.ts/.html/.css` — the month grid, occupancy per cell,
  own-bookings highlight + visually-hidden label, previous/next month. Reuses the scope picker.

---

## Phase 4: Wiring, localization, accessibility (US3, FR-007..FR-009)

- [ ] T009 [US3] Add `occupancy` + `calendar` routes (guarded by `authGuard`) to
  `libs/attendance/feature/src/lib/attendance.routes.ts` (mounted under `/attendance` already). Guard
  spec is covered by `007`; add a route smoke test if useful.
- [ ] T010 [US3] Add the `Occupancy` nav entry to `apps/web/src/app/app.html` for any signed-in user
  (beside Reserve / My reservations).
- [ ] T011 [US3] `occupancy.*` i18n namespace in `apps/web/public/i18n/{en,de}.json` (labels, headings,
  day/month names, preset names, "occupied/capacity" surrounding text, error/empty messages); assert
  DE/EN key parity (scenario 10, FR-008). Mirror the keys in the feature's test transloco helper.
- [ ] T012 [US3] WCAG 2.2 AA pass (FR-009): keyboard operability across the picker, list, and calendar;
  the calendar grid reads correctly to assistive tech; the own-bookings highlight has a non-colour cue;
  visible focus. Extend specs as needed.

---

## Verify (Definition of Done)
- [ ] `pnpm nx run-many -t test lint -p attendance-data-access attendance-feature web` green.
- [ ] `pnpm nx build web` green.
- [ ] OpenAPI spec + generated-client drift gates green (no change expected — `/occupancy` already in the spec).
- [ ] EN/DE i18n key parity holds.
- [ ] Reconcile this `tasks.md` to reality on merge (heed the tasks.md-lag convention).
