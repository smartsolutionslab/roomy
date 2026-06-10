import { provideZonelessChangeDetection, signal } from '@angular/core';
import { CurrentUser, SessionService } from '@roomy/shared-data-access';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { importSharedTestTransloco } from '../../testing/transloco';

import { UserMenu } from './user-menu';

async function renderMenu(user: CurrentUser | null = { name: 'Ada Lovelace', roles: ['employee'] }) {
  return render(UserMenu, {
    imports: [importSharedTestTransloco()],
    providers: [
      provideZonelessChangeDetection(),
      { provide: SessionService, useValue: { currentUser: signal(user) } as unknown as SessionService },
    ],
  });
}

describe('UserMenu', () => {
  it('shows the account avatar and keeps the menu closed until activated', async () => {
    await renderMenu();

    expect(screen.getByRole('button', { name: 'Account menu' })).toBeTruthy();
    expect(screen.getByText('AL')).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Sign out' })).toBeNull();
  });

  it('reveals the user name and a sign-out action when opened', async () => {
    await renderMenu();

    await userEvent.click(screen.getByRole('button', { name: 'Account menu' }));

    expect(screen.getByText('Ada Lovelace')).toBeTruthy();
    const signOut = screen.getByRole('button', { name: 'Sign out' });
    expect((signOut.closest('form') as HTMLFormElement).getAttribute('action')).toBe('/bff/logout');
  });

  it('renders nothing when there is no signed-in user', async () => {
    await renderMenu(null);

    expect(screen.queryByRole('button', { name: 'Account menu' })).toBeNull();
  });
});
