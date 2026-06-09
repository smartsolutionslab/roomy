import { Routes } from '@angular/router';
import { adminGuard, authGuard } from '@roomy/shared-feature';

export const organizationRoutes: Routes = [
  {
    path: 'offices',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./offices/offices-page').then((module) => module.OfficesPage),
  },
];
