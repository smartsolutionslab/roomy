import { Routes } from '@angular/router';

import { adminGuard } from './auth/admin.guard';
import { authGuard } from './auth/auth.guard';

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
  {
    path: 'not-authorized',
    canActivate: [authGuard],
    loadComponent: () => import('./admin/not-authorized').then((module) => module.NotAuthorized),
  },
];
