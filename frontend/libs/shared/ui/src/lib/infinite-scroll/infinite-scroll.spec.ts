import { Component, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { importSharedUiTestTransloco } from '../../testing/transloco';

import { InfiniteScroll } from './infinite-scroll';

@Component({
  imports: [InfiniteScroll],
  template: `<roomy-infinite-scroll
    [hasMore]="hasMore"
    [loading]="loading"
    (loadMore)="loads = loads + 1"
  >
    <p>projected item</p>
  </roomy-infinite-scroll>`,
})
class HostComponent {
  hasMore = true;
  loading = false;
  loads = 0;
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    imports: [importSharedUiTestTransloco()],
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('InfiniteScroll', () => {
  it('projects the list and offers a Load more control while more pages remain', async () => {
    await renderHost({ hasMore: true });

    expect(screen.getByText('projected item')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Load more' })).toBeTruthy();
  });

  it('emits loadMore when the Load more button is activated', async () => {
    const { fixture } = await renderHost({ hasMore: true });

    await userEvent.click(screen.getByRole('button', { name: 'Load more' }));

    expect(fixture.componentInstance.loads).toBe(1);
  });

  it('disables the control and shows a loading label while a page is in flight', async () => {
    await renderHost({ hasMore: true, loading: true });

    const button = screen.getByRole('button', { name: 'Loading…' });
    expect((button as HTMLButtonElement).disabled).toBe(true);
  });

  it('announces the end of the list and offers no control when no pages remain', async () => {
    await renderHost({ hasMore: false });

    expect(screen.getByText('End of list')).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Load more' })).toBeNull();
  });

  it('auto-loads the next page when the sentinel scrolls into view', async () => {
    let capturedCallback: IntersectionObserverCallback | null = null;
    class MockIntersectionObserver {
      constructor(callback: IntersectionObserverCallback) {
        capturedCallback = callback;
      }
      observe(): void {}
      disconnect(): void {}
      unobserve(): void {}
      takeRecords(): IntersectionObserverEntry[] {
        return [];
      }
    }
    const original = globalThis.IntersectionObserver;
    globalThis.IntersectionObserver =
      MockIntersectionObserver as unknown as typeof IntersectionObserver;

    try {
      const { fixture } = await renderHost({ hasMore: true });
      if (capturedCallback === null) {
        throw new Error('the component did not register an IntersectionObserver');
      }

      capturedCallback(
        [{ isIntersecting: true } as IntersectionObserverEntry],
        {} as IntersectionObserver,
      );

      expect(fixture.componentInstance.loads).toBe(1);
    } finally {
      globalThis.IntersectionObserver = original;
    }
  });
});
