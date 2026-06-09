import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { catchError, of } from 'rxjs';

import { Account } from '../data-access/account';
import { AccountClient } from '../data-access/account-client';

@Component({
  selector: 'roomy-account-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective],
  templateUrl: './account-page.html',
  styleUrl: './account-page.css',
})
export class AccountPage {
  private readonly accountClient = inject(AccountClient);

  protected readonly account = signal<Account | null>(null);
  protected readonly failed = signal(false);

  constructor() {
    this.accountClient
      .getCurrentAccount()
      .pipe(
        takeUntilDestroyed(),
        catchError(() => {
          this.failed.set(true);
          return of(null);
        }),
      )
      .subscribe((account) => this.account.set(account));
  }
}
