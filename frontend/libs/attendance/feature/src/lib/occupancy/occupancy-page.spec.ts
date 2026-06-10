import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import {
  AttendanceGateway,
  BookableOffice,
  OccupancyDay,
  OfficeId,
  RoomId,
  officeId,
  roomId,
} from '@roomy/attendance-api';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Observable, of, throwError } from 'rxjs';

import { importAttendanceTestTransloco } from '../../testing/transloco';

import { OccupancyPage } from './occupancy-page';

const munich: BookableOffice = {
  id: officeId('o1'),
  name: 'Munich',
  rooms: [
    { id: roomId('r1'), name: 'A1', capacity: 8 },
    { id: roomId('r2'), name: 'B1', capacity: 5 },
  ],
};

const todayDay: OccupancyDay = {
  date: '2026-06-08',
  office: { officeId: officeId('o1'), name: 'Munich', occupied: 9, capacity: 13, isFull: false },
  rooms: [
    { roomId: roomId('r1'), name: 'A1', occupied: 8, capacity: 8, isFull: true, occupants: [{ employeeId: 'e1', name: 'Ada' }] },
    { roomId: roomId('r2'), name: 'B1', occupied: 1, capacity: 5, isFull: false, occupants: [{ employeeId: 'e2', name: 'Ben' }] },
  ],
};

const futureDay: OccupancyDay = {
  date: '2026-06-15',
  office: { officeId: officeId('o1'), name: 'Munich', occupied: 2, capacity: 13, isFull: false },
  rooms: [
    { roomId: roomId('r1'), name: 'A1', occupied: 2, capacity: 8, isFull: false, occupants: undefined },
    { roomId: roomId('r2'), name: 'B1', occupied: 0, capacity: 5, isFull: false, occupants: undefined },
  ],
};

interface Stub {
  list?: () => Observable<BookableOffice[]>;
  occupancy?: (
    scope: { officeId?: OfficeId; roomId?: RoomId },
    from: string,
    to: string,
  ) => Observable<OccupancyDay[]>;
}

function renderPage(offices: BookableOffice[], stub: Stub = {}) {
  const gateway = {
    listBookableOffices: stub.list ?? (() => of(offices)),
    occupancy: stub.occupancy ?? (() => of([todayDay])),
    occupancyForOffice: () => of([]),
    reserve: () => of(undefined),
    myReservations: () => of({ items: [], nextCursor: null }),
    cancel: () => of(undefined),
  };

  return render(OccupancyPage, {
    imports: [importAttendanceTestTransloco()],
    inputs: { today: '2026-06-08' },
    providers: [provideZonelessChangeDetection(), { provide: AttendanceGateway, useValue: gateway }],
  });
}

describe('OccupancyPage', () => {
  it('shows the office rollup and per-room figures for the chosen office and day', async () => {
    const user = userEvent.setup();
    const calls: { scope: { officeId?: OfficeId; roomId?: RoomId }; from: string; to: string }[] = [];
    await renderPage([munich], {
      occupancy: (scope, from, to) => {
        calls.push({ scope, from, to });
        return of([todayDay]);
      },
    });

    await user.selectOptions(await screen.findByLabelText('Office'), 'o1');

    expect(calls.at(-1)).toEqual({ scope: { officeId: 'o1' }, from: '2026-06-08', to: '2026-06-08' });
    expect(await screen.findByText('Office: 9 of 13 occupied')).toBeTruthy();
    expect(screen.getByText('8 of 8 occupied')).toBeTruthy();
    expect(screen.getByText('1 of 5 occupied')).toBeTruthy();
    expect(screen.getByText('Full')).toBeTruthy();
  });

  it('queries a room scope when a specific room is chosen', async () => {
    const user = userEvent.setup();
    const calls: { scope: { officeId?: OfficeId; roomId?: RoomId } }[] = [];
    await renderPage([munich], {
      occupancy: (scope) => {
        calls.push({ scope });
        return of([todayDay]);
      },
    });

    await user.selectOptions(await screen.findByLabelText('Office'), 'o1');
    await user.selectOptions(screen.getByLabelText('Room'), 'r2');

    expect(calls.at(-1)?.scope).toEqual({ roomId: 'r2' });
  });

  it('requests a whole-month range for the month preset and lists every returned day', async () => {
    const user = userEvent.setup();
    const calls: { from: string; to: string }[] = [];
    await renderPage([munich], {
      occupancy: (_scope, from, to) => {
        calls.push({ from, to });
        return of([todayDay, futureDay]);
      },
    });

    await user.selectOptions(await screen.findByLabelText('Office'), 'o1');
    await user.selectOptions(screen.getByLabelText('Range'), 'month');

    expect(calls.at(-1)).toEqual({ from: '2026-06-01', to: '2026-06-30' });
    expect(await screen.findByText('2026-06-08')).toBeTruthy();
    expect(screen.getByText('2026-06-15')).toBeTruthy();
  });

  it('shows occupant names only where the response carries them', async () => {
    const user = userEvent.setup();
    await renderPage([munich], { occupancy: () => of([todayDay, futureDay]) });

    await user.selectOptions(await screen.findByLabelText('Office'), 'o1');

    expect(await screen.findByText('Ada')).toBeTruthy();
    expect(screen.getByText('Ben')).toBeTruthy();
    // Only today's two rooms carry occupants; the future day shows counts only.
    expect(screen.getAllByText('Booked').length).toBe(2);
  });

  it('shows an empty state when nothing is in the catalogue', async () => {
    await renderPage([]);

    expect(await screen.findByText('No offices or rooms are available yet.')).toBeTruthy();
  });

  it('surfaces a localized message when the scope is unknown', async () => {
    const user = userEvent.setup();
    await renderPage([munich], {
      occupancy: () => throwError(() => new HttpErrorResponse({ status: 404, error: { code: 'unknown_office' } })),
    });

    await user.selectOptions(await screen.findByLabelText('Office'), 'o1');

    expect(await screen.findByText('That office or room is no longer available.')).toBeTruthy();
  });
});
