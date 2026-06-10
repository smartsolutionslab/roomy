import { DestroyRef, Signal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subscription } from 'rxjs';

import { Page } from './page';

// Fetches one keyset page; `cursor` is undefined for the first page and the opaque nextCursor of the
// previous page thereafter (ADR-0044).
export type PageFetch<T> = (cursor: string | undefined) => Observable<Page<T>>;

// The reusable cursor-paging state container (ADR-0049): owns accumulation, the cursor, and the
// loading/failed flags so a paged page declares only its fetch. Pair it with `roomy-infinite-scroll`.
export interface CursorList<T> {
  // Accumulated items; null until the first page resolves, so views can show a loading placeholder.
  readonly items: Signal<T[] | null>;
  readonly hasMore: Signal<boolean>;
  readonly loading: Signal<boolean>;
  readonly failed: Signal<boolean>;
  // Fetch and append the next page (the first when no cursor yet). A no-op while a page is in flight
  // or the list has ended.
  loadMore(): void;
  // Cancel any in-flight request, discard the items + cursor, and load the first page again — e.g. a
  // filter or selection that the fetch closes over has changed.
  reset(): void;
  // Empty the list back to its unloaded state without fetching (e.g. the selection was cleared).
  clear(): void;
  // Replace the accumulated items for an optimistic change (drop a cancelled row, patch a field). A
  // no-op before the first page resolves.
  update(mutate: (items: T[]) => T[]): void;
}

export function cursorList<T>(
  fetch: PageFetch<T>,
  options?: { autoLoad?: boolean },
): CursorList<T> {
  const destroyRef = inject(DestroyRef);

  const items = signal<T[] | null>(null);
  const nextCursor = signal<string | null>(null);
  // Distinguishes "no more pages" from "not loaded yet" — the cursor is null in both states, so the
  // first load must not be mistaken for the end of the list.
  const ended = signal(false);
  const loading = signal(false);
  const failed = signal(false);

  let inFlight: Subscription | null = null;

  function cancelInFlight(): void {
    inFlight?.unsubscribe();
    inFlight = null;
  }

  function loadMore(): void {
    if (loading() || ended()) return;

    loading.set(true);
    failed.set(false);
    inFlight = fetch(nextCursor() ?? undefined)
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe({
        next: (loadedPage) => {
          items.update((current) => [...(current ?? []), ...loadedPage.items]);
          nextCursor.set(loadedPage.nextCursor);
          ended.set(loadedPage.nextCursor === null);
          loading.set(false);
          inFlight = null;
        },
        error: () => {
          failed.set(true);
          items.update((current) => current ?? []);
          loading.set(false);
          inFlight = null;
        },
      });
  }

  function clear(): void {
    cancelInFlight();
    items.set(null);
    nextCursor.set(null);
    ended.set(false);
    failed.set(false);
    loading.set(false);
  }

  function reset(): void {
    clear();
    loadMore();
  }

  function update(mutate: (items: T[]) => T[]): void {
    items.update((current) => (current === null ? current : mutate(current)));
  }

  if (options?.autoLoad ?? true) {
    loadMore();
  }

  return {
    items,
    hasMore: computed(() => !ended()),
    loading,
    failed,
    loadMore,
    reset,
    clear,
    update,
  };
}
