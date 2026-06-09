import { officeId, roomId } from './booking';
import type { OfficeId, RoomId } from './booking';
import type { OccupancyDayResponse } from './generated';

// One employee booked in a room on a day. Present only for today and the following day — the server
// applies the data-minimisation policy (004 FR-007), and the client never infers names the response
// withholds (008 FR-003).
export interface Occupant {
  readonly employeeId: string;
  readonly name: string;
}

// A room's occupancy figure for a day: occupied of capacity, full when occupied >= capacity. `occupants`
// is present only when the response carried it (today/tomorrow); otherwise undefined (counts only).
export interface OccupancyRoom {
  readonly roomId: RoomId;
  readonly name: string;
  readonly occupied: number;
  readonly capacity: number;
  readonly isFull: boolean;
  readonly occupants?: readonly Occupant[];
}

// The office rollup for a day: the sum of its rooms' occupied and capacity (OC-2).
export interface OccupancyOffice {
  readonly officeId: OfficeId;
  readonly name: string;
  readonly occupied: number;
  readonly capacity: number;
  readonly isFull: boolean;
}

// One day's occupancy for a scope: the office rollup and its rooms. A row in the list / a cell in the
// calendar. `date` is the Europe/Berlin calendar day (ISO yyyy-MM-dd).
export interface OccupancyDay {
  readonly date: string;
  readonly office: OccupancyOffice;
  readonly rooms: readonly OccupancyRoom[];
}

// Maps the trusted generated DTOs to branded view models at the data-access boundary (ADR-0020).
// Capacity/occupied are coerced from the contract's `number | string`. `occupants` maps to undefined
// when the response withholds it (null) — names are never inferred (008 FR-003).
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
          : room.occupants.map((occupant) => ({ employeeId: occupant.employeeId, name: occupant.name })),
    })),
  }));
}
