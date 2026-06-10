import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { AccountGateway } from '@roomy/identity-data-access';
import { Message, Page } from '@roomy/shared-ui';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'roomy-account-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, Page, Message],
  templateUrl: './account-page.html',
  styleUrl: './account-page.css',
})
export class AccountPage {
  private readonly accountGateway = inject(AccountGateway);

  protected readonly failed = signal(false);

  // A read-once load of the current account; toSignal owns the subscription and tears it down with the
  // component. A failed load flips `failed` and resolves to null.
  protected readonly account = toSignal(
    this.accountGateway.getCurrentAccount().pipe(
      catchError(() => {
        this.failed.set(true);
        return of(null);
      }),
    ),
    { initialValue: null },
  );
}
