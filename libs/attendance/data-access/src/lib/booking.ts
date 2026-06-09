import type { Brand } from '@roomy/util';

import type {
  BookableRoomResponse,
  MyReservationResponse,
  OccupancyDayResponse,
} from './generated';

// Branded identifiers so a bare string cannot be passed where an office/room/reservation identifier is
// expected. Backend DTOs are trusted (ADR-0020) and the contract types these as uuids, so the value is
// not re-validated here — the helpers only mint the brand at the data-access boundary.
export type OfficeId = Brand<string, 'OfficeId'>;
export const officeId = (value: string): OfficeId => value as OfficeId;

export type RoomId = Brand<string, 'RoomId'>;
export const roomId = (value: string): RoomId => value as RoomId;

export type ReservationId = Brand<string, 'ReservationId'>;
export const reservationId = (value: string): ReservationId => value as ReservationId;

// A bookable room and its fixed capacity. The second step of the reserve picker (AT-1).
export interface BookableRoom {
  readonly id: RoomId;
  readonly name: string;
  readonly capacity: number;
}

// An office and its bookable rooms — the first step of the reserve picker (AT-1).
export interface BookableOffice {
  readonly id: OfficeId;
  readonly name: string;
  readonly rooms: readonly BookableRoom[];
}

// A room's availability on the chosen day, used to show remaining places and grey out a full room before
// submitting (AT-3, FR-002). Derived from the occupancy read side for a single day.
export interface RoomAvailability {
  readonly roomId: RoomId;
  readonly occupied: number;
  readonly capacity: number;
  readonly isFull: boolean;
}

// One of the signed-in employee's own reservations (AT-4). `date` is the Europe/Berlin calendar day
// (ISO yyyy-MM-dd); whether it is upcoming (cancellable) or past is decided against "today" by the
// bookable-day helpers, so this view model stays clock-free.
export interface MyReservation {
  readonly id: ReservationId;
  readonly officeId: OfficeId;
  readonly officeName: string;
  readonly roomId: RoomId;
  readonly roomName: string;
  readonly date: string;
}

// Groups the flat /rooms catalogue into offices at the data-access boundary (ADR-0020). Every row carries
// its office, so offices are built in first-seen order and rooms appended in order. Capacity is coerced
// from the contract's `number | string` (OpenAPI integer widening) to a number — the one narrowing the
// contract cannot express.
export function toBookableOffices(rooms: readonly BookableRoomResponse[]): BookableOffice[] {
  const offices = new Map<string, { id: OfficeId; name: string; rooms: BookableRoom[] }>();
  for (const room of rooms) {
    let office = offices.get(room.officeId);
    if (office === undefined) {
      office = { id: officeId(room.officeId), name: room.officeName, rooms: [] };
      offices.set(room.officeId, office);
    }
    office.rooms.push({ id: roomId(room.roomId), name: room.roomName, capacity: Number(room.capacity) });
  }
  return [...offices.values()];
}

// Maps a single occupancy day's rooms to their availability for the room step. An empty list (the day
// is not in the response) yields no availability.
export function toRoomAvailability(days: readonly OccupancyDayResponse[]): RoomAvailability[] {
  const day = days[0];
  if (day === undefined) {
    return [];
  }
  return day.rooms.map((room) => ({
    roomId: roomId(room.roomId),
    occupied: Number(room.occupied),
    capacity: Number(room.capacity),
    isFull: room.isFull,
  }));
}

export function toMyReservation(response: MyReservationResponse): MyReservation {
  return {
    id: reservationId(response.reservationId),
    officeId: officeId(response.officeId),
    officeName: response.officeName,
    roomId: roomId(response.roomId),
    roomName: response.roomName,
    date: response.date,
  };
}
