import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { Page } from '@roomy/shared-data-access';

import { AttendanceGateway } from './attendance-gateway';
import { employeeId, officeId, roomId } from './booking';
import type { MyReservation } from './booking';
import type { Employee } from './employee';

describe('AttendanceGateway on-behalf', () => {
  let gateway: AttendanceGateway;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    gateway = TestBed.inject(AttendanceGateway);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('lists the employee directory, mapping the DTOs to branded view models', () => {
    let received: Page<Employee> | undefined;

    gateway.listEmployees().subscribe((page) => (received = page));

    const request = httpController.expectOne((req) => req.url === '/reservations/employees');
    expect(request.request.method).toBe('GET');
    request.flush({ items: [{ employeeId: 'e1', name: 'Ada' }], nextCursor: 'more' });

    expect(received?.nextCursor).toBe('more');
    expect(received?.items).toEqual([{ id: 'e1', name: 'Ada' }]);
  });

  it('forwards the cursor when paging the directory', () => {
    gateway.listEmployees('cursor-token').subscribe();

    const request = httpController.expectOne((req) => req.url === '/reservations/employees');
    expect(request.request.params.get('cursor')).toBe('cursor-token');
    request.flush({ items: [], nextCursor: null });
  });

  it("reads a chosen employee's reservations", () => {
    let received: Page<MyReservation> | undefined;

    gateway.reservationsFor(employeeId('e1')).subscribe((page) => (received = page));

    const request = httpController.expectOne((req) => req.url === '/reservations/by-employee/e1');
    expect(request.request.method).toBe('GET');
    request.flush({
      items: [
        {
          reservationId: 'res1',
          officeId: 'o1',
          officeName: 'Munich',
          roomId: 'r1',
          roomName: 'A1',
          date: '2026-06-10',
        },
      ],
      nextCursor: null,
    });

    expect(received?.nextCursor).toBeNull();
    expect(received?.items[0]).toEqual({
      id: 'res1',
      officeId: 'o1',
      officeName: 'Munich',
      roomId: 'r1',
      roomName: 'A1',
      date: '2026-06-10',
    });
  });

  it('reserves on behalf of an employee, sending onBehalfOf in the body', () => {
    gateway
      .reserve(officeId('o1'), roomId('r1'), '2026-06-10', employeeId('e1'))
      .subscribe();

    const request = httpController.expectOne((req) => req.url === '/reservations');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      officeId: 'o1',
      roomId: 'r1',
      date: '2026-06-10',
      onBehalfOf: 'e1',
    });
    request.flush({ reservationId: 'res1', officeId: 'o1', roomId: 'r1', date: '2026-06-10', employeeId: 'e1' });
  });
});
