import { TestBed } from '@angular/core/testing';
import { AttendanceGateway, BookableOffice, officeId, roomId } from '@roomy/attendance-api';
import { Subject, of, throwError } from 'rxjs';

import { BookableOfficesCatalogue, bookableOfficesCatalogue } from './bookable-offices-catalogue';

const munich: BookableOffice = {
  id: officeId('o1'),
  name: 'Munich',
  rooms: [{ id: roomId('r1'), name: 'A1', capacity: 8 }],
};

function create(gateway: Partial<AttendanceGateway>): BookableOfficesCatalogue {
  TestBed.configureTestingModule({
    providers: [{ provide: AttendanceGateway, useValue: gateway }],
  });
  return TestBed.runInInjectionContext(() => bookableOfficesCatalogue());
}

describe('bookableOfficesCatalogue', () => {
  it('loads the bookable offices on creation', () => {
    const catalogue = create({ listBookableOffices: () => of([munich]) });

    expect(catalogue.offices()).toEqual([munich]);
    expect(catalogue.loadFailed()).toBe(false);
  });

  it('leaves the offices unresolved while the load is in flight', () => {
    const pending = new Subject<BookableOffice[]>();
    const catalogue = create({ listBookableOffices: () => pending });

    expect(catalogue.offices()).toBeNull();
    expect(catalogue.loadFailed()).toBe(false);

    pending.next([munich]);
    pending.complete();

    expect(catalogue.offices()).toEqual([munich]);
  });

  it('flags a failure and falls back to an empty catalogue on error', () => {
    const catalogue = create({ listBookableOffices: () => throwError(() => new Error('boom')) });

    expect(catalogue.loadFailed()).toBe(true);
    expect(catalogue.offices()).toEqual([]);
  });

  it('reloads the offices on demand', () => {
    let attempt = 0;
    const catalogue = create({
      listBookableOffices: () =>
        attempt++ === 0 ? throwError(() => new Error('boom')) : of([munich]),
    });

    expect(catalogue.loadFailed()).toBe(true);

    catalogue.reload();

    expect(catalogue.offices()).toEqual([munich]);
    expect(catalogue.loadFailed()).toBe(false);
  });
});
