import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { MyReservation } from '@roomy/attendance-api';
import { Button } from '@roomy/shared-ui';

@Component({
  selector: 'roomy-reservation-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, Button],
  templateUrl: './reservation-item.html',
  styleUrl: './reservation-item.css',
})
export class ReservationItem {
  readonly reservation = input.required<MyReservation>();
  readonly namespace = input.required<string>();
  readonly showActions = input(false);
  readonly showChange = input(false);
  readonly cancelRequested = output();
  readonly changeRequested = output();
}
