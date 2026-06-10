import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AttendanceGateway } from './attendance-gateway';
import { officeId, roomId } from './booking';
import type { OccupancyDay } from './occupancy';

describe('AttendanceGateway.occupancy', () => {
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

  it('reads an office-scoped range and maps the day figures', () => {
    let received: OccupancyDay[] | undefined;

    gateway
      .occupancy({ officeId: officeId('o1') }, '2026-06-01', '2026-06-30')
      .subscribe((days) => (received = days));

    const request = httpController.expectOne((req) => req.url === '/occupancy');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('officeId')).toBe('o1');
    expect(request.request.params.get('from')).toBe('2026-06-01');
    expect(request.request.params.get('to')).toBe('2026-06-30');
    expect(request.request.params.has('roomId')).toBe(false);
    request.flush([
      {
        date: '2026-06-08',
        office: { officeId: 'o1', name: 'Munich', occupied: 3, capacity: 13, isFull: false },
        rooms: [{ roomId: 'r1', name: 'A1', occupied: 3, capacity: 8, isFull: false, occupants: null }],
      },
    ]);

    expect(received?.length).toBe(1);
    expect(received?.[0].office.name).toBe('Munich');
    expect(received?.[0].rooms[0].occupants).toBeUndefined();
  });

  it('reads a room-scoped range', () => {
    gateway.occupancy({ roomId: roomId('r1') }, '2026-06-08', '2026-06-08').subscribe();

    const request = httpController.expectOne((req) => req.url === '/occupancy');
    expect(request.request.params.get('roomId')).toBe('r1');
    expect(request.request.params.has('officeId')).toBe(false);
    request.flush([]);
  });
});
