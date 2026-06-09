import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { CurrentUser, SessionService } from '@roomy/shared-data-access';

import { authGuard } from './auth.guard';

interface SessionStub {
  currentUser: () => CurrentUser | null;
  ensureLoaded: () => Promise<void>;
}

function configure(session: SessionStub) {
  const assign = vi.fn();
  TestBed.configureTestingModule({
    providers: [
      { provide: SessionService, useValue: session },
      { provide: DOCUMENT, useValue: { defaultView: { location: { assign } } } },
    ],
  });
  return { assign };
}

function runGuard(url: string): Promise<boolean> {
  const state = { url } as RouterStateSnapshot;
  return TestBed.runInInjectionContext(() =>
    authGuard({} as ActivatedRouteSnapshot, state),
  ) as Promise<boolean>;
}

describe('authGuard', () => {
  it('allows navigation once a session is present', async () => {
    configure({
      currentUser: () => ({ name: 'Ada Lovelace', roles: ['employee'] }),
      ensureLoaded: () => Promise.resolve(),
    });

    expect(await runGuard('/account')).toBe(true);
  });

  it('redirects to the BFF sign-in with a return URL when there is no session', async () => {
    const { assign } = configure({
      currentUser: () => null,
      ensureLoaded: () => Promise.resolve(),
    });

    expect(await runGuard('/account')).toBe(false);
    expect(assign).toHaveBeenCalledWith('/bff/login?returnUrl=%2Faccount');
  });
});
