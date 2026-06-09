import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AdminUser } from './admin-user';

// The administrator account-management surface through the gateway (ADR-0030), token-free (ADR-0013).
// Reads require the administrator role; the identity API answers a non-administrator with 403.
@Injectable({ providedIn: 'root' })
export class AdminUsersClient {
  private readonly httpClient = inject(HttpClient);

  getAll(): Observable<AdminUser[]> {
    return this.httpClient.get<AdminUser[]>('/admin/users');
  }

  grantAdministrator(userId: string): Observable<void> {
    return this.httpClient.post<void>(`/admin/users/${userId}:grant-administrator`, null);
  }
}
