import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '@roomy/shared-data-access';

// Requires the administrator role. Assumes a decided session (pair with authGuard, which runs first),
// but awaits readiness defensively. A non-administrator is sent to the not-authorized view rather than
// the BFF — they are signed in, just not permitted.
export const adminGuard: CanActivateFn = async () => {
  const session = inject(SessionService);
  const router = inject(Router);

  await session.ensureLoaded();
  if (session.isAdministrator()) {
    return true;
  }

  return router.parseUrl('/not-authorized');
};
