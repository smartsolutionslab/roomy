import { Routes } from '@angular/router';
import { adminGuard, authGuard } from '@roomy/shared-feature';

export const identityRoutes: Routes = [
  {
    path: 'account',
    canActivate: [authGuard],
    loadComponent: () => import('./account/account-page').then((module) => module.AccountPage),
  },
  {
    path: 'admin/users',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./admin/admin-users-page').then((module) => module.AdminUsersPage),
  },
];
