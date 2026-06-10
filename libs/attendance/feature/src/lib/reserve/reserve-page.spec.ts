import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import {
  AttendanceGateway,
  BookableOffice,
  OfficeId,
  ReservationId,
  RoomAvailability,
  RoomId,
  employeeId,
  officeId,
  reservationId,
  roomId,
} from '@roomy/attendance-data-access';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Observable, of, throwError } from 'rxjs';

import { importAttendanceTestTransloco } from '../../testing/transloco';

import { ReservePage } from './reserve-page';

const munich: BookableOffice = {
  id: officeId('o1'),
  name: 'Munich',
  rooms: [
    { id: roomId('r1'), name: 'A1', capacity: 8 },
    { id: roomId('r2'), name: 'B1', capacity: 5 },
  ],
};

const allFree: RoomAvailability[] = [
  { roomId: roomId('r1'), occupied: 0, capacity: 8, isFull: false },
  { roomId: roomId('r2'), occupied: 0, capacity: 5, isFull: false },
];

interface Stub {
  list?: () => Observable<BookableOffice[]>;
  occupancy?: (office: OfficeId, day: string) => Observable<RoomAvailability[]>;
  reserve?: (office: OfficeId, room: RoomId, date: string) => Observable<ReservationId>;
}

function renderPage(offices: BookableOffice[], stub: Stub = {}) {
  const gateway = {
    listBookableOffices: stub.list ?? (() => of(offices)),
    occupancyForOffice: stub.occupancy ?? (() => of<RoomAvailability[]>([])),
    reserve: stub.reserve ?? (() => of(reservationId('res1'))),
    myReservations: () => of([]),
    cancel: () => of(undefined),
  };

  return render(ReservePage, {
    imports: [importAttendanceTestTransloco()],
    inputs: { today: '2026-06-08' },
    providers: [provideZonelessChangeDetection(), { provide: AttendanceGateway, useValue: gateway }],
  });
}

describe('ReservePage', () => {
  it('lists the bookable offices in the office picker', async () => {
    await renderPage([munich]);

    expect(await screen.findByRole('option', { name: 'Munich' })).toBeTruthy();
  });

  it('shows an empty state when nothing is bookable', async () => {
    await renderPage([]);

    expect(
      await screen.findByText('No offices or rooms are available to book yet.'),
    ).toBeTruthy();
  });

  it('offers only bookable days — today is offered, the weekend is not', async () => {
    await renderPage([munich]);

    expect(await screen.findByRole('option', { name: '2026-06-08' })).toBeTruthy();
    expect(screen.queryByRole('option', { name: '2026-06-13' })).toBeNull();
  });

  it('shows each room with its remaining places and disables a full room', async () => {
    const user = userEvent.setup();
    await renderPage([munich], {
      occupancy: () =>
        of<RoomAvailability[]>([
          { roomId: roomId('r1'), occupied: 8, capacity: 8, isFull: true },
          { roomId: roomId('r2'), occupied: 1, capacity: 5, isFull: false },
        ]),
    });

    await user.selectOptions(await screen.findByLabelText('Office'), 'o1');
    await user.selectOptions(screen.getByLabelText('Day'), '2026-06-08');

    expect(await screen.findByText('Full')).toBeTruthy();
    expect(screen.getByText('4 of 5 places left')).toBeTruthy();
    expect((screen.getByRole('button', { name: /A1/ }) as HTMLButtonElement).disabled).toBe(true);
    expect((screen.getByRole('button', { name: /B1/ }) as HTMLButtonElement).disabled).toBe(false);
  });

  it('reserves the chosen room for the chosen day and announces success', async () => {
    const user = userEvent.setup();
    let reservedWith: { office: string; room: string; date: string } | undefined;
    await renderPage([munich], {
      occupancy: () => of(allFree),
      reserve: (office, room, date) => {
        reservedWith = { office, room, date };
        return of(reservationId('res1'));
      },
    });

    await user.selectOptions(await screen.findByLabelText('Office'), 'o1');
    await user.selectOptions(screen.getByLabelText('Day'), '2026-06-08');
    await user.click(await screen.findByRole('button', { name: /A1/ }));
    await user.click(screen.getByRole('button', { name: 'Reserve' }));

    expect(reservedWith).toEqual({ office: 'o1', room: 'r1', date: '2026-06-08' });
    expect(await screen.findByText('Reserved A1 for 2026-06-08.')).toBeTruthy();
  });

  const rejections: ReadonlyArray<readonly [number, string, string]> = [
    [409, 'room_full', 'That room is full for the chosen day.'],
    [409, 'already_reserved_today', 'You already have a reservation that day.'],
    [422, 'not_bookable', 'Only working days within the next two weeks can be booked.'],
    [404, 'unknown_room', 'That room is no longer available.'],
    [409, 'concurrency_retry_exhausted', 'Someone just took the last place. Please try again.'],
  ];

  it.each(rejections)(
    'surfaces a localized message when reserve is rejected (%i %s)',
    async (status, code, message) => {
      const user = userEvent.setup();
      await renderPage([munich], {
        occupancy: () => of(allFree),
        reserve: () => throwError(() => new HttpErrorResponse({ status, error: { code } })),
      });

      await user.selectOptions(await screen.findByLabelText('Office'), 'o1');
      await user.selectOptions(screen.getByLabelText('Day'), '2026-06-08');
      await user.click(await screen.findByRole('button', { name: /A1/ }));
      await user.click(screen.getByRole('button', { name: 'Reserve' }));

      expect(await screen.findByText(message)).toBeTruthy();
    },
  );

  it('reserves on behalf of another employee when onBehalfOf is set', async () => {
    const user = userEvent.setup();
    let received: unknown[] | undefined;
    const gateway = {
      listBookableOffices: () => of([munich]),
      occupancyForOffice: () => of(allFree),
      reserve: (...args: unknown[]) => {
        received = args;
        return of(reservationId('res1'));
      },
      myReservations: () => of([]),
      cancel: () => of(undefined),
    };

    await render(ReservePage, {
      imports: [importAttendanceTestTransloco()],
      inputs: { today: '2026-06-08', onBehalfOf: employeeId('e9') },
      providers: [provideZonelessChangeDetection(), { provide: AttendanceGateway, useValue: gateway }],
    });

    await user.selectOptions(await screen.findByLabelText('Office'), 'o1');
    await user.selectOptions(screen.getByLabelText('Day'), '2026-06-08');
    await user.click(await screen.findByRole('button', { name: /A1/ }));
    await user.click(screen.getByRole('button', { name: 'Reserve' }));

    expect(received).toEqual(['o1', 'r1', '2026-06-08', 'e9']);
  });
});
