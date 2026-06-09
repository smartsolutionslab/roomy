import type { Brand } from '@roomy/util';

import type { OfficeResponse, RoomResponse } from './generated';

// Branded identifiers so a bare string cannot be passed where an office/room identifier is expected.
// Backend DTOs are trusted (ADR-0020) and the contract types these as uuids, so the value is not
// re-validated here — the helpers only mint the brand at the data-access boundary.
export type OfficeId = Brand<string, 'OfficeId'>;
export const officeId = (value: string): OfficeId => value as OfficeId;

export type RoomId = Brand<string, 'RoomId'>;
export const roomId = (value: string): RoomId => value as RoomId;

// A room with its fixed capacity (OR-3). Capacity is the number of places available on a day.
export interface Room {
  readonly id: RoomId;
  readonly name: string;
  readonly capacity: number;
}

// An office and its rooms. `capacity` is the derived sum of the rooms' capacities (FR-008), never set
// directly on the office.
export interface Office {
  readonly id: OfficeId;
  readonly name: string;
  readonly location: string;
  readonly capacity: number;
  readonly rooms: readonly Room[];
}

// Maps the trusted generated DTO to the branded view model at the data-access boundary (ADR-0020).
// The contract types capacity as `number | string` (OpenAPI integer widening), so it is coerced to a
// number here — the one place the narrowing the contract cannot express is enforced.
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
