import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AdminUser } from './admin-user';
import { AdminUsersClient } from './admin-users-client';

describe('AdminUsersClient', () => {
  let client: AdminUsersClient;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    client = TestBed.inject(AdminUsersClient);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('lists accounts from the gateway', () => {
    const accounts: AdminUser[] = [
      {
        userId: 'a3f1c2d4-0000-7000-8000-000000000001',
        email: 'ada@roomy.test',
        displayName: 'Ada Lovelace',
        role: 'administrator',
        status: 'active',
      },
    ];
    let received: AdminUser[] | undefined;

    client.getAll().subscribe((users) => (received = users));

    const request = httpController.expectOne('/admin/users');
    expect(request.request.method).toBe('GET');
    request.flush(accounts);
    expect(received).toEqual(accounts);
  });

  it('grants administrator to an account', () => {
    const userId = 'a3f1c2d4-0000-7000-8000-000000000002';
    let completed = false;

    client.grantAdministrator(userId).subscribe(() => (completed = true));

    const request = httpController.expectOne(`/admin/users/${userId}:grant-administrator`);
    expect(request.request.method).toBe('POST');
    request.flush(null);
    expect(completed).toBe(true);
  });
});
