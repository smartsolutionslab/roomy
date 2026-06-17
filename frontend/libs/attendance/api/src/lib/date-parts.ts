// Pure, clock-free parsing of ISO yyyy-MM-dd dates. Everything is computed in UTC so it never shifts
// with the host timezone.

export interface DateParts {
  readonly year: number;
  readonly month: number;
  readonly day: number;
}

export function parse(date: string): DateParts {
  const [year, month, day] = date.split('-').map(Number);
  return { year, month, day };
}

// The date at UTC midnight; safe for Intl formatting and weekday maths regardless of host timezone.
export function asUtcDate(date: string): Date {
  const { year, month, day } = parse(date);
  return new Date(Date.UTC(year, month - 1, day));
}

// 0 = Sunday … 6 = Saturday, computed in UTC (timezone-free).
export function weekday(date: string): number {
  return asUtcDate(date).getUTCDay();
}
