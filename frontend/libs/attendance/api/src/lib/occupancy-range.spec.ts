import { addMonths, isSameMonth, monthGrid, rangeFor } from './occupancy-range';

// June 2026: the 1st is a Monday, the 30th a Tuesday; 2026-06-10 is a Wednesday.
describe('rangeFor', () => {
  it('day is a single date', () => {
    expect(rangeFor('day', '2026-06-10')).toEqual({ from: '2026-06-10', to: '2026-06-10' });
  });

  it('week is the Monday–Sunday span containing the anchor', () => {
    expect(rangeFor('week', '2026-06-10')).toEqual({ from: '2026-06-08', to: '2026-06-14' });
  });

  it('month is the first to the last day of the anchor month', () => {
    expect(rangeFor('month', '2026-06-10')).toEqual({ from: '2026-06-01', to: '2026-06-30' });
  });

  it('keeps every preset within the 31-day backend bound', () => {
    for (const preset of ['day', 'week', 'month'] as const) {
      const { from, to } = rangeFor(preset, '2026-01-15');
      const span = (Date.parse(to) - Date.parse(from)) / 86_400_000 + 1;
      expect(span).toBeLessThanOrEqual(31);
    }
  });
});

describe('monthGrid', () => {
  it('is Monday-aligned whole weeks covering the month', () => {
    const weeks = monthGrid('2026-06-10');

    expect(weeks.every((week) => week.length === 7)).toBe(true);
    expect(weeks[0][0]).toBe('2026-06-01'); // month starts on a Monday → no leading days
    expect(weeks.at(-1)?.at(-1)).toBe('2026-07-05'); // trailing days complete the final week
    expect(weeks.length).toBe(5);
  });

  it('adds leading days from the previous month when the 1st is not a Monday', () => {
    // 2026-07-01 is a Wednesday, so the grid starts on Monday 2026-06-29.
    const weeks = monthGrid('2026-07-01');
    expect(weeks[0][0]).toBe('2026-06-29');
  });
});

describe('isSameMonth', () => {
  it('is true only within the anchor month', () => {
    expect(isSameMonth('2026-06-15', '2026-06-10')).toBe(true);
    expect(isSameMonth('2026-07-01', '2026-06-10')).toBe(false);
    expect(isSameMonth('2026-05-31', '2026-06-10')).toBe(false);
  });
});

describe('addMonths', () => {
  it('moves to the first of the target month, across year boundaries', () => {
    expect(addMonths('2026-06-10', 1)).toBe('2026-07-01');
    expect(addMonths('2026-01-15', -1)).toBe('2025-12-01');
  });
});
