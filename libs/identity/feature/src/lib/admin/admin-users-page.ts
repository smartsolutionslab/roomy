import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { AdminUser, AdminUsersGateway } from '@roomy/identity-data-access';
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

  protected grantAdministrator(account: AdminUser): void {
    this.grantFailed.set(false);
    this.adminUsersGateway
      .grantAdministrator(account.userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () =>
          this.users.update((accounts) =>
            (accounts ?? []).map((candidate) =>
              candidate.userId === account.userId
                ? { ...candidate, role: 'administrator' }
                : candidate,
            ),
          ),
        error: () => this.grantFailed.set(true),
      });
  }
}
