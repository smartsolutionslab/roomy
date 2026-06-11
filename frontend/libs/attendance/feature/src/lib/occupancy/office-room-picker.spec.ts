import { provideZonelessChangeDetection } from '@angular/core';
import { BookableOffice, officeId, roomId } from '@roomy/attendance-api';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { importAttendanceTestTransloco } from '../../testing/transloco';

import { OccupancyScope, OfficeRoomPicker } from './office-room-picker';

const munich: BookableOffice = {
  id: officeId('o1'),
  name: 'Munich',
  rooms: [
    { id: roomId('r1'), name: 'A1', capacity: 8 },
    { id: roomId('r2'), name: 'B1', capacity: 5 },
  ],
};

const berlin: BookableOffice = {
  id: officeId('o2'),
  name: 'Berlin',
  rooms: [{ id: roomId('r3'), name: 'C1', capacity: 4 }],
};

async function renderPicker() {
  const scopes: (OccupancyScope | null)[] = [];
  const view = await render(OfficeRoomPicker, {
    imports: [importAttendanceTestTransloco()],
    inputs: { offices: [munich, berlin] },
    on: { scopeChange: (scope: OccupancyScope | null) => scopes.push(scope) },
    providers: [provideZonelessChangeDetection()],
  });
  return { ...view, scopes };
}

describe('OfficeRoomPicker', () => {
  it('offers a tile per office and hides the room select until one is chosen', async () => {
    await renderPicker();

    expect(await screen.findByRole('button', { name: 'Munich' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Berlin' })).toBeTruthy();
    expect(screen.queryByLabelText('Room')).toBeNull();
  });

  it('emits an office scope and reveals the room select when an office tile is chosen', async () => {
    const { scopes } = await renderPicker();

    await userEvent.click(await screen.findByRole('button', { name: 'Munich' }));

    expect(screen.getByLabelText('Room')).toBeTruthy();
    expect(scopes.at(-1)).toEqual({ officeId: 'o1' });
  });

  it("lists the chosen office's rooms and emits a room scope when a room is chosen", async () => {
    const { scopes } = await renderPicker();

    await userEvent.click(await screen.findByRole('button', { name: 'Munich' }));
    expect(screen.getByRole('option', { name: 'A1' })).toBeTruthy();

    await userEvent.selectOptions(screen.getByLabelText('Room'), 'r2');

    expect(scopes.at(-1)).toEqual({ roomId: 'r2' });
  });

  it('falls back to the office scope and resets the room when the office changes', async () => {
    const { scopes } = await renderPicker();

    await userEvent.click(await screen.findByRole('button', { name: 'Munich' }));
    await userEvent.selectOptions(screen.getByLabelText('Room'), 'r2');
    await userEvent.click(screen.getByRole('button', { name: 'Berlin' }));

    expect(scopes.at(-1)).toEqual({ officeId: 'o2' });
    // Only the newly chosen office's rooms are offered.
    expect(screen.getByRole('option', { name: 'C1' })).toBeTruthy();
    expect(screen.queryByRole('option', { name: 'A1' })).toBeNull();
  });

  it('marks the chosen office tile as pressed', async () => {
    await renderPicker();

    await userEvent.click(await screen.findByRole('button', { name: 'Munich' }));

    expect(screen.getByRole('button', { name: 'Munich' }).getAttribute('aria-pressed')).toBe(
      'true',
    );
    expect(screen.getByRole('button', { name: 'Berlin' }).getAttribute('aria-pressed')).toBe(
      'false',
    );
  });
});
