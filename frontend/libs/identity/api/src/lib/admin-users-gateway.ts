import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Page, mapPage } from '@roomy/shared-data-access';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { toAdminUser } from './admin-user';
import type { AdminUser } from './admin-user';
import { ApiConfiguration, grantAdministrator, listUsers } from './generated';
import type { UserId } from './user-id';

@Injectable({ providedIn: 'root' })
export class AdminUsersGateway {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfiguration);

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
