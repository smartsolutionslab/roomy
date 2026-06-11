import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import {
  AttendanceGateway,
  BookableOffice,
  BookableRoom,
  EmployeeId,
  OfficeId,
  RoomAvailability,
  RoomId,
  bookableDaysFrom,
  errorCode,
  todayInBerlin,
} from '@roomy/attendance-api';
import { type ResultMessage } from '@roomy/shared-data-access';
import { Button, DaySelect, Message, Page, TileGroup, type SelectOption } from '@roomy/shared-ui';

// The reserve flow (AT-1/AT-2): pick an office, then a day, then a room with a remaining place, then
// confirm. The catalogue and the per-day availability come from the attendance context only; the day
// chooser offers only bookable days (FR-003), so a past/weekend/out-of-window day cannot be picked, and
// a full room is shown unbookable before submit (FR-002). `today` is an input defaulting to the
// Europe/Berlin calendar day, so the day rules are deterministic under test.
@Component({
  selector: 'roomy-reserve-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, Page, Message, Button, DaySelect, TileGroup],
  templateUrl: './reserve-page.html',
  styleUrl: './reserve-page.css',
})
export class ReservePage {
  private readonly gateway = inject(AttendanceGateway);
  private readonly destroyRef = inject(DestroyRef);

  readonly today = input<string>(todayInBerlin());
  // When set (administrator on-behalf, 009 AT-6), the reservation is created for that employee; null ⇒
  // the signed-in caller reserves for themselves (007, unchanged).
  readonly onBehalfOf = input<EmployeeId | null>(null);
  // Emitted after a successful reservation so a host (the on-behalf page) can refresh; ignored by the
  // self-service route.
  readonly reserved = output<void>();

  protected readonly offices = signal<BookableOffice[] | null>(null);
  protected readonly loadFailed = signal(false);

  protected readonly selectedOfficeId = signal<string | null>(null);
  protected readonly selectedDay = signal<string | null>(null);
  protected readonly selectedRoomId = signal<RoomId | null>(null);
  protected readonly availability = signal<Map<string, RoomAvailability> | null>(null);

  protected readonly result = signal<ResultMessage | null>(null);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly bookableDays = computed(() => bookableDaysFrom(this.today()));
  protected readonly selectedOffice = computed<BookableOffice | null>(
    () => this.offices()?.find((office) => office.id === this.selectedOfficeId()) ?? null,
  );
  protected readonly officeOptions = computed<SelectOption[]>(() =>
    (this.offices() ?? []).map((office) => ({ value: office.id, label: office.name })),
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
    this.selectedRoomId.set(null);
    this.availability.set(null);
    this.loadAvailability();
  }

  protected chooseDay(day: string): void {
    this.selectedDay.set(day || null);
    this.selectedRoomId.set(null);
    this.availability.set(null);
    this.loadAvailability();
  }

  protected chooseRoom(room: BookableRoom): void {
    if (this.isFull(room)) {
      return;
    }
    this.selectedRoomId.set(room.id);
  }

  private loadAvailability(): void {
    const office = this.selectedOffice();
    const day = this.selectedDay();
    if (office === null || day === null) {
      return;
    }

    this.gateway
      .occupancyForOffice(office.id, day)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (rooms) => this.availability.set(new Map(rooms.map((room) => [room.roomId, room]))),
        // Availability is an enhancement; if it fails the rooms still show their capacity and the server
        // remains the authority on a full room at submit time.
        error: () => this.availability.set(null),
      });
  }

  protected availabilityFor(room: BookableRoom): RoomAvailability | undefined {
    return this.availability()?.get(room.id);
  }

  protected isFull(room: BookableRoom): boolean {
    return this.availabilityFor(room)?.isFull ?? false;
  }

  protected remaining(room: BookableRoom): number {
    const availability = this.availabilityFor(room);
    return availability ? availability.capacity - availability.occupied : room.capacity;
  }

  // How full the room is, 0–100, for the availability bar. Unknown availability (no day chosen yet) reads
  // as empty.
  protected occupiedPercent(room: BookableRoom): number {
    const availability = this.availabilityFor(room);
    return availability ? Math.round((availability.occupied / availability.capacity) * 100) : 0;
  }

  protected canReserve(): boolean {
    const room = this.selectedRoomId();
    return this.selectedOffice() !== null && this.selectedDay() !== null && room !== null;
  }

  protected reserve(): void {
    const office = this.selectedOffice();
    const room = this.selectedRoomId();
    const day = this.selectedDay();
    if (office === null || room === null || day === null) {
      return;
    }

    this.result.set(null);
    this.errorKey.set(null);
    const roomName = office.rooms.find((candidate) => candidate.id === room)?.name ?? '';

    this.gateway
      .reserve(office.id as OfficeId, room, day, this.onBehalfOf() ?? undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.result.set({
            key: 'attendance.reserve.reserved',
            params: { room: roomName, date: day },
          });
          this.selectedRoomId.set(null);
          this.loadAvailability();
          this.reserved.emit();
        },
        error: (error: HttpErrorResponse) => this.handleReserveError(error),
      });
  }

  private handleReserveError(error: HttpErrorResponse): void {
    const code = errorCode(error);
    switch (code) {
      case 'room_full':
        this.errorKey.set('attendance.reserve.errors.roomFull');
        this.loadAvailability();
        break;
      case 'already_reserved_today':
        this.errorKey.set('attendance.reserve.errors.alreadyReserved');
        break;
      case 'not_bookable':
        this.errorKey.set('attendance.reserve.errors.notBookable');
        break;
      case 'unknown_room':
        this.errorKey.set('attendance.reserve.errors.unknownRoom');
        this.loadCatalogue();
        break;
      case 'concurrency_retry_exhausted':
        this.errorKey.set('attendance.reserve.errors.retry');
        break;
      default:
        this.errorKey.set('attendance.reserve.errors.generic');
    }
  }
}
