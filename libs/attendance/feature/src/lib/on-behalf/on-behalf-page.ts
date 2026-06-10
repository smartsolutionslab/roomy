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
  isPastDay,
  todayInBerlin,
} from '@roomy/attendance-data-access';
import { cursorList } from '@roomy/shared-data-access';
import { Button, FormField, InfiniteScroll, Message, Page } from '@roomy/shared-ui';
import { EMPTY } from 'rxjs';

import { ReservePage } from '../reserve/reserve-page';

type ResultMessage = { key: string; params?: Record<string, unknown> };

// The administrator on-behalf page (009, AT-6): pick an employee, then reserve for them (the embedded
// 007 reserve flow with `onBehalfOf` set) and view/cancel their reservations. Admin-gated by the route
// (adminGuard); the server also enforces admin on the directory/by-employee reads and on-behalf reserve.
// `today` is an input defaulting to the Europe/Berlin day so the upcoming/past split is deterministic.
@Component({
  selector: 'roomy-on-behalf-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, ReservePage, InfiniteScroll, Page, FormField, Message, Button],
  templateUrl: './on-behalf-page.html',
  styleUrl: './on-behalf-page.css',
})
export class OnBehalfPage {
  private readonly gateway = inject(AttendanceGateway);
  private readonly destroyRef = inject(DestroyRef);

  readonly today = input<string>(todayInBerlin());

  // The endless employee directory (ADR-0044/0049) feeding the picker.
  protected readonly employeesList = cursorList<Employee>((cursor) =>
    this.gateway.listEmployees(cursor),
  );

  protected readonly selectedEmployeeId = signal<string | null>(null);
  protected readonly result = signal<ResultMessage | null>(null);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly selectedEmployee = computed<Employee | null>(
    () =>
      this.employeesList.items()?.find((employee) => employee.id === this.selectedEmployeeId()) ??
      null,
  );

  // The chosen employee's reservations (ADR-0044/0049): deferred until a pick, then reloaded from the
  // first page whenever the selection changes (chooseEmployee) or a booking is made (onReserved). The
  // fetch reads the current selection, so reset() re-runs it for the newly picked employee.
  protected readonly reservationsList = cursorList<MyReservation>((cursor) => {
    // Only ever driven (reset/loadMore) while an employee is selected — chooseEmployee clears the list
    // instead of loading when the selection is empty — so the null branch is unreachable.
    const employee = this.selectedEmployee();
    return employee === null ? EMPTY : this.gateway.reservationsFor(employee.id, cursor);
  }, { autoLoad: false });

  protected readonly upcoming = computed(() =>
    (this.reservationsList.items() ?? [])
      .filter((reservation) => !isPastDay(reservation.date, this.today()))
      .sort((left, right) => left.date.localeCompare(right.date)),
  );
  protected readonly past = computed(() =>
    (this.reservationsList.items() ?? [])
      .filter((reservation) => isPastDay(reservation.date, this.today()))
      .sort((left, right) => right.date.localeCompare(left.date)),
  );

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
          const code = (error.error as { code?: string } | null)?.code;
          this.errorKey.set(
            code === 'past_immutable'
              ? 'attendance.onBehalf.errors.pastImmutable'
              : 'attendance.onBehalf.errors.generic',
          );
        },
      });
  }
}
