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
import { TranslocoDirective } from '@jsverse/transloco';
import {
  AttendanceGateway,
  BookableOffice,
  OccupancyDay,
  RangePreset,
  errorCode,
  rangeFor,
  roomId,
  todayInBerlin,
} from '@roomy/attendance-api';
import { FormField, Message, Page, Select, type SelectOption } from '@roomy/shared-ui';

// The occupancy list (OC-1/2/4/6): pick an office (optionally a single room) and a day / week / month
// range, and read each day's office rollup + per-room figures. Occupant names render only when the
// response carries them (today/tomorrow, FR-003); the view is read-only, so a past range simply shows
// history (FR-005). `today` is an input defaulting to the Europe/Berlin day, so the presets are
// deterministic under test.
@Component({
  selector: 'roomy-occupancy-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, Page, FormField, Message, Select],
  templateUrl: './occupancy-page.html',
  styleUrl: './occupancy-page.css',
})
export class OccupancyPage {
  private readonly gateway = inject(AttendanceGateway);
  private readonly destroyRef = inject(DestroyRef);

  readonly today = input<string>(todayInBerlin());

  protected readonly offices = signal<BookableOffice[] | null>(null);
  protected readonly loadFailed = signal(false);

  protected readonly selectedOfficeId = signal<string | null>(null);
  protected readonly selectedRoomId = signal<string>(''); // '' = all rooms (office scope)
  protected readonly preset = signal<RangePreset>('day');
  // null until the viewer picks a date; the effective anchor falls back to `today()`, read lazily so the
  // signal input is bound by the time it is evaluated (a constructor read would see the default).
  protected readonly anchor = signal<string | null>(null);
  protected readonly days = signal<OccupancyDay[] | null>(null);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly presets: readonly RangePreset[] = ['day', 'week', 'month'];
  protected readonly effectiveAnchor = computed(() => this.anchor() ?? this.today());
  protected readonly selectedOffice = computed<BookableOffice | null>(
    () => this.offices()?.find((office) => office.id === this.selectedOfficeId()) ?? null,
  );
  protected readonly officeOptions = computed<SelectOption[]>(() =>
    (this.offices() ?? []).map((office) => ({ value: office.id, label: office.name })),
  );
  protected readonly roomOptions = computed<SelectOption[]>(() =>
    (this.selectedOffice()?.rooms ?? []).map((room) => ({ value: room.id, label: room.name })),
  );

  constructor() {
    this.loadCatalogue();
  }

  private loadCatalogue(): void {
    this.gateway
      .listBookableOffices()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (offices) => this.offices.set(offices),
        error: () => {
          this.loadFailed.set(true);
          this.offices.set([]);
        },
      });
  }

  protected chooseOffice(officeId: string): void {
    this.selectedOfficeId.set(officeId || null);
    this.selectedRoomId.set('');
    this.load();
  }

  protected chooseRoom(roomValue: string): void {
    this.selectedRoomId.set(roomValue);
    this.load();
  }

  protected choosePreset(preset: string): void {
    this.preset.set(preset as RangePreset);
    this.load();
  }

  protected changeAnchor(date: string): void {
    if (date) {
      this.anchor.set(date);
      this.load();
    }
  }

  private load(): void {
    const office = this.selectedOffice();
    if (office === null) {
      this.days.set(null);
      return;
    }

    this.errorKey.set(null);
    const room = this.selectedRoomId();
    const scope = room ? { roomId: roomId(room) } : { officeId: office.id };
    const { from, to } = rangeFor(this.preset(), this.effectiveAnchor());

    this.gateway
      .occupancy(scope, from, to)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (days) => this.days.set(days),
        error: (error: HttpErrorResponse) => {
          const code = errorCode(error);
          if (code === 'unknown_office' || code === 'unknown_room') {
            this.errorKey.set('attendance.occupancy.unknownScope');
            this.loadCatalogue();
          } else {
            this.errorKey.set('attendance.occupancy.loadError');
          }
          this.days.set(null);
        },
      });
  }
}
