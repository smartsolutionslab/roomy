import { Routes } from '@angular/router';
import { authGuard } from '@roomy/shared-feature';

// The attendance section is self-service for any signed-in employee (FR-009): authGuard only, never
// adminGuard. Reserve is the default; my-reservations hosts cancel/change (AT-4/AT-5).
export const attendanceRoutes: Routes = [
  {
    path: 'reserve',
    canActivate: [authGuard],
    loadComponent: () => import('./reserve/reserve-page').then((module) => module.ReservePage),
  },
  {
    path: 'mine',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./my-reservations/my-reservations-page').then((module) => module.MyReservationsPage),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'reserve',
  },
];
