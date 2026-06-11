import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { toAccount } from './account';
import type { Account } from './account';
import { ApiConfiguration, getCurrentAccount } from './generated';

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
