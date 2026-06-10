import { toBookableOffices, toMyReservation, toRoomAvailability } from './booking';
import type {
  BookableRoomResponse,
  MyReservationResponse,
  OccupancyDayResponse,
} from './generated';

describe('toBookableOffices', () => {
  it('groups the flat catalogue into offices, preserving order', () => {
    const rooms: BookableRoomResponse[] = [
      { officeId: 'o1', officeName: 'Munich', roomId: 'r1', roomName: 'A1', capacity: 8 },
      { officeId: 'o1', officeName: 'Munich', roomId: 'r2', roomName: 'B1', capacity: 5 },
      { officeId: 'o2', officeName: 'Berlin', roomId: 'r3', roomName: 'Sky', capacity: 10 },
    ];

    expect(toBookableOffices(rooms)).toEqual([
      {
        id: 'o1',
        name: 'Munich',
        rooms: [
          { id: 'r1', name: 'A1', capacity: 8 },
          { id: 'r2', name: 'B1', capacity: 5 },
        ],
      },
      { id: 'o2', name: 'Berlin', rooms: [{ id: 'r3', name: 'Sky', capacity: 10 }] },
    ]);
  });

  it('coerces a string capacity from the contract to a number', () => {
    const rooms: BookableRoomResponse[] = [
      { officeId: 'o1', officeName: 'Munich', roomId: 'r1', roomName: 'A1', capacity: '8' },
    ];

    expect(toBookableOffices(rooms)[0].rooms[0].capacity).toBe(8);
  });

  it('maps an empty catalogue to no offices', () => {
    expect(toBookableOffices([])).toEqual([]);
  });
});

describe('toRoomAvailability', () => {
  it("maps the single day's rooms to their availability", () => {
    const days: OccupancyDayResponse[] = [
      {
        date: '2026-06-08',
        office: { officeId: 'o1', name: 'Munich', occupied: 3, capacity: 13, isFull: false },
        rooms: [
          { roomId: 'r1', name: 'A1', occupied: 8, capacity: 8, isFull: true, occupants: null },
          { roomId: 'r2', name: 'B1', occupied: 1, capacity: 5, isFull: false, occupants: null },
        ],
      },
    ];

    expect(toRoomAvailability(days)).toEqual([
      { roomId: 'r1', occupied: 8, capacity: 8, isFull: true },
      { roomId: 'r2', occupied: 1, capacity: 5, isFull: false },
    ]);
  });

  it('maps an empty response to no availability', () => {
    expect(toRoomAvailability([])).toEqual([]);
  });
});

describe('toMyReservation', () => {
  it('maps the reservation DTO to the branded view model', () => {
    const dto: MyReservationResponse = {
      reservationId: 'res1',
      officeId: 'o1',
      officeName: 'Munich',
      roomId: 'r1',
      roomName: 'A1',
      date: '2026-06-08',
    };

    expect(toMyReservation(dto)).toEqual({
      id: 'res1',
      officeId: 'o1',
      officeName: 'Munich',
      roomId: 'r1',
      roomName: 'A1',
      date: '2026-06-08',
    });
  });
});
