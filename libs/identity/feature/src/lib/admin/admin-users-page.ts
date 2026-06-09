import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { AdminUser, AdminUsersGateway, UserId } from '@roomy/identity-data-access';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'roomy-admin-users-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective],
  templateUrl: './admin-users-page.html',
  styleUrl: './admin-users-page.css',
})
export class AdminUsersPage {
  private readonly adminUsersGateway = inject(AdminUsersGateway);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly users = signal<AdminUser[] | null>(null);
  protected readonly loadFailed = signal(false);
  protected readonly grantFailed = signal(false);
  protected readonly confirmingUserId = signal<UserId | null>(null);
  protected readonly grantSucceeded = signal<string | null>(null);

  constructor() {
    this.adminUsersGateway
      .getAll()
      .pipe(
        takeUntilDestroyed(),
        catchError(() => {
          this.loadFailed.set(true);
          return of<AdminUser[]>([]);
        }),
      )
      .subscribe((accounts) => this.users.set(accounts));
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
