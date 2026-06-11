import { officeId, roomId } from './booking';
import type { OfficeId, RoomId } from './booking';
import type { OccupancyDayResponse } from './generated';

export interface Occupant {
  readonly employeeId: string;
  readonly name: string;
}

// occupants is present only for today/tomorrow; otherwise undefined (the server withholds names on other days).
export interface OccupancyRoom {
  readonly roomId: RoomId;
  readonly name: string;
  readonly occupied: number;
  readonly capacity: number;
  readonly isFull: boolean;
  readonly occupants?: readonly Occupant[];
}

export interface OccupancyOffice {
  readonly officeId: OfficeId;
  readonly name: string;
  readonly occupied: number;
  readonly capacity: number;
  readonly isFull: boolean;
}

// date is the ISO calendar day (yyyy-MM-dd).
export interface OccupancyDay {
  readonly date: string;
  readonly office: OccupancyOffice;
  readonly rooms: readonly OccupancyRoom[];
}

// occupied/capacity are coerced from the contract's number | string; occupants is undefined when the response withholds it.
export function toOccupancyDays(days: readonly OccupancyDayResponse[]): OccupancyDay[] {
  return days.map((day) => ({
    date: day.date,
    office: {
      officeId: officeId(day.office.officeId),
      name: day.office.name,
      occupied: Number(day.office.occupied),
      capacity: Number(day.office.capacity),
      isFull: day.office.isFull,
    },
    rooms: day.rooms.map((room) => ({
      roomId: roomId(room.roomId),
      name: room.name,
      occupied: Number(room.occupied),
      capacity: Number(room.capacity),
      isFull: room.isFull,
      occupants:
        room.occupants == null
          ? undefined
          : room.occupants.map((occupant) => ({
              employeeId: occupant.employeeId,
              name: occupant.name,
            })),
    })),
  }));
}
