import { Component, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { DayOption, DaySelect } from './day-select';

@Component({
  imports: [DaySelect],
  template: `<roomy-day-select
    [label]="label"
    [placeholder]="placeholder"
    [options]="options"
    [value]="value"
    (selected)="chosen = $event"
  />`,
})
class HostComponent {
  label = 'Day';
  placeholder = 'Pick a day';
  options: DayOption[] = [
    { value: '2026-06-10', weekday: 'Wed', date: 'Jun 10' },
    { value: '2026-06-11', weekday: 'Thu', date: 'Jun 11' },
  ];
  value: string | null = null;
  chosen = '';
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('DaySelect', () => {
  it('renders a tile per day showing the weekday and the date on separate lines', async () => {
    await renderHost();

    expect(screen.getByText('Wed')).toBeTruthy();
    expect(screen.getByText('Jun 10')).toBeTruthy();
    expect(screen.getByText('Thu')).toBeTruthy();
    expect(screen.getByText('Jun 11')).toBeTruthy();
    expect(screen.getByRole('button', { name: /Wed/ })).toBeTruthy();
  });

  it('marks the selected day tile as pressed', async () => {
    await renderHost({ value: '2026-06-11' });

    expect(screen.getByRole('button', { name: /Thu/ }).getAttribute('aria-pressed')).toBe('true');
    expect(screen.getByRole('button', { name: /Wed/ }).getAttribute('aria-pressed')).toBe('false');
  });

  it('emits the chosen day when a tile is clicked', async () => {
    const { fixture } = await renderHost();

    await userEvent.click(screen.getByRole('button', { name: /Thu/ }));

    expect(fixture.componentInstance.chosen).toBe('2026-06-11');
  });

  it('falls back to a labelled dropdown of the same days', async () => {
    const { fixture } = await renderHost();

    const fallback = screen.getByRole('combobox', { name: 'Day' });
    expect(screen.getByRole('option', { name: 'Pick a day' })).toBeTruthy();

    await userEvent.selectOptions(fallback, '2026-06-10');

    expect(fixture.componentInstance.chosen).toBe('2026-06-10');
  });
});
