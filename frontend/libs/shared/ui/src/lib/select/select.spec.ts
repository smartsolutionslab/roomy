import { Component, provideZonelessChangeDetection } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
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

@Component({
  imports: [Select, ReactiveFormsModule],
  template: `<roomy-select
    [label]="'Office'"
    [placeholder]="'Pick an office'"
    [options]="options"
    [formControl]="control"
  />`,
})
class FormHostComponent {
  readonly control = new FormControl('', { nonNullable: true });
  options: SelectOption[] = [
    { value: 'office-1', label: 'London' },
    { value: 'office-2', label: 'Berlin' },
  ];
}

function renderFormHost() {
  return render(FormHostComponent, {
    providers: [provideZonelessChangeDetection()],
  });
}

describe('Select as a form control', () => {
  it('reflects the form control value in the rendered selection', async () => {
    const { fixture } = await renderFormHost();

    fixture.componentInstance.control.setValue('office-2');
    fixture.detectChanges();

    expect((screen.getByLabelText('Office') as HTMLSelectElement).value).toBe('office-2');
  });

  it('writes the chosen option into the form control', async () => {
    const { fixture } = await renderFormHost();

    await userEvent.selectOptions(screen.getByLabelText('Office'), 'office-1');

    expect(fixture.componentInstance.control.value).toBe('office-1');
  });

  it('returns to the placeholder when the control is reset', async () => {
    const { fixture } = await renderFormHost();

    await userEvent.selectOptions(screen.getByLabelText('Office'), 'office-1');
    fixture.componentInstance.control.reset();
    fixture.detectChanges();

    expect((screen.getByLabelText('Office') as HTMLSelectElement).value).toBe('');
  });
});
