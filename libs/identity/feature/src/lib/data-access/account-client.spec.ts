import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Account } from './account';
import { AccountClient } from './account-client';

describe('AccountClient', () => {
  let client: AccountClient;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    client = TestBed.inject(AccountClient);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('requests the signed-in account from the gateway with a relative URL', () => {
    const expected: Account = {
      userId: 'a3f1c2d4-0000-7000-8000-000000000001',
      email: 'ada@roomy.test',
      displayName: 'Ada Lovelace',
      role: 'administrator',
    };
    let received: Account | undefined;

    client.getCurrentAccount().subscribe((account) => (received = account));

    const request = httpController.expectOne('/account/me');
    expect(request.request.method).toBe('GET');
    request.flush(expected);
    expect(received).toEqual(expected);
  });
});
