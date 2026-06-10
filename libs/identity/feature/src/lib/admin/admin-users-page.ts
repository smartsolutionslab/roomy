import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { AdminUser, AdminUsersGateway, UserId } from '@roomy/identity-data-access';
import { InfiniteScroll } from '@roomy/shared-ui';

@Component({
  selector: 'roomy-admin-users-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, InfiniteScroll],
  templateUrl: './admin-users-page.html',
  styleUrl: './admin-users-page.css',
})
export class AdminUsersPage {
  private readonly adminUsersGateway = inject(AdminUsersGateway);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly users = signal<AdminUser[] | null>(null);
  protected readonly nextCursor = signal<string | null>(null);
  protected readonly loadingMore = signal(false);
  protected readonly loadFailed = signal(false);
  protected readonly grantFailed = signal(false);
  protected readonly confirmingUserId = signal<UserId | null>(null);
  protected readonly grantSucceeded = signal<string | null>(null);

  constructor() {
    this.loadMore();
  }

  // Loads the next page (the first when no cursor yet) and appends it, so the list grows as the
  // administrator scrolls or activates "Load more" (ADR-0042). nextCursor === null marks the end.
  protected loadMore(): void {
    if (this.loadingMore()) {
      return;
    }

    this.loadingMore.set(true);
    this.adminUsersGateway
      .getAll(this.nextCursor() ?? undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          this.users.update((current) => [...(current ?? []), ...page.items]);
          this.nextCursor.set(page.nextCursor);
          this.loadingMore.set(false);
        },
        error: () => {
          this.loadFailed.set(true);
          this.users.update((current) => current ?? []);
          this.loadingMore.set(false);
        },
      });
  }

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
          this.users.update((accounts) =>
            (accounts ?? []).map((candidate) =>
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
