import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { toHiredEmployee } from './employee';
import type { HireEmployeeDetails, HiredEmployee } from './employee';
import { ApiConfiguration, hireEmployee } from './generated';

@Injectable({ providedIn: 'root' })
export class EmployeesGateway {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfiguration);

  // Resolves on 202: the employee is recorded and provisioning has started; convergence is async and not observed here.
  hire(details: HireEmployeeDetails): Observable<HiredEmployee> {
    return hireEmployee(this.http, this.config.rootUrl, { body: details }).pipe(
      map((response) => toHiredEmployee(response.body)),
    );
  }
}
