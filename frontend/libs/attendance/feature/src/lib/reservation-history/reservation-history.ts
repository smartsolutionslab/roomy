import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { MyReservation } from '@roomy/attendance-api';
import { InfiniteScroll } from '@roomy/shared-ui';

import { ReservationItem } from '../reservation-item/reservation-item';

// The booking history shared by the my-reservations and on-behalf pages: an endless list of the
// upcoming reservations (cancellable, optionally changeable) and the past ones (read-only), under their
// headings. The caller owns the cursor list and the upcoming/past split; this renders them and bubbles
// the row intents. `namespace` selects the translation keys; `headingLevel` fits the surrounding outline.
@Component({
  selector: 'roomy-reservation-history',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, InfiniteScroll, ReservationItem],
  templateUrl: './reservation-history.html',
  styleUrl: './reservation-history.css',
})
export class ReservationHistory {
  readonly upcoming = input.required<readonly MyReservation[]>();
  readonly past = input.required<readonly MyReservation[]>();
  readonly namespace = input.required<string>();
  readonly showChange = input(false);
  readonly hasMore = input(false);
  readonly loading = input(false);
  readonly headingLevel = input<2 | 3>(2);

  readonly loadMore = output<void>();
  readonly cancelRequested = output<MyReservation>();
  readonly changeRequested = output<MyReservation>();
}
