import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  effect,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

// The endless-list primitive (ADR-0042, ADR-0024): the parent projects the loaded items and owns the
// accumulation; this component signals when to fetch the next page. An IntersectionObserver auto-loads
// when the sentinel scrolls into view (progressive enhancement), and an always-present "Load more"
// button is the keyboard / screen-reader / no-JS path (WCAG 2.2 AA). When no further page exists it
// announces the end of the list. The parent passes hasMore (nextCursor !== null) and loading.
@Component({
  selector: 'roomy-infinite-scroll',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective],
  templateUrl: './infinite-scroll.html',
  styleUrl: './infinite-scroll.css',
})
export class InfiniteScroll {
  readonly hasMore = input.required<boolean>();
  readonly loading = input<boolean>(false);
  readonly loadMore = output<void>();

  private readonly sentinel = viewChild<ElementRef<HTMLElement>>('sentinel');
  private readonly destroyRef = inject(DestroyRef);
  private observer: IntersectionObserver | null = null;

  constructor() {
    effect(() => {
      const sentinel = this.sentinel();
      this.observer?.disconnect();
      this.observer = null;

      if (sentinel && typeof IntersectionObserver !== 'undefined') {
        this.observer = new IntersectionObserver((entries) => {
          if (entries.some((entry) => entry.isIntersecting)) {
            this.requestMore();
          }
        });
        this.observer.observe(sentinel.nativeElement);
      }
    });

    this.destroyRef.onDestroy(() => this.observer?.disconnect());
  }

  protected requestMore(): void {
    if (this.hasMore() && !this.loading()) {
      this.loadMore.emit();
    }
  }
}
