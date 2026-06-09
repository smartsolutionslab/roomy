import { Routes } from '@angular/router';

import { authGuard } from './auth/auth.guard';

export const identityRoutes: Routes = [
  {
    path: 'account',
    canActivate: [authGuard],
    loadComponent: () => import('./account/account-page').then((module) => module.AccountPage),
  },
];
