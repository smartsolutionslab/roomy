import { provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import type { SelectOption } from '../select/select';

import { TileGroup } from './tile-group';

const options: SelectOption[] = [
  { value: 'o1', label: 'Munich' },
  { value: 'o2', label: 'Berlin' },
];

async function renderTiles(value: string | null = null, placeholder: string | null = null) {
  const selected: string[] = [];
  const view = await render(TileGroup, {
    inputs: { label: 'Office', options, value, placeholder },
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

  it('provides a native dropdown fallback (for small screens) with the same options', async () => {
    await renderTiles();

    const fallback = screen.getByRole('combobox', { name: 'Office' });
    expect(fallback).toBeTruthy();
    expect(screen.getByRole('option', { name: 'Munich' })).toBeTruthy();
    expect(screen.getByRole('option', { name: 'Berlin' })).toBeTruthy();
  });

  it('emits the value chosen from the dropdown fallback', async () => {
    const { selected } = await renderTiles();

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Office' }), 'o2');

    expect(selected).toEqual(['o2']);
  });

  it('shows the placeholder as the empty leading option in the fallback when provided', async () => {
    await renderTiles(null, 'Select an office');

    const placeholder = screen.getByRole('option', {
      name: 'Select an office',
    }) as HTMLOptionElement;
    expect(placeholder.value).toBe('');
    expect(placeholder.selected).toBe(true);
  });
});
