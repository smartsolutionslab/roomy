import { provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import { Observable, of, throwError } from 'rxjs';

import { importIdentityTestTransloco } from '../../testing/transloco';
import { Account } from '../data-access/account';
import { AccountClient } from '../data-access/account-client';

import { AccountPage } from './account-page';

function renderAccountPage(currentAccount: Observable<Account>) {
  return render(AccountPage, {
    imports: [importIdentityTestTransloco()],
    providers: [
      provideZonelessChangeDetection(),
      { provide: AccountClient, useValue: { getCurrentAccount: () => currentAccount } },
    ],
  });
}

describe('AccountPage', () => {
  const account: Account = {
    userId: 'a3f1c2d4-0000-7000-8000-000000000001',
    email: 'ada@roomy.test',
    displayName: 'Ada Lovelace',
    role: 'administrator',
  };

  it('shows the signed-in account with a localized role', async () => {
    await renderAccountPage(of(account));

    expect(await screen.findByText('Ada Lovelace')).toBeTruthy();
    expect(screen.getByText('ada@roomy.test')).toBeTruthy();
    expect(screen.getByText('Administrator')).toBeTruthy();
  });

  it('localizes an employee role', async () => {
    await renderAccountPage(of({ ...account, role: 'employee' }));

    expect(await screen.findByText('Employee')).toBeTruthy();
  });

  it('shows an error message when the account cannot be loaded', async () => {
    await renderAccountPage(throwError(() => new Error('gateway unavailable')));

    expect(await screen.findByText('We could not load your account.')).toBeTruthy();
  });
});
