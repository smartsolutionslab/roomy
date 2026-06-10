import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';

import { CursorList, PageFetch, cursorList } from './cursor-list';
import { Page } from './page';

function page<T>(items: T[], nextCursor: string | null = null): Page<T> {
  return { items, nextCursor };
}

function create<T>(fetch: PageFetch<T>, options?: { autoLoad?: boolean }): CursorList<T> {
  return TestBed.runInInjectionContext(() => cursorList(fetch, options));
}

describe('cursorList', () => {
  it('auto-loads the first page with no cursor and exposes its items', () => {
    const list = create((cursor) => of(page(cursor === undefined ? ['a', 'b'] : [], null)));

    expect(list.items()).toEqual(['a', 'b']);
    expect(list.loading()).toBe(false);
    expect(list.hasMore()).toBe(false);
  });

  it('keeps loading true while the first page is in flight', () => {
    const subject = new Subject<Page<string>>();
    const list = create(() => subject);

    expect(list.loading()).toBe(true);
    expect(list.items()).toBeNull();

    subject.next(page(['a'], null));
    subject.complete();

    expect(list.loading()).toBe(false);
    expect(list.items()).toEqual(['a']);
  });

  it('appends each next page and stops once nextCursor is null', () => {
    const fetch = vi.fn<PageFetch<string>>((cursor) =>
      of(cursor === undefined ? page(['a'], 'cursor-2') : page(['b'], null)),
    );
    const list = create(fetch);

    expect(list.items()).toEqual(['a']);
    expect(list.hasMore()).toBe(true);

    list.loadMore();

    expect(list.items()).toEqual(['a', 'b']);
    expect(list.hasMore()).toBe(false);

    list.loadMore();

    expect(list.items()).toEqual(['a', 'b']);
    expect(fetch).toHaveBeenCalledTimes(2);
  });

  it('does not start a second fetch while a page is in flight', () => {
    const fetch = vi.fn<PageFetch<string>>(() => new Subject<Page<string>>());
    const list = create(fetch);

    list.loadMore();

    expect(fetch).toHaveBeenCalledTimes(1);
    expect(list.loading()).toBe(true);
  });

  it('reset cancels the in-flight request, discards its late response and reloads', () => {
    const subjects: Subject<Page<string>>[] = [];
    const list = create(() => {
      const subject = new Subject<Page<string>>();
      subjects.push(subject);
      return subject;
    });

    list.reset();

    expect(subjects.length).toBe(2);

    // The first request was cancelled by reset; its late emission must be ignored.
    subjects[0].next(page(['stale'], null));
    expect(list.items()).toBeNull();

    subjects[1].next(page(['fresh'], null));
    subjects[1].complete();
    expect(list.items()).toEqual(['fresh']);
  });

  it('clear empties the list without fetching', () => {
    const fetch = vi.fn<PageFetch<string>>(() => of(page(['a'], 'cursor-2')));
    const list = create(fetch);

    list.clear();

    expect(list.items()).toBeNull();
    expect(list.loading()).toBe(false);
    expect(fetch).toHaveBeenCalledTimes(1);
  });

  it('update replaces the accumulated items, and is a no-op before the first page', () => {
    const subject = new Subject<Page<string>>();
    const list = create(() => subject, { autoLoad: false });

    list.update((items) => [...items, 'x']);
    expect(list.items()).toBeNull();

    list.loadMore();
    subject.next(page(['a', 'b'], null));
    subject.complete();

    list.update((items) => items.filter((item) => item !== 'a'));
    expect(list.items()).toEqual(['b']);
  });

  it('flips failed and clears loading on error, then can retry', () => {
    let calls = 0;
    const list = create<string>(() => {
      calls += 1;
      return calls === 1 ? throwError(() => new Error('boom')) : of(page(['a'], null));
    });

    expect(list.failed()).toBe(true);
    expect(list.loading()).toBe(false);

    list.loadMore();

    expect(list.failed()).toBe(false);
    expect(list.items()).toEqual(['a']);
  });

  it('does not fetch until asked when autoLoad is false', () => {
    const fetch = vi.fn<PageFetch<string>>(() => of(page(['a'], null)));
    const list = create(fetch, { autoLoad: false });

    expect(fetch).not.toHaveBeenCalled();
    expect(list.items()).toBeNull();

    list.loadMore();

    expect(fetch).toHaveBeenCalledTimes(1);
    expect(list.items()).toEqual(['a']);
  });
});
