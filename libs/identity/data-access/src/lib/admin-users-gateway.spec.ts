import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { Page } from '@roomy/shared-data-access';

import type { AdminUser } from './admin-user';
import { AdminUsersGateway } from './admin-users-gateway';
import type { AdminUserResponse } from './generated';
import { userId } from './user-id';

describe('AdminUsersGateway', () => {
  let gateway: AdminUsersGateway;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    gateway = TestBed.inject(AdminUsersGateway);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('maps the admin user DTOs to branded domain users', () => {
    const dto: AdminUserResponse = {
      userId: 'a3f1c2d4-0000-7000-8000-000000000001',
      email: 'ada@roomy.test',
      displayName: 'Ada Lovelace',
      role: 'administrator',
      status: 'active',
    };
    let received: Page<AdminUser> | undefined;

    gateway.getAll().subscribe((page) => (received = page));

    const request = httpController.expectOne('/admin/users');
    expect(request.request.method).toBe('GET');
    request.flush({ items: [dto], nextCursor: 'next-page' });

    expect(received?.nextCursor).toBe('next-page');
    expect(received?.items).toEqual([
      {
        userId: 'a3f1c2d4-0000-7000-8000-000000000001',
        email: 'ada@roomy.test',
        displayName: 'Ada Lovelace',
        role: 'administrator',
        status: 'active',
      },
    ]);
  });

  it('forwards the cursor when loading a further page', () => {
    gateway.getAll('cursor-token').subscribe();

    const request = httpController.expectOne((candidate) => candidate.url === '/admin/users');
    expect(request.request.params.get('cursor')).toBe('cursor-token');
    request.flush({ items: [], nextCursor: null });
  });

  it('posts a grant to the user grant-administrator sub-resource', () => {
    const id = userId('a3f1c2d4-0000-7000-8000-000000000001');

    gateway.grantAdministrator(id).subscribe();

    const request = httpController.expectOne(
      '/admin/users/a3f1c2d4-0000-7000-8000-000000000001:grant-administrator',
    );
    expect(request.request.method).toBe('POST');
    request.flush(null);
  });
});
