# 0044. Cursor/keyset pagination for endless lists

- **Status:** Accepted
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

No backend list endpoint is paginated: every collection endpoint materializes and returns its
**entire** result set (verified by grep — no `page`/`pageSize`, cursor, `Skip`/`Take`, or
`TotalCount` anywhere; the read models do no internal limiting). For the lists that grow with
company size or over time this payload is unbounded:

- `GET /admin/users` (identity) — every user account; grows with the company.
- `GET /reservations/employees` (attendance) — the full employee directory for the admin picker.
- `GET /reservations/mine` (attendance) — the caller's reservations, past **and** future; grows
  over time.
- `GET /reservations/by-employee/{employeeId}` (attendance) — one employee's reservations, all time.

The web app wants **endless (infinite-scroll) lists** — the user scrolls and the client
transparently loads the next chunk. That needs a server pagination contract. This decision fixes a
**single, consistent convention** across services so each list view, generated client, and read
model follows the same shape; the consuming endpoints and the web infinite-scroll land with it
(`011-endless-lists`).

This is a cross-cutting structural change, so it is recorded here **before** the implementing code
(golden rule 4).

## Decision drivers

- Stability under concurrent inserts: endless scroll appends pages as the user goes, so a row
  inserted between two fetches must not skip or duplicate an item.
- The contract must be uniform across every list so one client primitive and one mental model serve
  all of them, and the sort key can change later without breaking clients.
- The limit must be pushed into the query — never materialize-then-slice — so an unbounded payload
  is structurally impossible.
- Reuse the repo's primitives (`Result`/`Error`, value objects in `shared-kernel`) and gates
  (build-time OpenAPI emit + `generate-client` drift gate, ADR-0036).

## Decision

**Keyset (cursor) pagination, not offset paging**, behind one convention.

1. **Request.** Two optional query parameters: an opaque `cursor` (absent = first page) and a
   `limit` with a **default of 50** and a **hard maximum of 100**. A `limit` below 1 or above 100,
   or a malformed `cursor`, is rejected with **400** (`Error.Validation` → ProblemDetails) — the
   server never silently clamps.
2. **Response envelope.** Every paginated list returns `{ items: [...], nextCursor: string | null }`.
   `nextCursor` is `null` exactly when there are no more items.
3. **Opaque cursor.** The cursor is the base64url-encoded JSON of the **last returned item's
   sort-key tuple**. It is opaque to clients so the sort key can evolve without a contract break.
4. **Keyset predicate in the query.** Read models push
   `WHERE (sortKey) > @cursor ORDER BY sortKey LIMIT @limit + 1` into SQL — fetching `limit + 1`
   rows to detect whether a further page exists — and never load-then-slice. Each list picks a
   **stable, total** sort order so the cursor is deterministic across inserts. The key is chosen to
   be both meaningful and **EF-translatable** — a single unique column where one exists, avoiding a
   `Guid` tiebreaker (C# `Guid` has no comparison operator to translate):
   - reservation history (`mine`, `by-employee`) by `Date` alone — the *one reservation per employee
     per day* invariant makes `Date` unique per employee, so no tiebreaker is needed;
   - the employee directory by `(Name, EmployeeId)` — names can collide, so the id is the tiebreaker
     (compared as text so it translates);
   - users by `Email` — the unique account key, a single text column.

**Shared primitives live in `shared-kernel`** (`SmartSolutionsLab.Roomy.SharedKernel.Pagination`)
so the domain layer may use them (a domain repository signature returns `Page<User>`), which a
`type:application` lib could not provide under the dependency rule:

- `Page<T>(IReadOnlyList<T> Items, string? NextCursor)` — the internal envelope returned by read
  models and query handlers.
- `PageRequest` — a value object built with `PageRequest.From(cursor, limit) → Result<PageRequest>`
  that applies the default/maximum/`>= 1` rules and carries the decoded cursor.
- `CursorCodec` — `Encode<TKey>` / `TryDecode<TKey>` over base64url JSON; each list owns a tiny
  sort-key record (e.g. `ReservationCursor(DateOnly Date, Guid Id)`).

The keyset predicate is written **inline in each read model's LINQ** (clear and EF-translatable); no
single-use `IQueryable` keyset abstraction is introduced (simplicity first).

**At the HTTP boundary, each endpoint returns a concrete `*Page` response record** (e.g.
`EmployeePage`, `MyReservationPage`, `AdminUserPage`) wrapping its existing `*Response` record —
not a generic `Page<EmployeeResponse>` — so the emitted OpenAPI schema names are stable and clean
for the drift gate (ADR-0036). The generic `Page<T>` stays internal to read models and handlers.

**`GET /reservations?date=` is wrapped in the same envelope but with `nextCursor` always `null`.**
That read replays the `AttendanceDay` aggregate in memory (no SQL projection to keyset) and is
bounded by daily room capacity, so it cannot grow unbounded. It adopts the envelope for **contract
uniformity** only; it is deliberately **not** switched to the eventually-consistent `Reservations`
projection, which would trade its strong read-your-writes consistency for pagination it does not
need.

**Web consumes it through a new shared UI library** `libs/shared/ui` → `@roomy/shared-ui`
(`type:ui`/`context:shared`), generated with the ADR-0035 `@nx/angular:library …
--unitTestRunner=vitest-analog` convention. It hosts one infinite-scroll list primitive: an
`IntersectionObserver` sentinel that requests the next page when scrolled into view, **plus an
explicit "Load more" button** as the keyboard/screen-reader/no-JS path (WCAG 2.2 AA, ADR-0024),
stopping when `nextCursor` is `null`. The lib fits the existing `type:ui`/`context:shared` taxonomy
(ADR-0035) — no taxonomy change, only the new lib.

## Considered options

- **A — Offset/`Skip`-`Take` numbered pages.** Familiar, but a row inserted between two fetches
  shifts every later offset, so endless scroll skips or duplicates items, and deep offsets scan and
  discard a growing prefix. Rejected — keyset is stable and O(limit) at any depth.
- **B — Keyset, opaque cursor envelope (chosen).** Stable across inserts, cheap at any depth, and
  the opaque cursor keeps the sort key a server detail.
- **C — Numeric/visible cursor (expose the sort key fields).** Couples clients to the sort columns;
  a sort-order change becomes a breaking contract change. Rejected for opacity.
- **D — Generic `Page<T>` at the HTTP boundary.** One type, but the OpenAPI generic schema names
  (`PageOfEmployeeResponse`) are generator-dependent and noisier in the drift gate. Rejected in
  favour of explicit `*Page` records at the edge, keeping the generic only internally.
- **E — Put `Page`/`PageRequest` in a `type:application` contracts lib.** The identity domain
  repository returns a page, and `domain → application` is forbidden by the dependency rule.
  Rejected; the primitives belong in `shared-kernel` (`domain → util`).

## Consequences

- A new `SmartSolutionsLab.Roomy.SharedKernel.Pagination` namespace holds `Page<T>`, `PageRequest`,
  and `CursorCodec`, unit-tested in `Roomy.SharedKernel.Tests` (request validation, cursor
  round-trip).
- The four unbounded endpoints accept `cursor` + `limit`, cap `limit`, and return `{ items,
  nextCursor }` with the keyset predicate in SQL — asserted by integration tests against real
  Postgres (page boundary, stability across an insert, end-of-list `nextCursor: null`, 400 on a bad
  cursor/limit). `GET /reservations?date=` returns the envelope with `nextCursor: null`.
- The OpenAPI specs and the generated Angular clients change; both regenerate offline and are
  drift-gated (ADR-0036). A contributor must re-run `generate-client` after a contract change.
- A new `@roomy/shared-ui` library exists; every context's list views compose its infinite-scroll
  primitive, so scroll/loading/at-end behaviour stays uniform and accessible.
- No list endpoint in scope can return an unbounded payload. Naturally-small reads (`/occupancy`,
  `/rooms`, `/offices`) and organization endpoints are out of scope (no growth risk, no web
  consumer today) and may adopt the convention later without a new decision.
