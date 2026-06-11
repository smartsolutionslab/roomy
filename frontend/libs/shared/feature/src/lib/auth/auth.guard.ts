import { DOCUMENT } from '@angular/common';
import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { SessionService } from '@roomy/shared-data-access';

// Requires a BFF session for the route. Waits for the first session resolution, then either allows
// navigation or sends the browser to the BFF sign-in with a return URL back to the attempted route.
// The redirect is a full-page navigation to the gateway, not an Angular route.
export const authGuard: CanActivateFn = async (_route, state) => {
  const session = inject(SessionService);
  const document = inject(DOCUMENT);

  await session.ensureLoaded();
  if (session.currentUser()) {
    return true;
  }

  const returnUrl = encodeURIComponent(state.url);
  document.defaultView?.location.assign(`/bff/login?returnUrl=${returnUrl}`);
  return false;
};
