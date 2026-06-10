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
import { Button, FormField, InfiniteScroll, Message, Page } from '@roomy/shared-ui';

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

  protected readonly employees = signal<Employee[] | null>(null);
  protected readonly employeesCursor = signal<string | null>(null);
  protected readonly loadingEmployees = signal(false);
  protected readonly loadFailed = signal(false);
  protected readonly selectedEmployeeId = signal<string | null>(null);
  protected readonly reservations = signal<MyReservation[] | null>(null);
  protected readonly reservationsCursor = signal<string | null>(null);
  protected readonly loadingReservations = signal(false);
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
    this.loadMoreEmployees();
  }

  // Appends the next page of the employee directory (ADR-0044) — the picker grows as the administrator
  // scrolls or activates "Load more". nextCursor === null marks the end of the directory.
  protected loadMoreEmployees(): void {
    if (this.loadingEmployees()) {
      return;
    }

    this.loadingEmployees.set(true);
    this.gateway
      .listEmployees(this.employeesCursor() ?? undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          this.employees.update((current) => [...(current ?? []), ...page.items]);
          this.employeesCursor.set(page.nextCursor);
          this.loadingEmployees.set(false);
        },
        error: () => {
          this.loadFailed.set(true);
          this.employees.update((current) => current ?? []);
          this.loadingEmployees.set(false);
        },
      });
  }

  protected chooseEmployee(employeeValue: string): void {
    this.selectedEmployeeId.set(employeeValue || null);
    this.result.set(null);
    this.errorKey.set(null);
    this.reservations.set(null);
    this.reservationsCursor.set(null);
    this.loadMoreReservations();
  }

  // Appends the next page of the chosen employee's reservations (ADR-0044); the upcoming/past split
  // derives from the accumulated set. No selection ⇒ nothing to load.
  protected loadMoreReservations(): void {
    const employee = this.selectedEmployee();
    if (employee === null || this.loadingReservations()) {
      return;
    }

    this.loadingReservations.set(true);
    this.gateway
      .reservationsFor(employee.id, this.reservationsCursor() ?? undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          this.reservations.update((current) => [...(current ?? []), ...page.items]);
          this.reservationsCursor.set(page.nextCursor);
          this.loadingReservations.set(false);
        },
        error: () => {
          this.reservations.update((current) => current ?? []);
          this.loadingReservations.set(false);
        },
      });
  }

  // The embedded reserve flow announces its own success; here we reload the employee's reservations
  // from the first page so the new booking appears.
  protected onReserved(): void {
    this.reservations.set(null);
    this.reservationsCursor.set(null);
    this.loadMoreReservations();
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
