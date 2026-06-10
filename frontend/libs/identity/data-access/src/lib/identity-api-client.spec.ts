import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Api, getCurrentAccount } from './generated';
import type { AccountResponse } from './generated';

// The client is generated from the identity OpenAPI spec (ADR-0036). This proves it is wired through
// the package barrel and calls the gateway with a relative, same-origin URL (rootUrl defaults to '',
// ADR-0030) — so the BFF forwards the token and the SPA never holds one (ADR-0013).
describe('generated identity API client', () => {
  let api: Api;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(Api);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('calls GET /account/me with a relative URL', async () => {
    const expected: AccountResponse = {
      userId: 'a3f1c2d4-0000-7000-8000-000000000001',
      email: 'ada@roomy.test',
      displayName: 'Ada Lovelace',
      role: 'administrator',
    };

    const pending = api.invoke(getCurrentAccount);

    const request = httpController.expectOne('/account/me');
    expect(request.request.method).toBe('GET');
    request.flush(expected);

    expect(await pending).toEqual(expected);
  });
});
