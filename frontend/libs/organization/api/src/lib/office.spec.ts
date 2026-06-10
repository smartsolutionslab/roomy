import type { OfficeResponse } from './generated';
import { toOffice } from './office';

describe('toOffice', () => {
  it('maps the office DTO to the branded view model', () => {
    const dto: OfficeResponse = {
      id: '0199a0b0-0000-7000-8000-000000000010',
      name: 'Berlin',
      location: 'Berlin, DE',
      capacity: 12,
      rooms: [
        { id: '0199a0b0-0000-7000-8000-000000000020', name: 'Sky', capacity: 8 },
        { id: '0199a0b0-0000-7000-8000-000000000021', name: 'Ground', capacity: 4 },
      ],
    };

    expect(toOffice(dto)).toEqual({
      id: '0199a0b0-0000-7000-8000-000000000010',
      name: 'Berlin',
      location: 'Berlin, DE',
      capacity: 12,
      rooms: [
        { id: '0199a0b0-0000-7000-8000-000000000020', name: 'Sky', capacity: 8 },
        { id: '0199a0b0-0000-7000-8000-000000000021', name: 'Ground', capacity: 4 },
      ],
    });
  });

  it('coerces a string capacity from the contract to a number', () => {
    const dto: OfficeResponse = {
      id: '0199a0b0-0000-7000-8000-000000000011',
      name: 'Munich',
      location: 'Munich, DE',
      capacity: '5',
      rooms: [{ id: '0199a0b0-0000-7000-8000-000000000022', name: 'Alpha', capacity: '5' }],
    };

    const office = toOffice(dto);

    expect(office.capacity).toBe(5);
    expect(office.rooms[0].capacity).toBe(5);
  });

  it('maps an office with no rooms to an empty room list', () => {
    const dto: OfficeResponse = {
      id: '0199a0b0-0000-7000-8000-000000000012',
      name: 'Hamburg',
      location: 'Hamburg, DE',
      capacity: 0,
      rooms: [],
    };

    expect(toOffice(dto).rooms).toEqual([]);
  });
});
