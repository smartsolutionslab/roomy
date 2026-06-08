import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { catchError, of } from 'rxjs';

import { CurrentUser } from './current-user';

// Reads the current session from the BFF (`GET /bff/user`) using a relative URL, so the call
// stays same-origin through the gateway (ADR-0030). A 401 means there is no session and is
// surfaced as `null`; the SPA never receives or handles a token (ADR-0013).
@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly httpClient = inject(HttpClient);
  private readonly currentUserState = signal<CurrentUser | null>(null);

  readonly currentUser = this.currentUserState.asReadonly();

  load(): void {
    this.httpClient
      .get<CurrentUser>('/bff/user')
      .pipe(catchError(() => of(null)))
      .subscribe((user) => this.currentUserState.set(user));
  }
}
