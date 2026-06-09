import { Routes } from '@angular/router';

export const identityRoutes: Routes = [
  {
    path: 'account',
    loadComponent: () => import('./account/account-page').then((module) => module.AccountPage),
  },
];
