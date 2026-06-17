// Pure, clock-free day rules: every function takes ISO yyyy-MM-dd dates so it stays testable; the one
// timezone-aware step is isolated in todayInBerlin. A day is bookable if it is a working day within
// today .. today + BOOKING_WINDOW_DAYS (inclusive) — the same rule the server enforces.

import { parse, weekday } from './date-parts';

export const BOOKING_WINDOW_DAYS = 14;

// Days since the Unix epoch, computed in UTC so it never shifts with the host timezone.
function epochDay(date: string): number {
  const { year, month, day } = parse(date);
  return Math.floor(Date.UTC(year, month - 1, day) / 86_400_000);
}

function format(value: Date): string {
  const year = value.getUTCFullYear();
  const month = String(value.getUTCMonth() + 1).padStart(2, '0');
  const day = String(value.getUTCDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function addDays(date: string, days: number): string {
  const { year, month, day } = parse(date);
  return format(new Date(Date.UTC(year, month - 1, day + days)));
}

export function isWorkingDay(date: string): boolean {
  const weekdayIndex = weekday(date);
  return weekdayIndex >= 1 && weekdayIndex <= 5;
}

export function isPastDay(date: string, today: string): boolean {
  return epochDay(date) < epochDay(today);
}

export function isBookable(date: string, today: string): boolean {
  const offset = epochDay(date) - epochDay(today);
  return offset >= 0 && offset <= BOOKING_WINDOW_DAYS && isWorkingDay(date);
}

export function bookableDaysFrom(today: string): string[] {
  const days: string[] = [];
  for (let offset = 0; offset <= BOOKING_WINDOW_DAYS; offset++) {
    const date = addDays(today, offset);
    if (isWorkingDay(date)) {
      days.push(date);
    }
  }
  return days;
}

// The current Europe/Berlin calendar day as ISO yyyy-MM-dd. `en-CA` formats as yyyy-MM-dd; `now` is
// injectable so callers can test against a fixed clock.
export function todayInBerlin(now: Date = new Date()): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Berlin' }).format(now);
}
