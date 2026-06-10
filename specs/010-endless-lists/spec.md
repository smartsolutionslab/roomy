# Feature Specification: Endless (infinite-scroll) lists via cursor pagination

**Feature Branch:** `feat/010-endless-lists`
**Status:** Draft
**Created:** 2026-06-10
**Updated:** 2026-06-10
**Realizes:** issue #133 (cursor/keyset pagination for the unbounded list endpoints + web
infinite-scroll). Convention recorded in **ADR-0042**.

## Summary

The lists that grow with company size or over time — the user directory, the employee picker, and a
person's reservation history — are returned in full today: every collection endpoint materializes
its entire result set (no `limit`, cursor, or `Skip`/`Take` anywhere). This feature makes those
lists **endless**: the API paginates with an opaque **cursor** and a capped **limit**, returning a
uniform `{ items, nextCursor }` envelope, and the web app loads the next chunk as the user scrolls.

Per **ADR-0042** the contract is **keyset** (not offset): each list has a stable total sort order
and a `WHERE (sortKey) > @cursor ORDER BY sortKey LIMIT @limit + 1` predicate pushed into SQL, so a
row inserted between two fetches is never skipped or duplicated and the payload can never be
unbounded. Shared primitives (`Page<T>`, `PageRequest`, `CursorCodec`) live in `shared-kernel`; the
web infinite-scroll list is a new `@roomy/shared-ui` primitive (accessible per ADR-0024). The
OpenAPI specs and the generated Angular clients are regenerated and drift-gated (ADR-0036).

## Affected endpoints

Keyset-paginated (genuinely unbounded):
- `GET /admin/users` (identity) — sort `(DisplayName, Identifier)`.
- `GET /reservations/employees` (attendance) — sort `(Name, EmployeeId)`.
- `GET /reservations/mine` (attendance) — sort `(Date, ReservationId)`.
- `GET /reservations/by-employee/{employeeId}` (attendance) — sort `(Date, ReservationId)`.

Envelope only, `nextCursor` always `null` (bounded by daily room capacity; replays the
`AttendanceDay` aggregate in memory, no SQL projection to keyset — ADR-0042):
- `GET /reservations?date=` (attendance).

Out of scope (naturally small / no web consumer): `GET /occupancy`, `/rooms`, `/offices`, and the
organization endpoints.

## User Scenarios & Testing

### Primary User Story
As a user of a long list (my reservations, the admin user directory, the on-behalf employee picker),
I want the list to keep loading as I scroll, so that I see everything without an unbounded payload or
a manual page control.

### Acceptance Scenarios

1. **First page**
   - GIVEN a paginated list endpoint with more items than the limit
   - WHEN it is called with no cursor
   - THEN it returns the first `limit` items in the stable sort order and a non-null `nextCursor`.

2. **Next page**
   - GIVEN the `nextCursor` from a previous page
   - WHEN the endpoint is called with that cursor
   - THEN it returns the next `limit` items, contiguous with the previous page (no gap, no overlap).

3. **End of list**
   - GIVEN the cursor pointing at the last full page
   - WHEN the endpoint is called
   - THEN it returns the remaining items and `nextCursor: null`.

4. **Stability across an insert**
   - GIVEN a first page has been read
   - WHEN a new row is inserted before the second fetch
   - THEN paging with the first page's `nextCursor` skips and duplicates no item from the first page.

5. **Default and capped limit**
   - WHEN no `limit` is given, the default (50) applies
   - WHEN `limit` exceeds the maximum (100) or is below 1, OR the `cursor` is malformed
   - THEN the request is rejected with **400** (the server never silently clamps).

6. **Endless scroll in the web app**
   - GIVEN a list view with more than one page
   - WHEN the user scrolls to the bottom (or activates "Load more")
   - THEN the next page is fetched and appended, until `nextCursor` is null, when loading stops.

7. **The endless list is accessible** (ADR-0024)
   - GIVEN a keyboard/screen-reader user
   - WHEN they reach the end of the loaded items
   - THEN an explicit "Load more" control is reachable and operable, and newly-loaded items are
     announced; "end of list" is conveyed when exhausted.

8. **Uniform envelope for the bounded day list**
   - WHEN `GET /reservations?date=` is called
   - THEN it returns `{ items, nextCursor }` with `nextCursor: null` (same shape, one page).

9. **Authorization is unchanged**
   - The admin-only reads stay admin-only; pagination adds no access; a non-admin still gets 403.

### Edge Cases
- Empty list → `{ items: [], nextCursor: null }`; the web view shows its empty-state.
- A cursor whose item was deleted between fetches → paging resumes from the next item after that
  sort key (keyset is position-by-value, not by row), no error.
- Exactly `limit` items remain → that page returns them with `nextCursor: null` (the `limit + 1`
  probe found no further row).
- A `401` mid-scroll → treated as signed out (existing behaviour), loading halts.

## Requirements

### Functional Requirements
- **FR-001:** Each unbounded endpoint MUST accept an optional opaque `cursor` and an optional
  `limit`, return `{ items, nextCursor }`, and apply the keyset predicate in SQL — never
  materialize-then-slice (ADR-0042).
- **FR-002:** `limit` MUST default to 50 and be capped at 100; a `limit` outside `[1, 100]` or a
  malformed `cursor` MUST be rejected with 400.
- **FR-003:** Each list MUST use a stable, total sort order (per the table above) so the cursor is
  deterministic; paging MUST be stable across an insert (no skip/duplicate).
- **FR-004:** `nextCursor` MUST be `null` exactly when no further items exist.
- **FR-005:** `GET /reservations?date=` MUST return the same envelope with `nextCursor: null`,
  without changing its strong-consistency aggregate-replay semantics.
- **FR-006:** The OpenAPI specs MUST be re-emitted and the generated Angular clients regenerated;
  the drift gate MUST stay green (ADR-0036).
- **FR-007:** The consuming web list views MUST load the next page on scroll and stop at the end,
  accumulating items; loading state and end-of-list MUST be conveyed.
- **FR-008:** The endless list MUST meet WCAG 2.2 AA (an operable "Load more" control as the
  keyboard/screen-reader/no-JS path, announced updates) and be localized DE + EN (ADR-0024).
- **FR-009:** Admin-gated reads remain admin-gated on the server; pagination changes no
  authorization (ADR-0040 guards unchanged).

### Key Entities (view models)
- **Page\<T\>** — `{ items, nextCursor }`; the uniform list envelope (backend + frontend).
- **PageRequest** — a validated `cursor` + `limit` (default 50 / max 100).

## Out of Scope (this feature / deferred)
- Offset/numbered paging and total counts.
- Pagination of naturally-small reads (`/occupancy`, `/rooms`, `/offices`) and organization
  endpoints (no growth risk, no web consumer today).
- Switching `GET /reservations?date=` to the eventually-consistent projection.

## Review & Acceptance Checklist
- [ ] No implementation details in the spec body beyond the ADR-0042 contract it realizes
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] Keyset (not offset); stability-across-insert asserted against real Postgres
- [ ] 400 on bad cursor/limit; `nextCursor: null` at end
- [ ] OpenAPI + generated clients regenerated, drift-gated
- [ ] Web infinite-scroll accessible (Load-more path) + localized
- [ ] Admin-gated reads stay admin-gated
- [ ] No open clarification markers remain
