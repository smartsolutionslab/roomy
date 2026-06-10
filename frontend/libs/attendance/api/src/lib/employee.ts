import { employeeId } from './booking';
import type { EmployeeId } from './booking';
import type { EmployeeResponse } from './generated';

// An employee an administrator may act on behalf of (009): the branded id and the display name to choose
// by. Mapped from the trusted generated DTO at the data-access boundary (ADR-0020).
export interface Employee {
  readonly id: EmployeeId;
  readonly name: string;
}

export function toEmployee(response: EmployeeResponse): Employee {
  return { id: employeeId(response.employeeId), name: response.name };
}
