import type { Brand } from '@roomy/util';

import type {
  BookableRoomResponse,
  MyReservationResponse,
  OccupancyDayResponse,
} from './generated';

// Backend uuids are trusted, so these mint the brand without re-validating.
export type OfficeId = Brand<string, 'OfficeId'>;
export const officeId = (value: string): OfficeId => value as OfficeId;

export type RoomId = Brand<string, 'RoomId'>;
export const roomId = (value: string): RoomId => value as RoomId;

export type ReservationId = Brand<string, 'ReservationId'>;
export const reservationId = (value: string): ReservationId => value as ReservationId;

export type EmployeeId = Brand<string, 'EmployeeId'>;
export const employeeId = (value: string): EmployeeId => value as EmployeeId;

export interface BookableRoom {
  readonly id: RoomId;
  readonly name: string;
  readonly capacity: number;
}

export interface BookableOffice {
  readonly id: OfficeId;
  readonly name: string;
  readonly rooms: readonly BookableRoom[];
}

export interface RoomAvailability {
  readonly roomId: RoomId;
  readonly occupied: number;
  readonly capacity: number;
  readonly isFull: boolean;
}

// date is the ISO calendar day (yyyy-MM-dd); past-vs-upcoming is decided elsewhere against "today".
export interface MyReservation {
  readonly id: ReservationId;
  readonly officeId: OfficeId;
  readonly officeName: string;
  readonly roomId: RoomId;
  readonly roomName: string;
  readonly date: string;
}

// Capacity is coerced from the contract's number | string (OpenAPI integer widening) — a narrowing the types can't express.
export function toBookableOffices(rooms: readonly BookableRoomResponse[]): BookableOffice[] {
  const offices = new Map<string, { id: OfficeId; name: string; rooms: BookableRoom[] }>();
  for (const room of rooms) {
    let office = offices.get(room.officeId);
    if (office === undefined) {
      office = {
        id: officeId(room.officeId),
        name: room.officeName,
        rooms: [],
      };
      offices.set(room.officeId, office);
    }
    office.rooms.push({
      id: roomId(room.roomId),
      name: room.roomName,
      capacity: Number(room.capacity),
    });
  }
  return [...offices.values()];
}

export function toRoomAvailability(days: readonly OccupancyDayResponse[]): RoomAvailability[] {
  const day = days[0];
  if (day === undefined) return [];

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
