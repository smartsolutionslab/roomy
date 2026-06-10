import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { Page } from '@roomy/shared-data-access';

import { AttendanceGateway } from './attendance-gateway';
import { officeId, reservationId, roomId } from './booking';
import type { BookableOffice, MyReservation, RoomAvailability } from './booking';

describe('AttendanceGateway', () => {
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

  it('lists the bookable catalogue, grouped into offices', () => {
    let received: BookableOffice[] | undefined;

    gateway.listBookableOffices().subscribe((offices) => (received = offices));

    const request = httpController.expectOne((req) => req.url === '/rooms');
    expect(request.request.method).toBe('GET');
    request.flush([
      { officeId: 'o1', officeName: 'Munich', roomId: 'r1', roomName: 'A1', capacity: 8 },
      { officeId: 'o1', officeName: 'Munich', roomId: 'r2', roomName: 'B1', capacity: 5 },
    ]);

    expect(received).toEqual([
      {
        id: 'o1',
        name: 'Munich',
        rooms: [
          { id: 'r1', name: 'A1', capacity: 8 },
          { id: 'r2', name: 'B1', capacity: 5 },
        ],
      },
    ]);
  });

  it('reads an office occupancy for one day as room availability', () => {
    let received: RoomAvailability[] | undefined;

    gateway.occupancyForOffice(officeId('o1'), '2026-06-08').subscribe((rooms) => (received = rooms));

    const request = httpController.expectOne((req) => req.url === '/occupancy');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('officeId')).toBe('o1');
    expect(request.request.params.get('from')).toBe('2026-06-08');
    expect(request.request.params.get('to')).toBe('2026-06-08');
    request.flush([
      {
        date: '2026-06-08',
        office: { officeId: 'o1', name: 'Munich', occupied: 8, capacity: 8, isFull: true },
        rooms: [{ roomId: 'r1', name: 'A1', occupied: 8, capacity: 8, isFull: true, occupants: null }],
      },
    ]);

    expect(received).toEqual([{ roomId: 'r1', occupied: 8, capacity: 8, isFull: true }]);
  });

  it('reserves a place and maps the new reservation id back', () => {
    let received: string | undefined;

    gateway
      .reserve(officeId('o1'), roomId('r1'), '2026-06-08')
      .subscribe((id) => (received = id));

    const request = httpController.expectOne((req) => req.url === '/reservations');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ officeId: 'o1', roomId: 'r1', date: '2026-06-08' });
    request.flush({
      reservationId: 'res1',
      officeId: 'o1',
      roomId: 'r1',
      date: '2026-06-08',
      employeeId: 'e1',
    });

    expect(received).toBe('res1');
  });

  it('lists my reservations, mapping the DTOs to branded view models', () => {
    let received: Page<MyReservation> | undefined;

    gateway.myReservations().subscribe((page) => (received = page));

    const request = httpController.expectOne((req) => req.url === '/reservations/mine');
    expect(request.request.method).toBe('GET');
    request.flush({
      items: [
        {
          reservationId: 'res1',
          officeId: 'o1',
          officeName: 'Munich',
          roomId: 'r1',
          roomName: 'A1',
          date: '2026-06-08',
        },
      ],
      nextCursor: 'next',
    });

    expect(received?.nextCursor).toBe('next');
    expect(received?.items).toEqual([
      {
        id: 'res1',
        officeId: 'o1',
        officeName: 'Munich',
        roomId: 'r1',
        roomName: 'A1',
        date: '2026-06-08',
      },
    ]);
  });

  it('cancels a reservation through its id with the day as a query parameter', () => {
    gateway.cancel(reservationId('res1'), '2026-06-08').subscribe();

    const request = httpController.expectOne((req) => req.url === '/reservations/res1');
    expect(request.request.method).toBe('DELETE');
    expect(request.request.params.get('date')).toBe('2026-06-08');
    request.flush(null);
  });
});
