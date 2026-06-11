import type { Brand } from '@roomy/util';

import type { OfficeResponse, RoomResponse } from './generated';

// Backend uuids are trusted, so these mint the brand without re-validating.
export type OfficeId = Brand<string, 'OfficeId'>;
export const officeId = (value: string): OfficeId => value as OfficeId;

export type RoomId = Brand<string, 'RoomId'>;
export const roomId = (value: string): RoomId => value as RoomId;

export interface Room {
  readonly id: RoomId;
  readonly name: string;
  readonly capacity: number;
}

// capacity is the derived sum of the rooms' capacities, never set directly.
export interface Office {
  readonly id: OfficeId;
  readonly name: string;
  readonly location: string;
  readonly capacity: number;
  readonly rooms: readonly Room[];
}

// capacity is coerced from the contract's number | string (OpenAPI integer widening) — a narrowing the types can't express.
export function toOffice(response: OfficeResponse): Office {
  return {
    id: officeId(response.id),
    name: response.name,
    location: response.location,
    capacity: Number(response.capacity),
    rooms: response.rooms.map(toRoom),
  };
}

export function toRoom(response: RoomResponse): Room {
  return {
    id: roomId(response.id),
    name: response.name,
    capacity: Number(response.capacity),
  };
}
