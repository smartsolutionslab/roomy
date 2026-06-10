import { Component, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { Select, SelectOption } from './select';

@Component({
  imports: [Select],
  template: `<roomy-select
    [label]="label"
    [placeholder]="placeholder"
    [options]="options"
    (selected)="chosen = $event"
  />`,
})
class HostComponent {
  label = 'Office';
  placeholder = 'Pick an office';
  options: SelectOption[] = [
    { value: 'office-1', label: 'London' },
    { value: 'office-2', label: 'Berlin' },
  ];
  chosen = '';
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('Select', () => {
  it('labels the control and offers the placeholder plus each option', async () => {
    await renderHost();

    expect(screen.getByLabelText('Office')).toBeTruthy();
    expect(screen.getByRole('option', { name: 'Pick an office' })).toBeTruthy();
    expect(screen.getByRole('option', { name: 'London' })).toBeTruthy();
    expect(screen.getByRole('option', { name: 'Berlin' })).toBeTruthy();
  });

  it('emits the chosen option value', async () => {
    const { fixture } = await renderHost();

    await userEvent.selectOptions(screen.getByLabelText('Office'), 'office-2');

    expect(fixture.componentInstance.chosen).toBe('office-2');
  });

  it('emits the empty string when the placeholder is reselected', async () => {
    const { fixture } = await renderHost();

    await userEvent.selectOptions(screen.getByLabelText('Office'), 'office-2');
    await userEvent.selectOptions(screen.getByLabelText('Office'), 'Pick an office');

    expect(fixture.componentInstance.chosen).toBe('');
  });
});
