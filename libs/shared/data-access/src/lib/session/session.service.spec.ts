import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { CurrentUser } from './current-user';
import { SessionService } from './session.service';

describe('SessionService', () => {
  let session: SessionService;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    session = TestBed.inject(SessionService);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('starts unloaded with no current user', () => {
    expect(session.loaded()).toBe(false);
    expect(session.currentUser()).toBeNull();
  });

  it('exposes the signed-in user and marks the session loaded on success', () => {
    const user: CurrentUser = { name: 'Ada Lovelace', roles: ['administrator'] };

    session.load();
    httpController.expectOne('/bff/user').flush(user);

    expect(session.currentUser()).toEqual(user);
    expect(session.loaded()).toBe(true);
  });

  it('treats a 401 as signed out but still marks the session loaded', () => {
    session.load();
    httpController
      .expectOne('/bff/user')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(session.currentUser()).toBeNull();
    expect(session.loaded()).toBe(true);
  });

  it('resolves ensureLoaded once the session has settled', async () => {
    const settled = session.ensureLoaded();
    httpController.expectOne('/bff/user').flush({ name: 'Ada', roles: [] });

    await settled;

    expect(session.loaded()).toBe(true);
  });

  it('loads the session once for concurrent callers', async () => {
    const first = session.ensureLoaded();
    const second = session.ensureLoaded();

    httpController.expectOne('/bff/user').flush({ name: 'Ada', roles: [] });
    await Promise.all([first, second]);

    expect(session.loaded()).toBe(true);
  });
});
