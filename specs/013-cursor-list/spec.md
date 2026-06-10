# Feature Specification: Reusable cursor-list paging helper

**Feature Branch:** `feat/013-cursor-list`
**Status:** Draft
**Created:** 2026-06-10
**Realizes:** the reusable endless-list state container decided in **ADR-0049** (extends the
keyset pagination of **ADR-0044 / spec 011**, within the frontend boundaries of ADR-0035).

## Summary

Every keyset-paginated page in the SPA hand-rolls the same cursor state machine
(`items` / `nextCursor` / `loading` / `failed` + a guarded `loadMore`). This feature
extracts it once into a `cursorList(fetch)` helper in `@roomy/shared-data-access`, paired
with the existing presentational `roomy-infinite-scroll`, and migrates the three consumers
(`admin-users`, `my-reservations`, `on-behalf`) onto it. Behaviour is unchanged for users,
the duplication is removed, and a latent stale-append race (switching the on-behalf employee
mid-fetch) is fixed.

## User Scenarios & Testing

### Primary story
As a developer adding a paged list, I declare only the fetch function and bind the helper's
signals to `roomy-infinite-scroll`, instead of re-implementing cursor accumulation.

### Acceptance Scenarios
1. **First page loads** — GIVEN a new `cursorList(fetch)` with auto-load, WHEN it is created,
   THEN it fetches the first page (no cursor) and exposes its `items`, with `loading` true
   while in flight and false after.
2. **Append until the end** — GIVEN a loaded list whose page carried a `nextCursor`, WHEN
   `loadMore()` runs, THEN the next page is appended; WHEN a page returns `nextCursor === null`,
   THEN `hasMore` is false and further `loadMore()` is a no-op.
3. **No concurrent fetch** — GIVEN a page in flight, WHEN `loadMore()` is called again, THEN
   no second request is issued.
4. **Reset reloads from scratch** — GIVEN a loaded list, WHEN `reset()` runs, THEN any
   in-flight request is cancelled (its late response is discarded), items + cursor are
   cleared, and the first page is fetched again.
5. **Clear empties without loading** — WHEN `clear()` runs, THEN items become empty and no
   request is issued.
6. **Optimistic update** — WHEN `update(mutate)` runs after the first page, THEN the
   accumulated items are replaced by `mutate(items)`; before the first page it is a no-op.
7. **Failure surfaces** — GIVEN the fetch errors, THEN `failed` is true and `loading` returns
   to false; the list can still be retried via `loadMore()`.
8. **Consumers unchanged** — the existing `admin-users`, `my-reservations` and `on-behalf`
   component tests (append/stop/empty/error/grant/cancel) stay green after migration.

### Edge Cases
- `autoLoad: false` — the helper fetches nothing until `loadMore()`/`reset()` is called
  (on-behalf's reservations list, which follows the picked employee).
- A late response from a request cancelled by `reset`/`clear` never mutates the list.

## Requirements
- **FR-001:** `cursorList(fetch, options?)` lives in `@roomy/shared-data-access`, depends only
  on `@angular/core` signals + the local `Page<T>`, and is created in an injection context.
- **FR-002:** It exposes `items`/`hasMore`/`loading`/`failed` signals and
  `loadMore`/`reset`/`clear`/`update`, with the semantics in the scenarios above.
- **FR-003:** It tears down with the component (`takeUntilDestroyed`) and cancels the
  in-flight request on `reset`/`clear`.
- **FR-004:** `roomy-infinite-scroll` and `Page<T>` are not changed; the three consumers
  migrate onto the helper with no change to their observable behaviour.

## Out of Scope
- Any change to the wire contract, the gateways, or the scroller UI.
- A combined fetch-and-render component (rejected — ADR-0049 option B).

## Review & Acceptance Checklist
- [ ] `cursorList` helper added with unit tests covering scenarios 1–7
- [ ] All three consumers migrated; their existing tests green (scenario 8)
- [ ] Stale-append race fixed (scenario 4) and covered by a test
- [ ] `pnpm nx affected -t lint test build` green
