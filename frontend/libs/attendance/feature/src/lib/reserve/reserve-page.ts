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
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
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
import {
  Button,
  DaySelect,
  type DayOption,
  Message,
  Page,
  TileGroup,
  type SelectOption,
} from '@roomy/shared-ui';

import { bookableOfficesCatalogue } from '../bookable-offices-catalogue';

import { RoomCell } from './room-cell';

// The reserve flow (AT-1/AT-2): pick an office, then a day, then a room with a remaining place, then
// confirm. The catalogue and the per-day availability come from the attendance context only; the day
// chooser offers only bookable days (FR-003), so a past/weekend/out-of-window day cannot be picked, and
// a full room is shown unbookable before submit (FR-002). `today` is an input defaulting to the
// Europe/Berlin calendar day, so the day rules are deterministic under test.
@Component({
  selector: 'roomy-reserve-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, Page, Message, Button, DaySelect, TileGroup, RoomCell],
  templateUrl: './reserve-page.html',
  styleUrl: './reserve-page.css',
})
export class ReservePage {
  private readonly gateway = inject(AttendanceGateway);
  private readonly destroyRef = inject(DestroyRef);
  private readonly transloco = inject(TranslocoService);
  private readonly activeLang = toSignal(this.transloco.langChanges$, {
    initialValue: this.transloco.getActiveLang(),
  });

  readonly today = input<string>(todayInBerlin());
  // When set (administrator on-behalf, 009 AT-6), the reservation is created for that employee; null ⇒
  // the signed-in caller reserves for themselves (007, unchanged).
  readonly onBehalfOf = input<EmployeeId | null>(null);
  // Emitted after a successful reservation so a host (the on-behalf page) can refresh; ignored by the
  // self-service route.
  readonly reserved = output<void>();

  private readonly catalogue = bookableOfficesCatalogue();
  protected readonly offices = this.catalogue.offices;
  protected readonly loadFailed = this.catalogue.loadFailed;

  protected readonly selectedOfficeId = signal<string | null>(null);
  protected readonly selectedDay = signal<string | null>(null);
  protected readonly selectedRoomId = signal<RoomId | null>(null);
  protected readonly availability = signal<Map<string, RoomAvailability> | null>(null);

  protected readonly result = signal<ResultMessage | null>(null);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly bookableDays = computed(() => bookableDaysFrom(this.today()));
  // Each bookable day as a tile: the weekday over the date, localised in the active language. Formatted
  // in UTC so the ISO day is not shifted by the runtime's timezone.
  protected readonly dayOptions = computed<DayOption[]>(() => {
    const language = this.activeLang();
    const weekday = new Intl.DateTimeFormat(language, { weekday: 'short', timeZone: 'UTC' });
    const date = new Intl.DateTimeFormat(language, {
      day: 'numeric',
      month: 'short',
      timeZone: 'UTC',
    });
    return this.bookableDays().map((day) => {
      const instant = new Date(`${day}T00:00:00Z`);
      return { value: day, weekday: weekday.format(instant), date: date.format(instant) };
    });
  });
  protected readonly selectedOffice = computed<BookableOffice | null>(
    () => this.offices()?.find((office) => office.id === this.selectedOfficeId()) ?? null,
  );
  protected readonly officeOptions = computed<SelectOption[]>(() =>
    (this.offices() ?? []).map((office) => ({ value: office.id, label: office.name })),
  );

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
        this.catalogue.reload();
        break;
      case 'concurrency_retry_exhausted':
        this.errorKey.set('attendance.reserve.errors.retry');
        break;
      default:
        this.errorKey.set('attendance.reserve.errors.generic');
    }
  }
}
