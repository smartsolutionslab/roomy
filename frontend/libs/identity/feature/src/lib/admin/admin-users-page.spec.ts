import { provideZonelessChangeDetection } from '@angular/core';
import { AdminUser, AdminUsersGateway, UserId, userId } from '@roomy/identity-data-access';
import type { Page } from '@roomy/shared-data-access';
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

const administrator: AdminUser = {
  userId: userId('a3f1c2d4-0000-7000-8000-000000000003'),
  email: 'ada@roomy.test',
  displayName: 'Ada Lovelace',
  role: 'administrator',
  status: 'active',
};

function page(items: AdminUser[], nextCursor: string | null = null): Page<AdminUser> {
  return { items, nextCursor };
}

function renderPage(
  accounts: AdminUser[],
  grant: (user: UserId) => Observable<void> = () => of(undefined),
  getAll: (cursor?: string) => Observable<Page<AdminUser>> = () => of(page(accounts)),
) {
  return render(AdminUsersPage, {
    imports: [importIdentityTestTransloco()],
    providers: [
      provideZonelessChangeDetection(),
      {
        provide: AdminUsersGateway,
        useValue: { getAll, grantAdministrator: grant },
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

  it('asks for confirmation before granting administrator', async () => {
    const grant = vi.fn(() => of<void>(undefined));
    await renderPage([employee], grant);

    await userEvent.click(await screen.findByRole('button', { name: 'Grant administrator' }));

    expect(await screen.findByRole('button', { name: 'Confirm' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Grant administrator' })).toBeNull();
    expect(grant).not.toHaveBeenCalled();
  });

  it('restores the grant action and makes no request when the confirmation is cancelled', async () => {
    const grant = vi.fn(() => of<void>(undefined));
    await renderPage([employee], grant);

    await userEvent.click(await screen.findByRole('button', { name: 'Grant administrator' }));
    await userEvent.click(await screen.findByRole('button', { name: 'Cancel' }));

    expect(await screen.findByRole('button', { name: 'Grant administrator' })).toBeTruthy();
    expect(grant).not.toHaveBeenCalled();
  });

  it('grants administrator on confirmation and announces the new role in the row', async () => {
    const grant = vi.fn(() => of<void>(undefined));
    await renderPage([employee], grant);

    await userEvent.click(await screen.findByRole('button', { name: 'Grant administrator' }));
    await userEvent.click(await screen.findByRole('button', { name: 'Confirm' }));

    expect(await screen.findByText('Administrator')).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Grant administrator' })).toBeNull();
    expect(grant).toHaveBeenCalledTimes(1);

    expect(await screen.findByText('Granted administrator to Grace Hopper.')).toBeTruthy();
  });

  it('keeps the row unchanged and announces an error when granting fails', async () => {
    await renderPage([employee], () => throwError(() => new Error('gateway error')));

    await userEvent.click(await screen.findByRole('button', { name: 'Grant administrator' }));
    await userEvent.click(await screen.findByRole('button', { name: 'Confirm' }));

    expect(
      await screen.findByText('We could not grant administrator. Please try again.'),
    ).toBeTruthy();
    expect(screen.getByText('Employee')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Grant administrator' })).toBeTruthy();
  });

  it('offers no grant action for an account that is already an administrator', async () => {
    await renderPage([administrator]);

    expect(await screen.findByText('Ada Lovelace')).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Grant administrator' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Confirm' })).toBeNull();
  });

  it('appends the next page of accounts when Load more is activated, then stops at the end', async () => {
    const user = userEvent.setup();
    await renderPage(
      [],
      () => of(undefined),
      (cursor) =>
        of(cursor === undefined ? page([employee], 'cursor-2') : page([administrator], null)),
    );

    expect(await screen.findByText('Grace Hopper')).toBeTruthy();
    expect(screen.queryByText('Ada Lovelace')).toBeNull();

    await user.click(screen.getByRole('button', { name: 'Load more' }));

    expect(await screen.findByText('Ada Lovelace')).toBeTruthy();
    expect(screen.getByText('End of list')).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Load more' })).toBeNull();
  });
});
