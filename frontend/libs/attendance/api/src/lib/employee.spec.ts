import { toEmployee } from './employee';
import type { EmployeeResponse } from './generated';

describe('toEmployee', () => {
  it('maps the employee DTO to the branded view model', () => {
    const dto: EmployeeResponse = { employeeId: 'e1', name: 'Ada' };

    expect(toEmployee(dto)).toEqual({ id: 'e1', name: 'Ada' });
  });
});
