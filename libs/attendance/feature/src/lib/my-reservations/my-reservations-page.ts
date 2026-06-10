import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import {
  AttendanceGateway,
  MyReservation,
  isPastDay,
  todayInBerlin,
} from '@roomy/attendance-data-access';
import { Button, InfiniteScroll, Message, Page } from '@roomy/shared-ui';

type ResultMessage = { key: string; params?: Record<string, unknown> };

// The signed-in employee's own reservations (AT-4): past and upcoming, with cancel offered only on
// upcoming rows (FR-006/FR-007) and "change" performed as cancel + re-reserve — a navigation into the
// reserve flow, never a combined edit (AT-5, FR-008). `today` is an input defaulting to the
// Europe/Berlin calendar day, so the upcoming/past split is deterministic under test.
@Component({
  selector: 'roomy-my-reservations-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, InfiniteScroll, Page, Message, Button],
  templateUrl: './my-reservations-page.html',
  styleUrl: './my-reservations-page.css',
})
export class MyReservationsPage {
  private readonly gateway = inject(AttendanceGateway);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly today = input<string>(todayInBerlin());

  protected readonly reservations = signal<MyReservation[] | null>(null);
  protected readonly nextCursor = signal<string | null>(null);
  protected readonly loadingMore = signal(false);
  protected readonly loadFailed = signal(false);
  protected readonly result = signal<ResultMessage | null>(null);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly upcoming = computed(() =>
    (this.reservations() ?? [])
      .filter((reservation) => !isPastDay(reservation.date, this.today()))
      .sort((left, right) => left.date.localeCompare(right.date)),
  );
  protected readonly past = computed(() =>
    (this.reservations() ?? [])
      .filter((reservation) => isPastDay(reservation.date, this.today()))
      .sort((left, right) => right.date.localeCompare(left.date)),
  );

  constructor() {
    this.loadMore();
  }

  // Loads the next page (the first when no cursor yet) and appends it, so the history grows as the
  // employee scrolls or activates "Load more" (ADR-0044); the upcoming/past split derives from the
  // accumulated set. nextCursor === null marks the end.
  protected loadMore(): void {
    if (this.loadingMore()) {
      return;
    }

    this.loadingMore.set(true);
    this.gateway
      .myReservations(this.nextCursor() ?? undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          this.reservations.update((current) => [...(current ?? []), ...page.items]);
          this.nextCursor.set(page.nextCursor);
          this.loadingMore.set(false);
        },
        error: () => {
          this.loadFailed.set(true);
          this.reservations.update((current) => current ?? []);
          this.loadingMore.set(false);
        },
      });
  }

  protected cancel(reservation: MyReservation): void {
    this.result.set(null);
    this.errorKey.set(null);

    this.gateway
      .cancel(reservation.id, reservation.date)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.reservations.update((items) =>
            (items ?? []).filter((candidate) => candidate.id !== reservation.id),
          );
          this.result.set({ key: 'attendance.mine.cancelled' });
        },
        error: (error: HttpErrorResponse) => this.handleCancelError(error),
      });
  }

  // Change = cancel + re-reserve (AT-5): cancel the existing reservation, then go to the reserve flow to
  // book the new room/office/day. There is no single combined edit step.
  protected change(reservation: MyReservation): void {
    this.result.set(null);
    this.errorKey.set(null);

    this.gateway
      .cancel(reservation.id, reservation.date)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.reservations.update((items) =>
            (items ?? []).filter((candidate) => candidate.id !== reservation.id),
          );
          this.goToReserve();
        },
        error: (error: HttpErrorResponse) => this.handleCancelError(error),
      });
  }

  protected goToReserve(): void {
    void this.router.navigate(['..', 'reserve'], { relativeTo: this.route });
  }

  private handleCancelError(error: HttpErrorResponse): void {
    const code = (error.error as { code?: string } | null)?.code;
    this.errorKey.set(
      code === 'past_immutable'
        ? 'attendance.mine.errors.pastImmutable'
        : 'attendance.mine.errors.generic',
    );
  }
}
