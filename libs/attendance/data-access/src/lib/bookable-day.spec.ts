import {
  addDays,
  bookableDaysFrom,
  isBookable,
  isPastDay,
  isWorkingDay,
  todayInBerlin,
} from './bookable-day';

// Anchored on Monday 2026-06-08. In that week: Fri = 06-12, Sat = 06-13, Sun = 06-14, next Mon = 06-15.
// The 14-day window from 06-08 ends on Mon 06-22 (inclusive); 06-23 is the first day beyond it.
const monday = '2026-06-08';

describe('isWorkingDay', () => {
  it('is true Monday–Friday and false at the weekend', () => {
    expect(isWorkingDay(monday)).toBe(true);
    expect(isWorkingDay('2026-06-12')).toBe(true);
    expect(isWorkingDay('2026-06-13')).toBe(false);
    expect(isWorkingDay('2026-06-14')).toBe(false);
  });
});

describe('isBookable', () => {
  it('accepts today when it is a working day (spontaneous booking, AT-2)', () => {
    expect(isBookable(monday, monday)).toBe(true);
  });

  it('rejects a past day', () => {
    expect(isBookable('2026-06-05', monday)).toBe(false);
  });

  it('rejects a weekend day within the window', () => {
    expect(isBookable('2026-06-13', monday)).toBe(false);
  });

  it('accepts a working day on the last day of the window (offset 14)', () => {
    expect(isBookable('2026-06-22', monday)).toBe(true);
  });

  it('rejects a working day beyond the 14-day window (offset 15)', () => {
    expect(isBookable('2026-06-23', monday)).toBe(false);
  });
});

describe('isPastDay', () => {
  it('is true before today, false on or after today', () => {
    expect(isPastDay('2026-06-05', monday)).toBe(true);
    expect(isPastDay(monday, monday)).toBe(false);
    expect(isPastDay('2026-06-09', monday)).toBe(false);
  });
});

describe('addDays', () => {
  it('adds days across the window and month boundaries', () => {
    expect(addDays(monday, 14)).toBe('2026-06-22');
    expect(addDays('2026-06-29', 3)).toBe('2026-07-02');
  });
});

describe('bookableDaysFrom', () => {
  it('lists only the working days in the window, today first', () => {
    const days = bookableDaysFrom(monday);

    expect(days[0]).toBe(monday);
    expect(days.at(-1)).toBe('2026-06-22');
    expect(days).not.toContain('2026-06-13');
    expect(days).not.toContain('2026-06-14');
    expect(days.length).toBe(11);
  });
});

describe('todayInBerlin', () => {
  it('returns the Europe/Berlin calendar day, which can differ from UTC late in the day', () => {
    // 23:30 UTC on 06-08 is already 01:30 on 06-09 in Berlin (UTC+2 in summer).
    expect(todayInBerlin(new Date('2026-06-08T23:30:00Z'))).toBe('2026-06-09');
  });
});
