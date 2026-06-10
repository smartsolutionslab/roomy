import { Routes } from '@angular/router';
import { adminGuard, authGuard, NavMeta } from '@roomy/shared-feature';

export const identityRoutes: Routes = [
  {
    // Reached from the account menu, not the navigation, so it carries no `data.nav`.
    path: 'account',
    canActivate: [authGuard],
    loadComponent: () => import('./account/account-page').then((module) => module.AccountPage),
  },
  {
    path: 'admin/users',
    canActivate: [authGuard, adminGuard],
    data: {
      nav: {
        labelKey: 'shell.adminLink',
        icon: 'admin',
        order: 70,
        descKey: 'home.cards.admin',
      } satisfies NavMeta,
    },
    loadComponent: () => import('./admin/admin-users-page').then((module) => module.AdminUsersPage),
  },
];
