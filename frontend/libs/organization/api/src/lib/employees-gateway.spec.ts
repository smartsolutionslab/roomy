import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import type { HiredEmployee } from './employee';
import { EmployeesGateway } from './employees-gateway';

describe('EmployeesGateway', () => {
  let gateway: EmployeesGateway;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    gateway = TestBed.inject(EmployeesGateway);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('hires a colleague by posting the hire details to /employees', () => {
    gateway
      .hire({
        displayName: 'Ada Lovelace',
        email: 'ada@example.com',
        role: 'Employee',
        initialPassword: 'first-password',
      })
      .subscribe();

    const request = httpController.expectOne('/employees');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      displayName: 'Ada Lovelace',
      email: 'ada@example.com',
      role: 'Employee',
      initialPassword: 'first-password',
    });
    request.flush(
      { employeeId: '0199a0b0-0000-7000-8000-000000000001', userId: '0199a0b0-0000-7000-8000-000000000002', state: 'Provisioning' },
      { status: 202, statusText: 'Accepted' },
    );
  });

  it('maps the 202 response to a branded hired-employee view model', () => {
    let received: HiredEmployee | undefined;

    gateway
      .hire({
        displayName: 'Grace Hopper',
        email: 'grace@example.com',
        role: 'Administrator',
        initialPassword: 'first-password',
      })
      .subscribe((hired) => (received = hired));

    httpController.expectOne('/employees').flush(
      { employeeId: '0199a0b0-0000-7000-8000-0000000000aa', userId: '0199a0b0-0000-7000-8000-0000000000bb', state: 'Provisioning' },
      { status: 202, statusText: 'Accepted' },
    );

    expect(received).toEqual({
      employeeId: '0199a0b0-0000-7000-8000-0000000000aa',
      userId: '0199a0b0-0000-7000-8000-0000000000bb',
      state: 'Provisioning',
    });
  });

  it('carries the chosen administrator role in the request body', () => {
    gateway
      .hire({
        displayName: 'Grace Hopper',
        email: 'grace@example.com',
        role: 'Administrator',
        initialPassword: 'first-password',
      })
      .subscribe();

    const request = httpController.expectOne('/employees');
    expect(request.request.body.role).toBe('Administrator');
    request.flush(
      { employeeId: '0199a0b0-0000-7000-8000-0000000000aa', userId: '0199a0b0-0000-7000-8000-0000000000bb', state: 'Provisioning' },
      { status: 202, statusText: 'Accepted' },
    );
  });
});
