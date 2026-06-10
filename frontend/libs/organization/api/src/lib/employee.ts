import type { Brand } from '@roomy/util';

import type { HiredEmployeeResponse } from './generated';

// Branded identifiers so a bare string cannot be passed where an employee/user identifier is expected.
// Backend DTOs are trusted (ADR-0020) and the contract types these as uuids, so the value is not
// re-validated here — the helpers only mint the brand at the data-access boundary.
export type EmployeeId = Brand<string, 'EmployeeId'>;
export const employeeId = (value: string): EmployeeId => value as EmployeeId;

export type UserId = Brand<string, 'UserId'>;
export const userId = (value: string): UserId => value as UserId;

// The two roles a colleague can be hired into (008 contract: role ∈ "Employee" | "Administrator").
export type EmployeeRole = 'Employee' | 'Administrator';

// What an administrator supplies to hire a colleague. `initialPassword` is a transient secret sent once
// to seed the login credential (008 FR-009); it is never stored client-side.
export interface HireEmployeeDetails {
  readonly displayName: string;
  readonly email: string;
  readonly role: EmployeeRole;
  readonly initialPassword: string;
}

// The boundary view model of a successful (202) hire: the employee is recorded and provisioning has
// started. `state` is "Provisioning" — convergence to active/failed is asynchronous and not observed here.
export interface HiredEmployee {
  readonly employeeId: EmployeeId;
  readonly userId: UserId;
  readonly state: string;
}

export function toHiredEmployee(response: HiredEmployeeResponse): HiredEmployee {
  return {
    employeeId: employeeId(response.employeeId),
    userId: userId(response.userId),
    state: response.state,
  };
}
