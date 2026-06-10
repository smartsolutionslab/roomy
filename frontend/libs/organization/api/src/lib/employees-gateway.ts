import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { toHiredEmployee } from './employee';
import type { HireEmployeeDetails, HiredEmployee } from './employee';
import { ApiConfiguration, hireEmployee } from './generated';

// Hires colleagues through the gateway (`POST /employees`) using the generated client (ADR-0036), mapping
// the trusted DTO to a branded view model at this boundary (ADR-0020). The generated client defaults to a
// relative root URL, so the call stays same-origin (ADR-0030) and the BFF forwards the token — the SPA
// never sees one (ADR-0013). Hiring requires the administrator role; the gateway/API returns 403 otherwise.
@Injectable({ providedIn: 'root' })
export class EmployeesGateway {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfiguration);

  // Resolves on 202 Accepted: the employee is recorded and account provisioning has started. Convergence
  // to active/failed happens asynchronously and is not observed here (008 contract).
  hire(details: HireEmployeeDetails): Observable<HiredEmployee> {
    return hireEmployee(this.http, this.config.rootUrl, { body: details }).pipe(
      map((response) => toHiredEmployee(response.body)),
    );
  }
}
