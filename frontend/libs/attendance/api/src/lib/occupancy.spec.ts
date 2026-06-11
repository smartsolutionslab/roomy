import type { OccupancyDayResponse } from './generated';
import { toOccupancyDays } from './occupancy';

const today: OccupancyDayResponse = {
  date: '2026-06-08',
  office: { officeId: 'o1', name: 'Munich', occupied: 3, capacity: 13, isFull: false },
  rooms: [
    {
      roomId: 'r1',
      name: 'A1',
      occupied: 8,
      capacity: 8,
      isFull: true,
      occupants: [{ employeeId: 'e1', name: 'Ada' }],
    },
    { roomId: 'r2', name: 'B1', occupied: 1, capacity: 5, isFull: false, occupants: [] },
  ],
};

const futureDay: OccupancyDayResponse = {
  date: '2026-06-15',
  office: { officeId: 'o1', name: 'Munich', occupied: 2, capacity: 13, isFull: false },
  rooms: [{ roomId: 'r1', name: 'A1', occupied: 2, capacity: 8, isFull: false, occupants: null }],
};

describe('toOccupancyDays', () => {
  it('maps the office rollup and per-room figures', () => {
    const [day] = toOccupancyDays([today]);

    expect(day.date).toBe('2026-06-08');
    expect(day.office).toEqual({
      officeId: 'o1',
      name: 'Munich',
      occupied: 3,
      capacity: 13,
      isFull: false,
    });
    expect(day.rooms[0]).toMatchObject({ roomId: 'r1', occupied: 8, capacity: 8, isFull: true });
  });

  it('keeps occupant names when the response carries them (today/tomorrow)', () => {
    const [day] = toOccupancyDays([today]);

    expect(day.rooms[0].occupants).toEqual([{ employeeId: 'e1', name: 'Ada' }]);
    expect(day.rooms[1].occupants).toEqual([]);
  });

  it('leaves occupants undefined when the response withholds them (never inferred)', () => {
    const [day] = toOccupancyDays([futureDay]);

    expect(day.rooms[0].occupants).toBeUndefined();
  });

  it('coerces a string occupied/capacity from the contract to a number', () => {
    const [day] = toOccupancyDays([
      {
        date: '2026-06-08',
        office: { officeId: 'o1', name: 'Munich', occupied: '3', capacity: '13', isFull: false },
        rooms: [
          { roomId: 'r1', name: 'A1', occupied: '8', capacity: '8', isFull: true, occupants: null },
        ],
      },
    ]);

    expect(day.office.occupied).toBe(3);
    expect(day.rooms[0].capacity).toBe(8);
  });
});
