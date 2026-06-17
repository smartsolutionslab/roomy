import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { catchError, of } from 'rxjs';

import { CurrentUser } from './current-user';

// Reads the current session from the BFF (`GET /bff/user`) over a relative, same-origin URL. A 401
// means no session and is surfaced as `null`; the SPA never receives or handles a token. `loaded`
// flips true once the first response settles (success or 401) so route guards can wait for a decided session.
@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly httpClient = inject(HttpClient);
  private readonly currentUserState = signal<CurrentUser | null>(null);
  private readonly loadedState = signal(false);
  private loadOnce?: Promise<void>;

  readonly currentUser = this.currentUserState.asReadonly();
  readonly loaded = this.loadedState.asReadonly();
  readonly isAdministrator = computed(
    () => this.currentUserState()?.roles.includes('administrator') ?? false,
  );

  load(): void {
    void this.ensureLoaded();
  }

  // Resolves once the first `/bff/user` response settles (success or 401), so route guards can
  // wait for a decided session before allowing or redirecting. Memoized: concurrent callers and
  // the startup `load()` share the one request.
  ensureLoaded(): Promise<void> {
    return (this.loadOnce ??= new Promise<void>((resolve) => {
      this.httpClient
        .get<CurrentUser>('/bff/user')
        .pipe(catchError(() => of(null)))
        .subscribe((user) => {
          this.currentUserState.set(user);
          this.loadedState.set(true);
          resolve();
        });
    }));
  }
}
