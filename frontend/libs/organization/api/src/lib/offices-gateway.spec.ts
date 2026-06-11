import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import type { OfficeResponse, RoomResponse } from './generated';
import type { Office } from './office';
import { officeId, roomId } from './office';
import { OfficesGateway } from './offices-gateway';

const berlin: OfficeResponse = {
  id: '0199a0b0-0000-7000-8000-000000000010',
  name: 'Berlin',
  location: 'Berlin, DE',
  capacity: 8,
  rooms: [{ id: '0199a0b0-0000-7000-8000-000000000020', name: 'Sky', capacity: 8 }],
};

describe('OfficesGateway', () => {
  let gateway: OfficesGateway;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    gateway = TestBed.inject(OfficesGateway);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('lists offices, mapping the DTOs to branded view models', () => {
    let received: Office[] | undefined;

    gateway.listOffices().subscribe((offices) => (received = offices));

    const request = httpController.expectOne('/offices');
    expect(request.request.method).toBe('GET');
    request.flush([berlin]);

    expect(received).toEqual([
      {
        id: '0199a0b0-0000-7000-8000-000000000010',
        name: 'Berlin',
        location: 'Berlin, DE',
        capacity: 8,
        rooms: [{ id: '0199a0b0-0000-7000-8000-000000000020', name: 'Sky', capacity: 8 }],
      },
    ]);
  });

  it('creates an office and maps the created office back', () => {
    let received: Office | undefined;

    gateway.createOffice('Berlin', 'Berlin, DE').subscribe((office) => (received = office));

    const request = httpController.expectOne('/offices');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Berlin', location: 'Berlin, DE' });
    request.flush(berlin);

    expect(received?.name).toBe('Berlin');
  });

  it('renames an office through its name sub-resource', () => {
    gateway.renameOffice(officeId(berlin.id), 'Berlin HQ').subscribe();

    const request = httpController.expectOne(`/offices/${berlin.id}/name`);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ name: 'Berlin HQ' });
    request.flush({ ...berlin, name: 'Berlin HQ' });
  });

  it('changes an office location through its location sub-resource', () => {
    gateway.relocateOffice(officeId(berlin.id), 'Berlin, Mitte').subscribe();

    const request = httpController.expectOne(`/offices/${berlin.id}/location`);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ location: 'Berlin, Mitte' });
    request.flush({ ...berlin, location: 'Berlin, Mitte' });
  });

  it('adds a room and maps the created room back', () => {
    const room: RoomResponse = {
      id: '0199a0b0-0000-7000-8000-000000000021',
      name: 'Ground',
      capacity: 4,
    };
    let received;

    gateway.addRoom(officeId(berlin.id), 'Ground', 4).subscribe((created) => (received = created));

    const request = httpController.expectOne(`/offices/${berlin.id}/rooms`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Ground', capacity: 4 });
    request.flush(room);

    expect(received).toEqual({
      id: '0199a0b0-0000-7000-8000-000000000021',
      name: 'Ground',
      capacity: 4,
    });
  });

  it('renames a room and maps the refreshed office back', () => {
    let received: Office | undefined;

    gateway
      .renameRoom(officeId(berlin.id), roomId(berlin.rooms[0].id), 'Sky Lounge')
      .subscribe((office) => (received = office));

    const request = httpController.expectOne(
      `/offices/${berlin.id}/rooms/${berlin.rooms[0].id}/name`,
    );
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ name: 'Sky Lounge' });
    request.flush({
      ...berlin,
      rooms: [{ ...berlin.rooms[0], name: 'Sky Lounge' }],
    });

    expect(received?.rooms[0].name).toBe('Sky Lounge');
  });
});
