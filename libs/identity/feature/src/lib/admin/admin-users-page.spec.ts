import { provideZonelessChangeDetection } from '@angular/core';
import { AdminUser, AdminUsersGateway, UserId, userId } from '@roomy/identity-data-access';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Observable, of, throwError } from 'rxjs';

import { importIdentityTestTransloco } from '../../testing/transloco';

import { AdminUsersPage } from './admin-users-page';

const employee: AdminUser = {
  userId: userId('a3f1c2d4-0000-7000-8000-000000000002'),
  email: 'grace@roomy.test',
  displayName: 'Grace Hopper',
  role: 'employee',
  status: 'active',
};

function renderPage(
  accounts: AdminUser[],
  grant: (user: UserId) => Observable<void> = () => of(undefined),
) {
  return render(AdminUsersPage, {
    imports: [importIdentityTestTransloco()],
    providers: [
      provideZonelessChangeDetection(),
      {
        provide: AdminUsersGateway,
        useValue: { getAll: () => of(accounts), grantAdministrator: grant },
      },
    ],
  });
}

describe('AdminUsersPage', () => {
  it('lists each account with its name, email and localized role and status', async () => {
    await renderPage([employee]);

    expect(await screen.findByText('Grace Hopper')).toBeTruthy();
    expect(screen.getByText('grace@roomy.test')).toBeTruthy();
    expect(screen.getByText('Employee')).toBeTruthy();
    expect(screen.getByText('Active')).toBeTruthy();
  });

  it('shows an empty state when there are no accounts', async () => {
    await renderPage([]);

    expect(await screen.findByText('No accounts yet.')).toBeTruthy();
  });

  it('grants administrator and reflects the new role in the row', async () => {
    await renderPage([employee]);

    await userEvent.click(await screen.findByRole('button', { name: 'Grant administrator' }));

    expect(await screen.findByText('Administrator')).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Grant administrator' })).toBeNull();
  });

  it('keeps the row unchanged and announces an error when granting fails', async () => {
    await renderPage([employee], () => throwError(() => new Error('gateway error')));

    await userEvent.click(await screen.findByRole('button', { name: 'Grant administrator' }));

    expect(
      await screen.findByText('We could not grant administrator. Please try again.'),
    ).toBeTruthy();
    expect(screen.getByText('Employee')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Grant administrator' })).toBeTruthy();
  });
});
