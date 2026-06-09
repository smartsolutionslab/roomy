import { Route } from '@angular/router';

import { authGuard } from './auth/auth.guard';

// The shared `/not-authorized` destination the admin guard redirects a signed-in but unpermitted
// user to (ADR-0038). Registered once at the app composition root; every administrator-gated context
// (identity, organization, …) routes here rather than owning its own copy.
export const notAuthorizedRoute: Route = {
  path: 'not-authorized',
  canActivate: [authGuard],
  loadComponent: () =>
    import('./not-authorized/not-authorized').then((module) => module.NotAuthorized),
};
