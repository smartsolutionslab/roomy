import { Component, provideZonelessChangeDetection } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { importOrganizationTestTransloco } from '../../testing/transloco';

import { CreateOfficeForm } from './create-office-form';

@Component({
  imports: [CreateOfficeForm],
  template: `<roomy-create-office-form
    [form]="form"
    [conflict]="conflict"
    [failed]="failed"
    (submitted)="submitted = submitted + 1"
  />`,
})
class HostComponent {
  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: Validators.required }),
    location: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });
  conflict = false;
  failed = false;
  submitted = 0;
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    imports: [importOrganizationTestTransloco()],
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('CreateOfficeForm', () => {
  it('renders the heading, the name and location fields, and the submit button', async () => {
    await renderHost();

    expect(screen.getByRole('heading', { name: 'Add an office' })).toBeTruthy();
    expect(screen.getByLabelText('Name')).toBeTruthy();
    expect(screen.getByLabelText('Location')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Create office' })).toBeTruthy();
  });

  it('hides the name-conflict message until conflict is set', async () => {
    await renderHost({ conflict: false });

    expect(screen.queryByText('An office with that name already exists.')).toBeNull();
  });

  it('shows the name-conflict message when conflict is set', async () => {
    await renderHost({ conflict: true });

    expect(screen.getByText('An office with that name already exists.')).toBeTruthy();
  });

  it('hides the generic error until failed is set', async () => {
    await renderHost({ failed: false });

    expect(screen.queryByText('We could not create the office. Please try again.')).toBeNull();
  });

  it('shows the generic error when failed is set', async () => {
    await renderHost({ failed: true });

    expect(screen.getByText('We could not create the office. Please try again.')).toBeTruthy();
  });

  it('emits submitted when the form is submitted', async () => {
    const { fixture } = await renderHost();

    await userEvent.type(screen.getByLabelText('Name'), 'Munich');
    await userEvent.type(screen.getByLabelText('Location'), 'Munich, DE');
    await userEvent.click(screen.getByRole('button', { name: 'Create office' }));

    expect(fixture.componentInstance.submitted).toBe(1);
  });
});
