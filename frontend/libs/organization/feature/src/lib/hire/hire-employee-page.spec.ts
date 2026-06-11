import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { EmployeesGateway, HireEmployeeDetails, HiredEmployee } from '@roomy/organization-api';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Observable, of, throwError } from 'rxjs';

import { importOrganizationTestTransloco } from '../../testing/transloco';

import { HireEmployeePage } from './hire-employee-page';

const provisioning: HiredEmployee = {
  employeeId: '0199a0b0-0000-7000-8000-000000000001',
  userId: '0199a0b0-0000-7000-8000-000000000002',
  state: 'Provisioning',
};

interface GatewayStub {
  hire?: (details: HireEmployeeDetails) => Observable<HiredEmployee>;
}

function renderPage(stub: GatewayStub = {}) {
  const calls: HireEmployeeDetails[] = [];
  const gateway = {
    hire: (details: HireEmployeeDetails) => {
      calls.push(details);
      return (stub.hire ?? (() => of(provisioning)))(details);
    },
  };

  return render(HireEmployeePage, {
    imports: [importOrganizationTestTransloco()],
    providers: [provideZonelessChangeDetection(), { provide: EmployeesGateway, useValue: gateway }],
  }).then((view) => ({ ...view, calls }));
}

async function fillValidHire() {
  await userEvent.type(screen.getByLabelText('Display name'), 'Ada Lovelace');
  await userEvent.type(screen.getByLabelText('Work email'), 'ada@example.com');
  await userEvent.selectOptions(screen.getByLabelText('Role'), 'Employee');
  await userEvent.type(screen.getByLabelText('Initial password'), 'first-password');
}

describe('HireEmployeePage', () => {
  it('hires a colleague with the entered details and reports provisioning started', async () => {
    const { calls } = await renderPage();

    await fillValidHire();
    await userEvent.click(screen.getByRole('button', { name: 'Hire' }));

    expect(calls).toEqual([
      {
        displayName: 'Ada Lovelace',
        email: 'ada@example.com',
        role: 'Employee',
        initialPassword: 'first-password',
      },
    ]);
    // The acknowledgement says provisioning started — not that the account is ready to use.
    expect(screen.getByText(/was hired/i)).toBeTruthy();
  });

  it('clears the form after a successful hire', async () => {
    await renderPage();

    await fillValidHire();
    await userEvent.click(screen.getByRole('button', { name: 'Hire' }));

    expect((screen.getByLabelText('Display name') as HTMLInputElement).value).toBe('');
    expect((screen.getByLabelText('Initial password') as HTMLInputElement).value).toBe('');
    expect((screen.getByLabelText('Role') as HTMLSelectElement).value).toBe('');
  });

  it('sends the administrator role when chosen', async () => {
    const { calls } = await renderPage();

    await userEvent.type(screen.getByLabelText('Display name'), 'Grace Hopper');
    await userEvent.type(screen.getByLabelText('Work email'), 'grace@example.com');
    await userEvent.selectOptions(screen.getByLabelText('Role'), 'Administrator');
    await userEvent.type(screen.getByLabelText('Initial password'), 'first-password');
    await userEvent.click(screen.getByRole('button', { name: 'Hire' }));

    expect(calls[0]?.role).toBe('Administrator');
  });

  it('does not call the API while required fields are missing', async () => {
    const { calls } = await renderPage();

    await userEvent.click(screen.getByRole('button', { name: 'Hire' }));

    expect(calls).toEqual([]);
  });

  it('does not call the API for a malformed email', async () => {
    const { calls } = await renderPage();

    await userEvent.type(screen.getByLabelText('Display name'), 'Ada Lovelace');
    await userEvent.type(screen.getByLabelText('Work email'), 'not-an-email');
    await userEvent.selectOptions(screen.getByLabelText('Role'), 'Employee');
    await userEvent.type(screen.getByLabelText('Initial password'), 'first-password');
    await userEvent.click(screen.getByRole('button', { name: 'Hire' }));

    expect(calls).toEqual([]);
  });

  it('shows a validation error and does not claim a hire when the server rejects it (400)', async () => {
    await renderPage({
      hire: () => throwError(() => new HttpErrorResponse({ status: 400 })),
    });

    await fillValidHire();
    await userEvent.click(screen.getByRole('button', { name: 'Hire' }));

    expect(screen.getByText('Please check the details and try again.')).toBeTruthy();
    expect(screen.queryByText(/was hired/i)).toBeNull();
  });

  it('shows a generic failure on an unexpected error', async () => {
    await renderPage({
      hire: () => throwError(() => new HttpErrorResponse({ status: 500 })),
    });

    await fillValidHire();
    await userEvent.click(screen.getByRole('button', { name: 'Hire' }));

    expect(screen.getByText('We could not complete the hire. Please try again.')).toBeTruthy();
    expect(screen.queryByText(/was hired/i)).toBeNull();
  });
});
