import { Route } from '@angular/router';
import { identityRoutes } from '@roomy/identity-feature';

export const appRoutes: Route[] = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./home/home').then((module) => module.Home),
  },
  ...identityRoutes,
];
