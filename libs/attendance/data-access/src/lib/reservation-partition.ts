import { isPastDay } from './bookable-day';
import type { MyReservation } from './booking';

// The viewer's reservations split for display: upcoming (today onward, soonest first) and past (most
// recent first) — the ordering both the self-service and on-behalf lists present. `today` is the
// Europe/Berlin calendar day the caller is anchored to; a reservation dated today counts as upcoming.
export interface PartitionedReservations {
  readonly upcoming: MyReservation[];
  readonly past: MyReservation[];
}

export function partitionReservationsByDay(
  reservations: readonly MyReservation[],
  today: string,
): PartitionedReservations {
  const upcoming = reservations
    .filter((reservation) => !isPastDay(reservation.date, today))
    .sort((left, right) => left.date.localeCompare(right.date));
  const past = reservations
    .filter((reservation) => isPastDay(reservation.date, today))
    .sort((left, right) => right.date.localeCompare(left.date));
  return { upcoming, past };
}
