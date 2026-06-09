// The bookable-day rules (FR-003), kept clock-free: every function takes the dates it needs as ISO
// yyyy-MM-dd strings so it is pure and testable. A day is bookable only if it is a working day (Mon–Fri)
// within today through today + 14 calendar days (inclusive) — the same rule the server enforces (003
// FR-002). The page supplies "today" from `todayInBerlin`, so the only timezone-aware step is isolated.

export const BOOKING_WINDOW_DAYS = 14;

interface DateParts {
  readonly year: number;
  readonly month: number;
  readonly day: number;
}

function parse(date: string): DateParts {
  const [year, month, day] = date.split('-').map(Number);
  return { year, month, day };
}

// Days since the Unix epoch, computed in UTC so it never shifts with the host timezone.
function epochDay(date: string): number {
  const { year, month, day } = parse(date);
  return Math.floor(Date.UTC(year, month - 1, day) / 86_400_000);
}

// 0 = Sunday … 6 = Saturday, computed in UTC (timezone-free).
function weekday(date: string): number {
  const { year, month, day } = parse(date);
  return new Date(Date.UTC(year, month - 1, day)).getUTCDay();
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

// The selectable days for the day chooser: every working day in the window, today first (FR-003).
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
