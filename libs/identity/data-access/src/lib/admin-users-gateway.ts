import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Page, mapPage } from '@roomy/shared-data-access';
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

  // One keyset-paginated page of accounts, ordered by email (GET /admin/users, admin-only; ADR-0044).
  // Absent cursor = the first page; the page carries the opaque nextCursor (null at the end).
  getAll(cursor?: string): Observable<Page<AdminUser>> {
    return listUsers(this.http, this.config.rootUrl, { cursor }).pipe(
      map((response) => mapPage(response.body, toAdminUser)),
    );
  }

  grantAdministrator(user: UserId): Observable<void> {
    return grantAdministrator(this.http, this.config.rootUrl, { userId: user }).pipe(
      map(() => undefined),
    );
  }
}
