import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { AdminUser, AdminUsersGateway, UserId } from '@roomy/identity-api';
import { cursorList } from '@roomy/shared-data-access';
import { Button, InfiniteScroll, Message, Page } from '@roomy/shared-ui';

@Component({
  selector: 'roomy-admin-users-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, InfiniteScroll, Page, Message, Button],
  templateUrl: './admin-users-page.html',
  styleUrl: './admin-users-page.css',
})
export class AdminUsersPage {
  private readonly adminUsersGateway = inject(AdminUsersGateway);
  private readonly destroyRef = inject(DestroyRef);

  // The endless account list: the helper owns the cursor accumulation, so this page declares only the
  // fetch and binds the helper's signals to roomy-infinite-scroll.
  protected readonly list = cursorList<AdminUser>((cursor) =>
    this.adminUsersGateway.getAll(cursor),
  );

  protected readonly grantFailed = signal(false);
  protected readonly confirmingUserId = signal<UserId | null>(null);
  protected readonly grantSucceeded = signal<string | null>(null);

  protected requestGrant(account: AdminUser): void {
    this.grantFailed.set(false);
    this.grantSucceeded.set(null);
    this.confirmingUserId.set(account.userId);
  }

  protected cancelGrant(): void {
    this.confirmingUserId.set(null);
  }

  protected confirmGrant(account: AdminUser): void {
    this.grantFailed.set(false);
    this.adminUsersGateway
      .grantAdministrator(account.userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.list.update((accounts) =>
            accounts.map((candidate) =>
              candidate.userId === account.userId
                ? { ...candidate, role: 'administrator' }
                : candidate,
            ),
          );
          this.confirmingUserId.set(null);
          this.grantSucceeded.set(account.displayName);
        },
        error: () => {
          this.confirmingUserId.set(null);
          this.grantFailed.set(true);
        },
      });
  }
}
