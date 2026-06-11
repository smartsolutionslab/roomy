import { provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import type { SelectOption } from '../select/select';

import { TileGroup } from './tile-group';

const options: SelectOption[] = [
  { value: 'o1', label: 'Munich' },
  { value: 'o2', label: 'Berlin' },
];

async function renderTiles(value: string | null = null) {
  const selected: string[] = [];
  const view = await render(TileGroup, {
    inputs: { label: 'Office', options, value },
    on: { selected: (chosen: string) => selected.push(chosen) },
    providers: [provideZonelessChangeDetection()],
  });
  return { ...view, selected };
}

describe('TileGroup', () => {
  it('renders a labelled group with a tile per option', async () => {
    await renderTiles();

    expect(screen.getByRole('group', { name: 'Office' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Munich' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Berlin' })).toBeTruthy();
  });

  it('marks no tile as pressed when nothing is selected', async () => {
    await renderTiles(null);

    expect(screen.getByRole('button', { name: 'Munich' }).getAttribute('aria-pressed')).toBe(
      'false',
    );
    expect(screen.getByRole('button', { name: 'Berlin' }).getAttribute('aria-pressed')).toBe(
      'false',
    );
  });

  it('marks the tile matching the value as pressed', async () => {
    await renderTiles('o2');

    expect(screen.getByRole('button', { name: 'Berlin' }).getAttribute('aria-pressed')).toBe(
      'true',
    );
    expect(screen.getByRole('button', { name: 'Munich' }).getAttribute('aria-pressed')).toBe(
      'false',
    );
  });

  it('emits the value of a clicked tile', async () => {
    const { selected } = await renderTiles();

    await userEvent.click(screen.getByRole('button', { name: 'Munich' }));

    expect(selected).toEqual(['o1']);
  });
});
