import { employeeId } from './booking';
import type { EmployeeId } from './booking';
import type { EmployeeResponse } from './generated';

export interface Employee {
  readonly id: EmployeeId;
  readonly name: string;
}

export function toEmployee(response: EmployeeResponse): Employee {
  return { id: employeeId(response.employeeId), name: response.name };
}
