import { provideZonelessChangeDetection } from '@angular/core';
import {
  AttendanceGateway,
  BookableOffice,
  MyReservation,
  OccupancyDay,
  OfficeId,
  RoomId,
  officeId,
  reservationId,
  roomId,
} from '@roomy/attendance-api';
import type { Page } from '@roomy/shared-data-access';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Observable, of } from 'rxjs';

import { importAttendanceTestTransloco } from '../../testing/transloco';

import { OccupancyCalendar } from './occupancy-calendar';

const munich: BookableOffice = {
  id: officeId('o1'),
  name: 'Munich',
  rooms: [{ id: roomId('r1'), name: 'A1', capacity: 8 }],
};

const june10: OccupancyDay = {
  date: '2026-06-10',
  office: { officeId: officeId('o1'), name: 'Munich', occupied: 5, capacity: 13, isFull: false },
  rooms: [
    {
      roomId: roomId('r1'),
      name: 'A1',
      occupied: 5,
      capacity: 8,
      isFull: false,
      occupants: undefined,
    },
  ],
};

const myJune10: MyReservation = {
  id: reservationId('res1'),
  officeId: officeId('o1'),
  officeName: 'Munich',
  roomId: roomId('r1'),
  roomName: 'A1',
  date: '2026-06-10',
};

interface Stub {
  occupancy?: (
    scope: { officeId?: OfficeId; roomId?: RoomId },
    from: string,
    to: string,
  ) => Observable<OccupancyDay[]>;
  mine?: (cursor?: string) => Observable<Page<MyReservation>>;
}

function reservationPage(
  items: MyReservation[],
  nextCursor: string | null = null,
): Page<MyReservation> {
  return { items, nextCursor };
}

function renderPage(stub: Stub = {}) {
  const gateway = {
    listBookableOffices: () => of([munich]),
    occupancy: stub.occupancy ?? (() => of([june10])),
    occupancyForOffice: () => of([]),
    reserve: () => of(undefined),
    myReservations: stub.mine ?? (() => of(reservationPage([myJune10]))),
    cancel: () => of(undefined),
  };

  return render(OccupancyCalendar, {
    imports: [importAttendanceTestTransloco()],
    inputs: { today: '2026-06-10' },
    providers: [
      provideZonelessChangeDetection(),
      { provide: AttendanceGateway, useValue: gateway },
    ],
  });
}

describe('OccupancyCalendar', () => {
  it('renders the month grid with localized month and weekday headers', async () => {
    const user = userEvent.setup();
    await renderPage();

    await user.click(await screen.findByRole('button', { name: 'Munich' }));

    expect(await screen.findByRole('heading', { name: 'June 2026' })).toBeTruthy();
    expect(screen.getByRole('columnheader', { name: 'Mon' })).toBeTruthy();
    expect(screen.getByRole('columnheader', { name: 'Sun' })).toBeTruthy();
  });

  it("shows a day's occupancy figure and highlights the viewer's booked day", async () => {
    const user = userEvent.setup();
    const calls: { from: string; to: string }[] = [];
    await renderPage({
      occupancy: (_scope, from, to) => {
        calls.push({ from, to });
        return of([june10]);
      },
    });

    await user.click(await screen.findByRole('button', { name: 'Munich' }));

    expect(calls.at(-1)).toEqual({ from: '2026-06-01', to: '2026-06-30' });
    expect(await screen.findByText('5/13')).toBeTruthy();
    expect(screen.getByText('You have a reservation')).toBeTruthy();
  });

  it('re-queries when navigating to the next month', async () => {
    const user = userEvent.setup();
    const calls: { from: string; to: string }[] = [];
    await renderPage({
      occupancy: (_scope, from, to) => {
        calls.push({ from, to });
        return of([]);
      },
    });

    await user.click(await screen.findByRole('button', { name: 'Munich' }));
    await user.click(screen.getByRole('button', { name: 'Next month' }));

    expect(calls.at(-1)).toEqual({ from: '2026-07-01', to: '2026-07-31' });
    expect(await screen.findByRole('heading', { name: 'July 2026' })).toBeTruthy();
  });
});
