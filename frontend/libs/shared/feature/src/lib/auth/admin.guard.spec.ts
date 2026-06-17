import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { CurrentUser, SessionService } from '@roomy/shared-data-access';

import { adminGuard } from './admin.guard';

function configure(currentUser: CurrentUser | null) {
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      {
        provide: SessionService,
        useValue: {
          currentUser: () => currentUser,
          isAdministrator: () => currentUser?.roles.includes('administrator') ?? false,
          ensureLoaded: () => Promise.resolve(),
        },
      },
    ],
  });
}

function runGuard(): Promise<boolean | UrlTree> {
  const state = { url: '/admin/users' } as RouterStateSnapshot;
  return TestBed.runInInjectionContext(() =>
    adminGuard({} as ActivatedRouteSnapshot, state),
  ) as Promise<boolean | UrlTree>;
}

describe('adminGuard', () => {
  it('allows an administrator', async () => {
    configure({ name: 'Ada Lovelace', roles: ['employee', 'administrator'] });

    expect(await runGuard()).toBe(true);
  });

  it('redirects a non-administrator to the not-authorized view', async () => {
    configure({ name: 'Grace Hopper', roles: ['employee'] });

    const result = await runGuard();

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/not-authorized');
  });
});
