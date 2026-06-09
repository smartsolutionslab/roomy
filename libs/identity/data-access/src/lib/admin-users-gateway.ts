import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { toAdminUser } from './admin-user';
import type { AdminUser } from './admin-user';
import { ApiConfiguration, grantAdministrator, listUsers } from './generated';
import type { UserId } from './user-id';

// The administrator account-management surface through the gateway using the generated client
// (ADR-0036), mapping trusted DTOs to branded domain types at this boundary (ADR-0020). Reads require
// the administrator role; the identity API answers a non-administrator with 403. Token-free (ADR-0013).
@Injectable({ providedIn: 'root' })
export class AdminUsersGateway {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfiguration);

  getAll(): Observable<AdminUser[]> {
    return listUsers(this.http, this.config.rootUrl).pipe(
      map((response) => response.body.map(toAdminUser)),
    );
  }

  grantAdministrator(user: UserId): Observable<void> {
    return grantAdministrator(this.http, this.config.rootUrl, { userId: user }).pipe(
      map(() => undefined),
    );
  }
}
