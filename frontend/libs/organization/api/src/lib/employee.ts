import type { Brand } from '@roomy/util';

import type { HiredEmployeeResponse } from './generated';

// Backend uuids are trusted, so these mint the brand without re-validating.
export type EmployeeId = Brand<string, 'EmployeeId'>;
export const employeeId = (value: string): EmployeeId => value as EmployeeId;

export type UserId = Brand<string, 'UserId'>;
export const userId = (value: string): UserId => value as UserId;

export type EmployeeRole = 'Employee' | 'Administrator';

// initialPassword is a transient secret sent once to seed the login; it is never stored client-side.
export interface HireEmployeeDetails {
  readonly displayName: string;
  readonly email: string;
  readonly role: EmployeeRole;
  readonly initialPassword: string;
}

// state is "Provisioning" right after a hire; convergence to active/failed is async and not observed here.
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
