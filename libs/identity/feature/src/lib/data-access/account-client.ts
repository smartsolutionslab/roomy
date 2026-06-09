import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Account } from './account';

// Reads the signed-in account from the identity API through the gateway (`GET /account/me`) using
// a relative URL, so the call stays same-origin (ADR-0030). The BFF forwards the access token; the
// SPA never sees one (ADR-0013).
@Injectable({ providedIn: 'root' })
export class AccountClient {
  private readonly httpClient = inject(HttpClient);

  getCurrentAccount(): Observable<Account> {
    return this.httpClient.get<Account>('/account/me');
  }
}
