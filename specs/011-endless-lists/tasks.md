---
description: "Task list for Endless (infinite-scroll) lists via cursor pagination (011)"
---

# Tasks: Endless (infinite-scroll) lists via cursor pagination

**Input**: Design documents in `specs/011-endless-lists/` (plan.md, spec.md) and ADR-0044.

**Tests**: REQUIRED (RED→GREEN). xUnit + Shouldly for shared primitives, host endpoints, and read
models (real Postgres for keyset); `@testing-library/angular` on `vitest-analog` for the SPA.

**Organization**: Adds `…SharedKernel.Pagination`; touches identity + attendance hosts/read models;
adds `@roomy/shared-ui`; wires existing `data-access` facades and feature list views. No new backend
context; no write-model change.

## Story label map
| Label | Story | Scenarios | Priority |
|---|---|---|---|
| US1 | Shared cursor-pagination convention (primitives) | 1, 2, 3, 5 | P1 (MVP) |
| US2 | Keyset-paginate the unbounded endpoints (+ envelope for the day list) | 1–5, 8, 9 | P1 (MVP) |
| US3 | OpenAPI + generated clients | 6 (FR-006) | P1 |
| US4 | Web endless scroll (accessible, localized) | 6, 7 | P1/P2 |

---

## Phase 1: Shared primitives (US1)
- [x] T001 [US1] `Page<T>(IReadOnlyList<T> Items, string? NextCursor)` in
  `backend/libs/shared-kernel/src/Pagination/`.
- [x] T002 [US1] `CursorCodec.Encode<TKey>/TryDecode<TKey>` (base64url JSON). Test:
  round-trip a sort-key record; `TryDecode` returns false on malformed input.
- [x] T003 [US1] `PageRequest` value object + `PageRequest.From(string? cursor, int? limit) →
  Result<PageRequest>` (default 50, cap 100, `>= 1`, decode cursor; `Error.Validation` otherwise).
  Test: default; cap rejected; `< 1` rejected; bad cursor rejected; valid carries decoded cursor.

## Phase 2: Identity `/admin/users` (US2)
- [x] T004 [US2] `IUserRepository.GetPageAsync(PageRequest, CancellationToken) → Task<Page<User>>`
  keyset `(DisplayName, Identifier)`; implement in `UserRepository` (`limit + 1` probe). Replace the
  single `GetAllAsync` use; update the two test fakes.
- [x] T005 [US2] `AdminUserEndpoints.ListAccountsAsync`: bind `cursor`/`limit`, `PageRequest.From`
  (400 on invalid), return `AdminUserPage`; `.Produces<AdminUserPage>()`. Re-emit OpenAPI.
- [x] T006 [US2] Identity integration tests (real Postgres): first page + `nextCursor`; next page
  contiguity; end `nextCursor: null`; stability across an insert; 400 bad cursor/limit; 403 non-admin.

## Phase 3: Attendance (US2)
- [x] T007 [US2] `ViewMyReservations`/`ViewEmployees` carry a `PageRequest`; handlers return
  `Result<Page<…View>>`.
- [x] T008 [US2] `MyReservationsReadModel` keyset `(Date, ReservationId)`; `EmployeeCatalog` keyset
  `(Name, EmployeeId)`; both return `Page<T>` with the `limit + 1` probe. Read-model integration
  tests (real Postgres): boundary + stability.
- [x] T009 [US2] `ReservationEndpoints`: `ViewMine`/`ViewEmployees`/`ViewForEmployee` bind
  `cursor`/`limit` + validate + return `MyReservationPage`/`EmployeePage`; `ViewAsync` wraps in
  `ReservationPage(items, nextCursor: null)`. `.Produces<…Page>()`. Host tests + re-emit OpenAPI.

## Phase 4: Generated clients (US3)
- [x] T010 [US3] `nx run identity-data-access:generate-client` +
  `nx run attendance-data-access:generate-client`; commit regenerated trees; drift gate green.

## Phase 5: Web endless scroll (US4)
- [x] T011 [US4] Frontend `Page<T>` + `mapPage` in `@roomy/shared-data-access`.
- [x] T012 [US4] New `@roomy/shared-ui` (`type:ui`/`context:shared`): infinite-scroll list
  (`IntersectionObserver` sentinel + "Load more" button, stop at `nextCursor` null), WCAG 2.2 AA.
  `@testing-library` spec: emits "load next" on intersection / button; hides at end.
- [x] T013 [US4] Facades take `cursor`/`limit`, return `Observable<Page<…>>`:
  `attendance-gateway` (`reservationsMine`/`listEmployees`/`reservationsFor`; `/reservations?date=`
  unwraps `.items`) + identity admin `listUsers`. Update `.spec.ts`.
- [x] T014 [US4] `my-reservations-page`, `on-behalf-page` (picker + employee list), `admin-users-page`
  accumulate pages via `@roomy/shared-ui`; Transloco "Load more"/"End of list" (DE + EN). Specs:
  append on intersection / "Load more"; stop at end.

## Phase 6: Verify
- [x] T015 Full gates: `dotnet build -warnaserror`, `dotnet test`, `dotnet format
  --verify-no-changes`, `pnpm nx affected -t lint test build`, both drift gates. Atomic Conventional
  Commits; PR. **All green; `011-endless-lists` complete.**
