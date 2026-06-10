import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { toAccount } from './account';
import type { Account } from './account';
import { ApiConfiguration, getCurrentAccount } from './generated';

// Reads the signed-in account through the gateway (`GET /account/me`) using the generated client
// (ADR-0036), then maps the trusted DTO to the branded domain type at this boundary (ADR-0020). The
// generated client defaults to a relative root URL, so the call stays same-origin (ADR-0030) and the
// BFF forwards the token — the SPA never sees one (ADR-0013).
@Injectable({ providedIn: 'root' })
export class AccountGateway {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfiguration);

  getCurrentAccount(): Observable<Account> {
    return getCurrentAccount(this.http, this.config.rootUrl).pipe(
      map((response) => toAccount(response.body)),
    );
  }
}
