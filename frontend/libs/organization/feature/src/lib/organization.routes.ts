import { Routes } from '@angular/router';
import { adminGuard, authGuard, NavMeta } from '@roomy/shared-feature';

export const organizationRoutes: Routes = [
  {
    path: 'offices',
    canActivate: [authGuard, adminGuard],
    data: {
      nav: {
        labelKey: 'shell.officesLink',
        icon: 'offices',
        order: 60,
        descKey: 'home.cards.offices',
      } satisfies NavMeta,
    },
    loadComponent: () => import('./offices/offices-page').then((module) => module.OfficesPage),
  },
  {
    path: 'employees/hire',
    canActivate: [authGuard, adminGuard],
    data: {
      nav: {
        labelKey: 'shell.hireLink',
        icon: 'hire',
        order: 65,
        descKey: 'home.cards.hire',
      } satisfies NavMeta,
    },
    loadComponent: () =>
      import('./hire/hire-employee-page').then((module) => module.HireEmployeePage),
  },
];
