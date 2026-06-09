import { Route } from '@angular/router';
import { identityRoutes } from '@roomy/identity-feature';
import { organizationRoutes } from '@roomy/organization-feature';
import { notAuthorizedRoute } from '@roomy/shared-feature';

export const appRoutes: Route[] = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./home/home').then((module) => module.Home),
  },
  notAuthorizedRoute,
  ...identityRoutes,
  ...organizationRoutes,
];
