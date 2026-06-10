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

import { ReservePage } from '../reserve/reserve-page';

type ResultMessage = { key: string; params?: Record<string, unknown> };

// The administrator on-behalf page (009, AT-6): pick an employee, then reserve for them (the embedded
// 007 reserve flow with `onBehalfOf` set) and view/cancel their reservations. Admin-gated by the route
// (adminGuard); the server also enforces admin on the directory/by-employee reads and on-behalf reserve.
// `today` is an input defaulting to the Europe/Berlin day so the upcoming/past split is deterministic.
@Component({
  selector: 'roomy-on-behalf-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, ReservePage],
  templateUrl: './on-behalf-page.html',
  styleUrl: './on-behalf-page.css',
})
export class OnBehalfPage {
  private readonly gateway = inject(AttendanceGateway);
  private readonly destroyRef = inject(DestroyRef);

  readonly today = input<string>(todayInBerlin());

  protected readonly employees = signal<Employee[] | null>(null);
  protected readonly loadFailed = signal(false);
  protected readonly selectedEmployeeId = signal<string | null>(null);
  protected readonly reservations = signal<MyReservation[] | null>(null);
  protected readonly result = signal<ResultMessage | null>(null);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly selectedEmployee = computed<Employee | null>(
    () => this.employees()?.find((employee) => employee.id === this.selectedEmployeeId()) ?? null,
  );
  protected readonly upcoming = computed(() =>
    (this.reservations() ?? [])
      .filter((reservation) => !isPastDay(reservation.date, this.today()))
      .sort((left, right) => left.date.localeCompare(right.date)),
  );
  protected readonly past = computed(() =>
    (this.reservations() ?? [])
      .filter((reservation) => isPastDay(reservation.date, this.today()))
      .sort((left, right) => right.date.localeCompare(left.date)),
  );

  constructor() {
    this.gateway
      .listEmployees()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (employees) => this.employees.set(employees),
        error: () => {
          this.loadFailed.set(true);
          this.employees.set([]);
        },
      });
  }

  protected chooseEmployee(employeeValue: string): void {
    this.selectedEmployeeId.set(employeeValue || null);
    this.result.set(null);
    this.errorKey.set(null);
    this.loadReservations();
  }

  private loadReservations(): void {
    const employee = this.selectedEmployee();
    if (employee === null) {
      this.reservations.set(null);
      return;
    }

    this.gateway
      .reservationsFor(employee.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (reservations) => this.reservations.set(reservations),
        error: () => this.reservations.set([]),
      });
  }

  // The embedded reserve flow announces its own success; here we just refresh the employee's list.
  protected onReserved(): void {
    this.loadReservations();
  }

  protected cancel(reservation: MyReservation): void {
    this.result.set(null);
    this.errorKey.set(null);

    this.gateway
      .cancel(reservation.id, reservation.date)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.reservations.update((items) =>
            (items ?? []).filter((candidate) => candidate.id !== reservation.id),
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
