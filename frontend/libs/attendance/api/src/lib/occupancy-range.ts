import { addDays } from './bookable-day';
import { parse, weekday } from './date-parts';

// Pure, clock-free range maths: callers pass an ISO yyyy-MM-dd anchor. The presets stay within the
// backend's 31-day bound.

export type RangePreset = 'day' | 'week' | 'month';

export interface DateRange {
  readonly from: string;
  readonly to: string;
}

// Days from the most recent Monday (Monday = 0 … Sunday = 6).
function daysSinceMonday(date: string): number {
  return (weekday(date) + 6) % 7;
}

function firstOfMonth(date: string): string {
  const { year, month } = parse(date);
  return `${year}-${String(month).padStart(2, '0')}-01`;
}

function lastOfMonth(date: string): string {
  const { year, month } = parse(date);
  // Day 0 of the next month is the last day of this month.
  return new Date(Date.UTC(year, month, 0)).toISOString().slice(0, 10);
}

export function rangeFor(preset: RangePreset, anchor: string): DateRange {
  switch (preset) {
    case 'day':
      return { from: anchor, to: anchor };
    case 'week': {
      const from = addDays(anchor, -daysSinceMonday(anchor));
      return { from, to: addDays(from, 6) };
    }
    case 'month':
      return { from: firstOfMonth(anchor), to: lastOfMonth(anchor) };
  }
}

// Monday-aligned grid of whole weeks covering the anchor's month; each inner array is 7 ISO dates.
export function monthGrid(anchor: string): string[][] {
  const monthFrom = firstOfMonth(anchor);
  const monthTo = lastOfMonth(anchor);
  const gridStart = addDays(monthFrom, -daysSinceMonday(monthFrom));
  const gridEnd = addDays(monthTo, 6 - daysSinceMonday(monthTo));

  const weeks: string[][] = [];
  let cursor = gridStart;
  while (cursor <= gridEnd) {
    const week: string[] = [];
    for (let dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++) {
      week.push(cursor);
      cursor = addDays(cursor, 1);
    }
    weeks.push(week);
  }
  return weeks;
}

export function isSameMonth(date: string, anchor: string): boolean {
  return parse(date).year === parse(anchor).year && parse(date).month === parse(anchor).month;
}

export function addMonths(anchor: string, delta: number): string {
  const { year, month } = parse(anchor);
  return `${new Date(Date.UTC(year, month - 1 + delta, 1)).toISOString().slice(0, 10)}`;
}
