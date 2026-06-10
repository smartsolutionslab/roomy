import { Routes } from '@angular/router';
import { adminGuard, authGuard, NavMeta } from '@roomy/shared-feature';

// The attendance section is self-service for any signed-in employee (FR-009): authGuard only, never
// adminGuard. Reserve is the default; my-reservations hosts cancel/change (AT-4/AT-5); occupancy and
// calendar are the read-only views (008, OC-1..4/6). Each navigable route carries its sidebar/dashboard
// presentation in `data.nav`; visibility follows the guard above (ADR-0050).
export const attendanceRoutes: Routes = [
  {
    path: 'reserve',
    canActivate: [authGuard],
    data: {
      nav: {
        labelKey: 'shell.reserveLink',
        icon: 'reserve',
        order: 10,
        descKey: 'home.cards.reserve',
      } satisfies NavMeta,
    },
    loadComponent: () => import('./reserve/reserve-page').then((module) => module.ReservePage),
  },
  {
    path: 'mine',
    canActivate: [authGuard],
    data: {
      nav: {
        labelKey: 'shell.myReservationsLink',
        icon: 'my-reservations',
        order: 20,
        descKey: 'home.cards.mine',
      } satisfies NavMeta,
    },
    loadComponent: () =>
      import('./my-reservations/my-reservations-page').then((module) => module.MyReservationsPage),
  },
  {
    path: 'occupancy',
    canActivate: [authGuard],
    data: {
      nav: {
        labelKey: 'shell.occupancyLink',
        icon: 'occupancy',
        order: 30,
        descKey: 'home.cards.occupancy',
      } satisfies NavMeta,
    },
    loadComponent: () =>
      import('./occupancy/occupancy-page').then((module) => module.OccupancyPage),
  },
  {
    path: 'calendar',
    canActivate: [authGuard],
    data: {
      nav: {
        labelKey: 'shell.calendarLink',
        icon: 'calendar',
        order: 40,
        descKey: 'home.cards.calendar',
      } satisfies NavMeta,
    },
    loadComponent: () =>
      import('./occupancy/occupancy-calendar').then((module) => module.OccupancyCalendar),
  },
  {
    path: 'on-behalf',
    canActivate: [authGuard, adminGuard],
    data: {
      nav: {
        labelKey: 'shell.onBehalfLink',
        icon: 'on-behalf',
        order: 50,
        descKey: 'home.cards.onBehalf',
      } satisfies NavMeta,
    },
    loadComponent: () => import('./on-behalf/on-behalf-page').then((module) => module.OnBehalfPage),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'reserve',
  },
];
