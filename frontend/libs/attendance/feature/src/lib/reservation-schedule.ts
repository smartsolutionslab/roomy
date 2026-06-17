import { Signal, computed } from '@angular/core';
import { MyReservation, partitionReservationsByDay } from '@roomy/attendance-api';

export interface ReservationSchedule {
  readonly upcoming: Signal<readonly MyReservation[]>;
  readonly past: Signal<readonly MyReservation[]>;
}

// The upcoming/past split shared by the reservation-list pages: derives from an accumulated items signal
// against the company-local today.
export function reservationSchedule(
  items: Signal<readonly MyReservation[] | null>,
  today: Signal<string>,
): ReservationSchedule {
  const schedule = computed(() => partitionReservationsByDay(items() ?? [], today()));
  return {
    upcoming: computed(() => schedule().upcoming),
    past: computed(() => schedule().past),
  };
}
