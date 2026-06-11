import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import {
  AttendanceGateway,
  BookableOffice,
  MyReservation,
  OccupancyDay,
  addMonths,
  isSameMonth,
  monthGrid,
  rangeFor,
  todayInBerlin,
} from '@roomy/attendance-api';
import { Button, Heat, Message, Page } from '@roomy/shared-ui';
import { EMPTY, expand, reduce } from 'rxjs';

import { OccupancyScope, OfficeRoomPicker } from './office-room-picker';

// The occupancy calendar (OC-3, FR-004): a month grid where each in-month day shows its occupancy figure
// (the office rollup for the chosen scope) and the days the viewer holds a reservation are highlighted
// with a non-colour cue. Figures come from one month-range `occupancy` call; the highlight from
// `myReservations`. Month and weekday names are localised via Intl in the active language.
@Component({
  selector: 'roomy-occupancy-calendar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, Page, Message, Button, OfficeRoomPicker, Heat],
  templateUrl: './occupancy-calendar.html',
  styleUrl: './occupancy-calendar.css',
})
export class OccupancyCalendar {
  private readonly gateway = inject(AttendanceGateway);
  private readonly destroyRef = inject(DestroyRef);
  private readonly transloco = inject(TranslocoService);

  readonly today = input<string>(todayInBerlin());

  protected readonly offices = signal<BookableOffice[] | null>(null);
  protected readonly loadFailed = signal(false);
  protected readonly scope = signal<OccupancyScope | null>(null);
  protected readonly anchorMonth = signal<string | null>(null);
  protected readonly occupancyByDate = signal<Map<string, OccupancyDay>>(new Map());
  protected readonly myDays = signal<ReadonlySet<string>>(new Set());

  private readonly activeLang = toSignal(this.transloco.langChanges$, {
    initialValue: this.transloco.getActiveLang(),
  });

  protected readonly month = computed(
    () => this.anchorMonth() ?? rangeFor('month', this.today()).from,
  );
  protected readonly weeks = computed(() => monthGrid(this.month()));
  protected readonly weekdayLabels = computed(() =>
    this.weeks()[0].map((date) =>
      new Intl.DateTimeFormat(this.activeLang(), { weekday: 'short' }).format(asUtcDate(date)),
    ),
  );
  protected readonly monthLabel = computed(() =>
    new Intl.DateTimeFormat(this.activeLang(), { month: 'long', year: 'numeric' }).format(
      asUtcDate(this.month()),
    ),
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

  protected onScope(scope: OccupancyScope | null): void {
    this.scope.set(scope);
    this.load();
  }

  protected previousMonth(): void {
    this.anchorMonth.set(addMonths(this.month(), -1));
    this.load();
  }

  protected nextMonth(): void {
    this.anchorMonth.set(addMonths(this.month(), 1));
    this.load();
  }

  protected isSameMonth(date: string): boolean {
    return isSameMonth(date, this.month());
  }

  protected figureFor(date: string): OccupancyDay | undefined {
    return this.occupancyByDate().get(date);
  }

  // How full the office is on this day, 0–1, for the heat tint; null when the day has no figure (outside
  // the month or not yet loaded) so the cell stays untinted.
  protected occupancyRatio(date: string): number | null {
    const day = this.occupancyByDate().get(date);
    if (day === undefined) {
      return null;
    }
    return day.office.capacity > 0 ? day.office.occupied / day.office.capacity : 0;
  }

  protected isMine(date: string): boolean {
    return this.myDays().has(date);
  }

  private load(): void {
    const scope = this.scope();
    if (scope === null) {
      return;
    }

    const { from, to } = rangeFor('month', this.month());

    this.gateway
      .occupancy(scope, from, to)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (days) => this.occupancyByDate.set(new Map(days.map((day) => [day.date, day]))),
        error: () => this.occupancyByDate.set(new Map()),
      });

    // The calendar marks every day the viewer holds a reservation, so it walks all pages of the
    // keyset-paginated history (ADR-0044) — following nextCursor until the list is exhausted — rather
    // than only the first page (which, being date-ordered, need not include the displayed month).
    this.gateway
      .myReservations()
      .pipe(
        expand((page) => (page.nextCursor ? this.gateway.myReservations(page.nextCursor) : EMPTY)),
        reduce((all, page) => [...all, ...page.items], [] as MyReservation[]),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (reservations) =>
          this.myDays.set(new Set(reservations.map((reservation) => reservation.date))),
        error: () => this.myDays.set(new Set()),
      });
  }
}

function asUtcDate(date: string): Date {
  const [year, month, day] = date.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day));
}
