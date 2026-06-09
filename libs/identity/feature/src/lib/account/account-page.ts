import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { Account, AccountGateway } from '@roomy/identity-data-access';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'roomy-account-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective],
  templateUrl: './account-page.html',
  styleUrl: './account-page.css',
})
export class AccountPage {
  private readonly accountGateway = inject(AccountGateway);

  protected readonly account = signal<Account | null>(null);
  protected readonly failed = signal(false);

  constructor() {
    this.accountGateway
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
