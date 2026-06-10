import { Component, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { DaySelect } from './day-select';

@Component({
  imports: [DaySelect],
  template: `<roomy-day-select
    [label]="label"
    [placeholder]="placeholder"
    [days]="days"
    (daySelected)="chosen = $event"
  />`,
})
class HostComponent {
  label = 'Day';
  placeholder = 'Pick a day';
  days = ['2026-06-10', '2026-06-11'];
  chosen = '';
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('DaySelect', () => {
  it('labels the control and offers the placeholder plus each day', async () => {
    await renderHost();

    expect(screen.getByLabelText('Day')).toBeTruthy();
    expect(screen.getByRole('option', { name: 'Pick a day' })).toBeTruthy();
    expect(screen.getByRole('option', { name: '2026-06-10' })).toBeTruthy();
    expect(screen.getByRole('option', { name: '2026-06-11' })).toBeTruthy();
  });

  it('emits the chosen day', async () => {
    const { fixture } = await renderHost();

    await userEvent.selectOptions(screen.getByLabelText('Day'), '2026-06-11');

    expect(fixture.componentInstance.chosen).toBe('2026-06-11');
  });
});
