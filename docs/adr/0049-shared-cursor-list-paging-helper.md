# 0049. Shared cursorList paging helper for keyset lists

- **Status:** Accepted
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

ADR-0044 introduced keyset (cursor) pagination for endless lists: a gateway returns a
`Page<T>` (`{ items, nextCursor }`, ADR-0020 boundary), and the presentational
`roomy-infinite-scroll` (ADR-0044/0024) signals when to fetch the next slice while the
parent owns accumulation. In practice every paged page re-implemented the *same* cursor
state machine by hand — `items`/`nextCursor`/`loadingMore`/`loadFailed` signals plus a
guarded `loadMore()` that appends the page and tracks the cursor. It appears verbatim in
`admin-users`, `my-reservations` and twice in `on-behalf` (the employee directory and the
selected employee's reservations).

The duplication had drifted and hidden a bug: none of the copies cancel an in-flight
request, so switching the on-behalf employee quickly can append a stale page to the new
list. "Parent owns accumulation" (ADR-0044) is the right boundary, but *every* parent
owning the identical machine by copy-paste is not.

Where should the cursor accumulation live so each page declares only *what to fetch*?

## Decision drivers

- **Remove the duplication** without changing the ADR-0044 split (presentational scroller +
  parent-owned state) or the `Page<T>` boundary type.
- **Boundaries (ADR-0035).** State that orchestrates gateway calls is a `data-access`
  concern; the helper must stay consumable as `feature → data-access` and depend only on
  `@angular/core` signals + the local `Page<T>`.
- **Cover the real shapes:** auto-load on creation (most lists), deferred load + reload from
  scratch when a parameter changes (on-behalf reservations follow the picked employee),
  empty-without-load when a selection clears, and optimistic local mutation (cancel a row,
  patch a granted role).
- **Correctness:** a reload must cancel the previous in-flight request so a stale page can
  never append.

## Considered options

- **A — Keep copy-pasting** the state machine per page. Status quo; drifts, carries the
  stale-append bug, and grows with every new list.
- **B — An all-in-one smart `<roomy-paged-list>`** that fetches *and* renders via a
  projected template. One "control", but a `ui` component would have to import `Page<T>`
  (a `data-access` type) and own RxJS state — crossing the `ui → data-access` boundary and
  fighting ADR-0035/0048's presentational primitives.
- **C — A `cursorList()` factory in `@roomy/shared-data-access` (chosen)** that owns the
  state machine and is paired with the unchanged `roomy-infinite-scroll`. Each page keeps
  its own markup and declares only the fetch function.

## Decision

**Option C.** `@roomy/shared-data-access` gains `cursorList(fetch, options?)`, called in an
injection context (a component field initializer). It returns a `CursorList<T>`:

- **Signals:** `items` (`T[] | null` — null until the first page resolves), `hasMore`,
  `loading`, `failed`.
- **`loadMore()`** — fetch and append the next page (the first when no cursor yet); a no-op
  while a page is in flight or the list has ended. An explicit `ended` flag (set when a page
  returns `nextCursor === null`) distinguishes "not loaded yet" from "no more", so the first
  load is never mistaken for the end.
- **`reset()`** — cancel any in-flight request, discard items + cursor, and load the first
  page again (a parameter changed).
- **`clear()`** — empty the list without loading (a selection cleared).
- **`update(mutate)`** — replace the accumulated items for an optimistic change; a no-op
  before the first page resolves.

`options.autoLoad` (default `true`) loads the first page on creation; on-behalf's
reservations list passes `false` and drives it via `reset()`/`clear()` from the picker. The
helper tears down with `takeUntilDestroyed` and additionally unsubscribes the in-flight
request on `reset`/`clear`, fixing the stale-append race.

`admin-users`, `my-reservations` and `on-behalf` migrate onto it, deleting their hand-rolled
machines; `roomy-infinite-scroll` and `Page<T>` are unchanged.

## Consequences

**Positive**
- One tested implementation of cursor accumulation; pages shrink to a `cursorList(...)`
  declaration plus their own markup. The stale-append race is fixed for every consumer.
- Stays within ADR-0035 boundaries and the ADR-0044 presentational/stateful split; no new
  dependency.

**Negative / trade-offs**
- A small amount of imperative state lives behind a factory rather than in the component;
  authors call `reset`/`clear` for parameterized lists instead of resetting signals inline.
- The helper must be created in an injection context (field initializer / constructor),
  like any `inject()`-based utility.

## Follow-ups
- Future paged lists compose `cursorList` + `roomy-infinite-scroll` rather than re-deriving
  the machine. Extends ADR-0044; no taxonomy change.
