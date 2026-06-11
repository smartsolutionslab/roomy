import { Component, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { SelectOption } from '../select/select';

import { Combobox } from './combobox';

@Component({
  imports: [Combobox],
  template: `<roomy-combobox
    [label]="label"
    [placeholder]="placeholder"
    [options]="options"
    [loading]="loading"
    [noResultsText]="noResultsText"
    (searchChange)="searched = searched.concat($event)"
    (selected)="chosen = $event"
  />`,
})
class HostComponent {
  label = 'Employee';
  placeholder = 'Search by name';
  options: SelectOption[] = [
    { value: 'e1', label: 'Ada' },
    { value: 'e2', label: 'Hannah' },
  ];
  loading = false;
  noResultsText = 'No matches';
  searched: string[] = [];
  chosen = '';
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('Combobox', () => {
  it('renders a labelled combobox with the listbox closed', async () => {
    await renderHost();

    expect(screen.getByRole('combobox', { name: 'Employee' })).toBeTruthy();
    expect(screen.queryByRole('listbox')).toBeNull();
  });

  it('opens and emits the typed query', async () => {
    const { fixture } = await renderHost();

    await userEvent.type(screen.getByRole('combobox', { name: 'Employee' }), 'ada');

    expect(fixture.componentInstance.searched).toContain('ada');
    expect(screen.getByRole('listbox')).toBeTruthy();
    expect(screen.getByRole('option', { name: 'Ada' })).toBeTruthy();
  });

  it('selects an option on click, filling the input and closing the list', async () => {
    const { fixture } = await renderHost();
    const input = screen.getByRole('combobox', { name: 'Employee' }) as HTMLInputElement;

    await userEvent.type(input, 'a');
    await userEvent.click(screen.getByRole('option', { name: 'Ada' }));

    expect(fixture.componentInstance.chosen).toBe('e1');
    expect(input.value).toBe('Ada');
    expect(screen.queryByRole('listbox')).toBeNull();
  });

  it('navigates with the arrow keys and selects with Enter', async () => {
    const { fixture } = await renderHost();
    const input = screen.getByRole('combobox', { name: 'Employee' });

    await userEvent.type(input, 'a');
    await userEvent.keyboard('{ArrowDown}{ArrowDown}{Enter}');

    expect(fixture.componentInstance.chosen).toBe('e2');
  });

  it('shows the no-results text when a query matches nothing', async () => {
    await renderHost({ options: [] });

    await userEvent.type(screen.getByRole('combobox', { name: 'Employee' }), 'zzz');

    expect(screen.getByText('No matches')).toBeTruthy();
  });

  it('closes the list on Escape', async () => {
    await renderHost();

    const input = screen.getByRole('combobox', { name: 'Employee' });
    await userEvent.type(input, 'a');
    expect(screen.getByRole('listbox')).toBeTruthy();

    await userEvent.keyboard('{Escape}');

    expect(screen.queryByRole('listbox')).toBeNull();
  });
});
