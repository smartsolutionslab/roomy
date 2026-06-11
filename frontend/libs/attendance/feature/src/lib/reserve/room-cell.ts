import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { BookableRoom, RoomAvailability } from '@roomy/attendance-api';

@Component({
  selector: 'roomy-room-cell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective],
  templateUrl: './room-cell.html',
  styleUrl: './room-cell.css',
})
export class RoomCell {
  readonly room = input.required<BookableRoom>();
  readonly availability = input<RoomAvailability | undefined>(undefined);
  readonly selected = input(false);
  readonly chosen = output();

  protected readonly isFull = computed(() => this.availability()?.isFull ?? false);

  protected readonly remaining = computed(() => {
    const availability = this.availability();
    return availability ? availability.capacity - availability.occupied : this.room().capacity;
  });

  // How full the room is, 0–100, for the availability bar. Unknown availability (no day chosen yet) reads
  // as empty.
  protected readonly occupiedPercent = computed(() => {
    const availability = this.availability();
    return availability ? Math.round((availability.occupied / availability.capacity) * 100) : 0;
  });
}
