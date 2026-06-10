# Implementation Plan: Endless (infinite-scroll) lists via cursor pagination (010)

**Spec:** `specs/010-endless-lists/spec.md` · **Decision:** ADR-0042 · **Issue:** #133

## Approach

One vertical slice realizing ADR-0042: shared primitives → per-endpoint keyset backend (+ real
Postgres tests) → re-emitted OpenAPI + regenerated clients → web infinite-scroll. Keyset, opaque
cursor, `{ items, nextCursor }`, default limit 50 / max 100, 400 on bad input.

## Bounded contexts touched
- **shared-kernel** — `Page<T>`, `PageRequest`, `CursorCodec` (`…SharedKernel.Pagination`).
- **identity** — `GET /admin/users` keyset on `(DisplayName, Identifier)`.
- **attendance** — `/reservations/mine`, `/reservations/employees`, `/reservations/by-employee/{id}`
  keyset on their sort keys; `/reservations?date=` envelope-only.
- **web** — `@roomy/shared-ui` infinite-scroll; per-context `data-access` facades + feature list
  views.

## Key design points
- Shared primitives in `shared-kernel` so the identity **domain** repository may return `Page<User>`
  (`domain → util`; a `type:application` lib is forbidden to the domain — ADR-0042 option E).
- Keyset predicate written **inline** in each read model's LINQ (EF-translatable; no single-use
  abstraction). Fetch `limit + 1` rows to compute `nextCursor`.
- HTTP boundary returns concrete `*Page` records (`AdminUserPage`, `MyReservationPage`,
  `EmployeePage`, `ReservationPage`) wrapping existing `*Response` records → stable OpenAPI schema
  names; generic `Page<T>` stays internal.
- `/reservations?date=` keeps its `AttendanceDay` replay; only the response is wrapped, `nextCursor`
  always `null`.
- Web: frontend `Page<T>` + `mapPage` in `@roomy/shared-data-access`; `@roomy/shared-ui` hosts an
  `IntersectionObserver` sentinel + "Load more" button; list views accumulate pages in a signal.

## Test strategy (RED→GREEN)
- `Roomy.SharedKernel.Tests`: `PageRequest.From` (default/cap/`< 1`/bad cursor) + `CursorCodec`
  round-trip.
- Identity + attendance **integration** tests (real Postgres): page boundary, next-page contiguity,
  stability across an insert, end-of-list `nextCursor: null`, 400 on bad cursor/limit, 403 unchanged.
- `OpenApiDocumentTests` re-emit; `generate-client` drift gate.
- `@testing-library/angular`: list appends next page on intersection / "Load more"; stops at end.

## Gates
`dotnet build -warnaserror` · `dotnet test` · `dotnet format --verify-no-changes` ·
`pnpm nx affected -t lint test build` · drift gates (`git diff --exit-code` after spec re-emit +
both `generate-client`).
