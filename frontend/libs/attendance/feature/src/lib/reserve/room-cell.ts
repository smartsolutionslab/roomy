import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { BookableRoom, RoomAvailability } from '@roomy/attendance-api';
import { UsageBar } from '@roomy/shared-ui';

@Component({
  selector: 'roomy-room-cell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, UsageBar],
  templateUrl: './room-cell.html',
  styleUrl: './room-cell.css',
})
export class RoomCell {
  readonly room = input.required<BookableRoom>();
  readonly availability = input<RoomAvailability | undefined>(undefined);
  readonly selected = input(false);
  readonly chosen = output();

  protected readonly isFull = computed(() => this.availability()?.isFull ?? false);

  protected readonly availableSlots = computed(() => {
    const availability = this.availability();
    return availability ? availability.capacity - availability.occupied : this.room().capacity;
  });
}
