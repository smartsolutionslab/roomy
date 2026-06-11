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
  errorCode,
  partitionReservationsByDay,
  todayInBerlin,
} from '@roomy/attendance-api';
import { cursorList, type ResultMessage } from '@roomy/shared-data-access';
import { Button, Message, Page } from '@roomy/shared-ui';

import { ReservationHistory } from '../reservation-history/reservation-history';

// The signed-in employee's own reservations (AT-4): past and upcoming, with cancel offered only on
// upcoming rows (FR-006/FR-007) and "change" performed as cancel + re-reserve — a navigation into the
// reserve flow, never a combined edit (AT-5, FR-008). `today` is an input defaulting to the
// Europe/Berlin calendar day, so the upcoming/past split is deterministic under test.
@Component({
  selector: 'roomy-my-reservations-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, Page, Message, Button, ReservationHistory],
  templateUrl: './my-reservations-page.html',
})
export class MyReservationsPage {
  private readonly gateway = inject(AttendanceGateway);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly today = input<string>(todayInBerlin());

  // The endless reservation history (ADR-0044/0049): the helper owns the cursor accumulation; the
  // upcoming/past split derives from its accumulated items.
  protected readonly list = cursorList<MyReservation>((cursor) =>
    this.gateway.myReservations(cursor),
  );

  protected readonly result = signal<ResultMessage | null>(null);
  protected readonly errorKey = signal<string | null>(null);

  private readonly schedule = computed(() =>
    partitionReservationsByDay(this.list.items() ?? [], this.today()),
  );
  protected readonly upcoming = computed(() => this.schedule().upcoming);
  protected readonly past = computed(() => this.schedule().past);

  protected cancel(reservation: MyReservation): void {
    this.result.set(null);
    this.errorKey.set(null);

    this.gateway
      .cancel(reservation.id, reservation.date)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.list.update((items) => items.filter((candidate) => candidate.id !== reservation.id));
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
          this.list.update((items) => items.filter((candidate) => candidate.id !== reservation.id));
          this.goToReserve();
        },
        error: (error: HttpErrorResponse) => this.handleCancelError(error),
      });
  }

  protected goToReserve(): void {
    void this.router.navigate(['..', 'reserve'], { relativeTo: this.route });
  }

  private handleCancelError(error: HttpErrorResponse): void {
    this.errorKey.set(
      errorCode(error) === 'past_immutable'
        ? 'attendance.mine.errors.pastImmutable'
        : 'attendance.mine.errors.generic',
    );
  }
}
