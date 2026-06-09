import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import type { Account } from './account';
import { AccountGateway } from './account-gateway';
import type { AccountResponse } from './generated';

describe('AccountGateway', () => {
  let gateway: AccountGateway;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    gateway = TestBed.inject(AccountGateway);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('maps the account DTO to the branded domain account', () => {
    const dto: AccountResponse = {
      userId: 'a3f1c2d4-0000-7000-8000-000000000001',
      email: 'ada@roomy.test',
      displayName: 'Ada Lovelace',
      role: 'administrator',
    };
    let received: Account | undefined;

    gateway.getCurrentAccount().subscribe((account) => (received = account));

    const request = httpController.expectOne('/account/me');
    expect(request.request.method).toBe('GET');
    request.flush(dto);

    expect(received).toEqual({
      userId: 'a3f1c2d4-0000-7000-8000-000000000001',
      email: 'ada@roomy.test',
      displayName: 'Ada Lovelace',
      role: 'administrator',
    });
  });
});
