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
  Employee,
  MyReservation,
  errorCode,
  partitionReservationsByDay,
  todayInBerlin,
} from '@roomy/attendance-api';
import { cursorList, type ResultMessage } from '@roomy/shared-data-access';
import { Combobox, InfiniteScroll, Message, Page, type SelectOption } from '@roomy/shared-ui';
import { EMPTY, Subject, debounceTime, distinctUntilChanged, map } from 'rxjs';

import { ReservationItem } from '../reservation-item/reservation-item';
import { ReservePage } from '../reserve/reserve-page';

// The administrator on-behalf page (009, AT-6): pick an employee, then reserve for them (the embedded
// 007 reserve flow with `onBehalfOf` set) and view/cancel their reservations. Admin-gated by the route
// (adminGuard); the server also enforces admin on the directory/by-employee reads and on-behalf reserve.
// `today` is an input defaulting to the Europe/Berlin day so the upcoming/past split is deterministic.
@Component({
  selector: 'roomy-on-behalf-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TranslocoDirective,
    ReservePage,
    ReservationItem,
    InfiniteScroll,
    Page,
    Message,
    Combobox,
  ],
  templateUrl: './on-behalf-page.html',
  styleUrl: './on-behalf-page.css',
})
export class OnBehalfPage {
  private readonly gateway = inject(AttendanceGateway);
  private readonly destroyRef = inject(DestroyRef);

  readonly today = input<string>(todayInBerlin());

  // The debounced name search over the on-behalf directory (012, FR-009). Blank means "no filter".
  private readonly searchInput = new Subject<string>();
  protected readonly query = signal('');

  // The endless employee directory (ADR-0044/0049) feeding the picker. The fetch reads the current
  // search query, so reset() re-runs it ranked by name similarity whenever the query changes.
  protected readonly employeesList = cursorList<Employee>((cursor) =>
    this.gateway.listEmployees(this.query(), cursor),
  );

  protected readonly selectedEmployeeId = signal<string | null>(null);
  protected readonly result = signal<ResultMessage | null>(null);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly selectedEmployee = computed<Employee | null>(
    () =>
      this.employeesList.items()?.find((employee) => employee.id === this.selectedEmployeeId()) ??
      null,
  );
  protected readonly employeeOptions = computed<SelectOption[]>(() =>
    (this.employeesList.items() ?? []).map((employee) => ({
      value: employee.id,
      label: employee.name,
    })),
  );

  // The chosen employee's reservations (ADR-0044/0049): deferred until a pick, then reloaded from the
  // first page whenever the selection changes (chooseEmployee) or a booking is made (onReserved). The
  // fetch reads the current selection, so reset() re-runs it for the newly picked employee.
  protected readonly reservationsList = cursorList<MyReservation>(
    (cursor) => {
      // Only ever driven (reset/loadMore) while an employee is selected — chooseEmployee clears the list
      // instead of loading when the selection is empty — so the null branch is unreachable.
      const employee = this.selectedEmployee();
      return employee === null ? EMPTY : this.gateway.reservationsFor(employee.id, cursor);
    },
    { autoLoad: false },
  );

  private readonly schedule = computed(() =>
    partitionReservationsByDay(this.reservationsList.items() ?? [], this.today()),
  );
  protected readonly upcoming = computed(() => this.schedule().upcoming);
  protected readonly past = computed(() => this.schedule().past);

  constructor() {
    this.searchInput
      .pipe(
        debounceTime(250),
        map((value) => value.trim()),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((query) => this.applySearch(query));
  }

  protected onSearch(query: string): void {
    this.searchInput.next(query);
  }

  // A new query reloads the directory from the first page (ranked by similarity) and drops any pick the
  // narrowed results may no longer contain, so the page never shows a reserve flow for a hidden employee.
  private applySearch(query: string): void {
    this.query.set(query);
    this.selectedEmployeeId.set(null);
    this.reservationsList.clear();
    this.result.set(null);
    this.errorKey.set(null);
    this.employeesList.reset();
  }

  protected chooseEmployee(employeeValue: string): void {
    this.selectedEmployeeId.set(employeeValue || null);
    this.result.set(null);
    this.errorKey.set(null);
    if (this.selectedEmployee() === null) {
      this.reservationsList.clear();
    } else {
      this.reservationsList.reset();
    }
  }

  // The embedded reserve flow announces its own success; reload the employee's reservations from the
  // first page so the new booking appears.
  protected onReserved(): void {
    this.reservationsList.reset();
  }

  protected cancel(reservation: MyReservation): void {
    this.result.set(null);
    this.errorKey.set(null);

    this.gateway
      .cancel(reservation.id, reservation.date)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.reservationsList.update((items) =>
            items.filter((candidate) => candidate.id !== reservation.id),
          );
          this.result.set({ key: 'attendance.onBehalf.cancelled' });
        },
        error: (error: HttpErrorResponse) => {
          this.errorKey.set(
            errorCode(error) === 'past_immutable'
              ? 'attendance.onBehalf.errors.pastImmutable'
              : 'attendance.onBehalf.errors.generic',
          );
        },
      });
  }
}
