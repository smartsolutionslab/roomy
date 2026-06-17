import { computed, provideZonelessChangeDetection, signal, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Routes } from '@angular/router';
import { CurrentUser, SessionService } from '@roomy/shared-data-access';

import { adminGuard } from '../auth/admin.guard';
import { authGuard } from '../auth/auth.guard';

import { NavigationService } from './navigation.service';

// A representative config: a self-service route, an admin-guarded route, a route guarded only via an
// admin-guarded ancestor (inheritance), a nested child under a parent path, and a route with no nav
// metadata that must never surface. Declaration order is deliberately not display order.
const routes: Routes = [
  {
    path: 'account',
    canActivate: [authGuard],
    children: [],
  },
  {
    path: 'offices',
    canActivate: [authGuard, adminGuard],
    data: { nav: { labelKey: 'shell.officesLink', icon: 'offices', order: 30 } },
    children: [],
  },
  {
    path: 'attendance',
    children: [
      {
        path: 'reserve',
        canActivate: [authGuard],
        data: { nav: { labelKey: 'shell.reserveLink', icon: 'reserve', order: 10 } },
        children: [],
      },
    ],
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    children: [
      {
        path: 'reports',
        canActivate: [authGuard],
        data: { nav: { labelKey: 'shell.adminLink', icon: 'admin', order: 20 } },
        children: [],
      },
    ],
  },
];

function setup(user: CurrentUser | null) {
  const currentUser: WritableSignal<CurrentUser | null> = signal(user);
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideRouter(routes),
      {
        provide: SessionService,
        useValue: {
          currentUser,
          isAdministrator: computed(() => currentUser()?.roles.includes('administrator') ?? false),
        },
      },
    ],
  });
  return { service: TestBed.inject(NavigationService), currentUser };
}

describe('NavigationService', () => {
  it('builds entries only for routes that declare nav metadata', () => {
    const { service } = setup({ name: 'Ada', roles: ['employee', 'administrator'] });

    const labels = service.items().map((item) => item.labelKey);
    expect(labels).not.toContain('account');
    expect(service.items().every((item) => item.labelKey.length > 0)).toBe(true);
  });

  it('resolves nested routes to their full path', () => {
    const { service } = setup({ name: 'Ada', roles: ['employee'] });

    const reserve = service.items().find((item) => item.labelKey === 'shell.reserveLink');
    expect(reserve?.path).toBe('/attendance/reserve');
  });

  it('orders entries by their declared order, not declaration order', () => {
    const { service } = setup({ name: 'Ada', roles: ['employee', 'administrator'] });

    expect(service.items().map((item) => item.labelKey)).toEqual([
      'shell.reserveLink', // order 10
      'shell.adminLink', // order 20
      'shell.officesLink', // order 30
    ]);
  });

  it('infers requiresAdmin from a route guarded by adminGuard', () => {
    const { service } = setup({ name: 'Ada', roles: ['employee', 'administrator'] });

    const offices = service.items().find((item) => item.labelKey === 'shell.officesLink');
    expect(offices?.requiresAdmin).toBe(true);
  });

  it('inherits requiresAdmin from an admin-guarded ancestor route', () => {
    const { service } = setup({ name: 'Ada', roles: ['employee', 'administrator'] });

    const reports = service.items().find((item) => item.labelKey === 'shell.adminLink');
    expect(reports?.requiresAdmin).toBe(true);
  });

  it('treats a route with no admin guard as available to any signed-in user', () => {
    const { service } = setup({ name: 'Ada', roles: ['employee'] });

    const reserve = service.items().find((item) => item.labelKey === 'shell.reserveLink');
    expect(reserve?.requiresAdmin).toBe(false);
  });

  it('hides administrator-only entries from a non-administrator', () => {
    const { service } = setup({ name: 'Grace', roles: ['employee'] });

    const labels = service.items().map((item) => item.labelKey);
    expect(labels).toEqual(['shell.reserveLink']);
    expect(service.adminItems()).toEqual([]);
  });

  it('shows administrator-only entries to an administrator', () => {
    const { service } = setup({ name: 'Ada', roles: ['employee', 'administrator'] });

    expect(service.mainItems().map((item) => item.labelKey)).toEqual(['shell.reserveLink']);
    expect(service.adminItems().map((item) => item.labelKey)).toEqual([
      'shell.adminLink',
      'shell.officesLink',
    ]);
  });

  it('reacts to the session: admin entries appear once an administrator signs in', () => {
    const { service, currentUser } = setup({ name: 'Grace', roles: ['employee'] });

    expect(service.adminItems()).toEqual([]);

    currentUser.set({ name: 'Ada', roles: ['employee', 'administrator'] });
    expect(service.adminItems().map((item) => item.labelKey)).toEqual([
      'shell.adminLink',
      'shell.officesLink',
    ]);
  });
});
