import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { BookableOffice, OfficeId, RoomId, roomId } from '@roomy/attendance-api';
import { Select, type SelectOption } from '@roomy/shared-ui';

// The occupancy scope chosen in the picker: a whole office, or a single room within it.
export type OccupancyScope = { readonly officeId: OfficeId } | { readonly roomId: RoomId };

// The office-and-room scope picker shared by the occupancy list and calendar (008): an office dropdown
// and, once an office is chosen, a room dropdown (its placeholder = the whole office). It owns the
// selection and emits the resulting `OccupancyScope` (or null when no office is chosen) so each host only
// reacts to the scope; the bookable catalogue is passed in. Presentational beyond the selection itself —
// it makes no gateway calls. (Reserve keeps its own office picker: it selects a room from an availability
// grid, not a dropdown.)
@Component({
  selector: 'roomy-office-room-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, Select],
  templateUrl: './office-room-picker.html',
})
export class OfficeRoomPicker {
  readonly offices = input.required<readonly BookableOffice[]>();
  readonly scopeChange = output<OccupancyScope | null>();

  protected readonly selectedOfficeId = signal<string | null>(null);
  protected readonly selectedRoomId = signal<string>(''); // '' = the whole office

  protected readonly selectedOffice = computed<BookableOffice | null>(
    () => this.offices().find((office) => office.id === this.selectedOfficeId()) ?? null,
  );
  protected readonly officeOptions = computed<SelectOption[]>(() =>
    this.offices().map((office) => ({ value: office.id, label: office.name })),
  );
  protected readonly roomOptions = computed<SelectOption[]>(() =>
    (this.selectedOffice()?.rooms ?? []).map((room) => ({ value: room.id, label: room.name })),
  );

  protected chooseOffice(officeValue: string): void {
    this.selectedOfficeId.set(officeValue || null);
    this.selectedRoomId.set('');
    this.emitScope();
  }

  protected chooseRoom(roomValue: string): void {
    this.selectedRoomId.set(roomValue);
    this.emitScope();
  }

  private emitScope(): void {
    const office = this.selectedOffice();
    if (office === null) {
      this.scopeChange.emit(null);
      return;
    }

    const room = this.selectedRoomId();
    this.scopeChange.emit(room ? { roomId: roomId(room) } : { officeId: office.id });
  }
}
