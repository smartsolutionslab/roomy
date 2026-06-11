import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  AttendanceGateway,
  MyReservation,
  ReservationId,
  officeId,
  reservationId,
  roomId,
} from '@roomy/attendance-api';
import type { Page } from '@roomy/shared-data-access';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Observable, of, throwError } from 'rxjs';

import { importAttendanceTestTransloco } from '../../testing/transloco';

import { MyReservationsPage } from './my-reservations-page';

function reservation(id: string, date: string, roomName: string): MyReservation {
  return {
    id: reservationId(id),
    officeId: officeId('o1'),
    officeName: 'Munich',
    roomId: roomId('r1'),
    roomName,
    date,
  };
}

const upcoming = reservation('res-up', '2026-06-10', 'A1');
const past = reservation('res-past', '2026-06-05', 'B1');

function page(items: MyReservation[], nextCursor: string | null = null): Page<MyReservation> {
  return { items, nextCursor };
}

interface Stub {
  list?: (cursor?: string) => Observable<Page<MyReservation>>;
  cancel?: (reservation: ReservationId, date: string) => Observable<void>;
}

function renderPage(reservations: MyReservation[], stub: Stub = {}) {
  const navigated: { commands: unknown[] }[] = [];
  const gateway = {
    listBookableOffices: () => of([]),
    occupancyForOffice: () => of([]),
    reserve: () => of(reservationId('res1')),
    myReservations: stub.list ?? (() => of(page(reservations))),
    cancel: stub.cancel ?? (() => of(undefined)),
  };
  const router = {
    navigate: (commands: unknown[]) => {
      navigated.push({ commands });
      return Promise.resolve(true);
    },
  };

  return {
    navigated,
    ...render(MyReservationsPage, {
      imports: [importAttendanceTestTransloco()],
      inputs: { today: '2026-06-08' },
      providers: [
        provideZonelessChangeDetection(),
        { provide: AttendanceGateway, useValue: gateway },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: {} },
      ],
    }),
  };
}

describe('MyReservationsPage', () => {
  it('lists upcoming and past reservations, distinguishing them', async () => {
    renderPage([upcoming, past]);

    expect(await screen.findByRole('heading', { name: 'Upcoming' })).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'Past' })).toBeTruthy();
    expect(screen.getByText('A1')).toBeTruthy();
    expect(screen.getByText('2026-06-10')).toBeTruthy();
    expect(screen.getByText('B1')).toBeTruthy();
  });

  it('offers cancel only on an upcoming reservation, not a past one', async () => {
    renderPage([upcoming, past]);

    expect(
      await screen.findByRole('button', { name: /Cancel the reservation for A1/ }),
    ).toBeTruthy();
    expect(screen.queryByRole('button', { name: /Cancel the reservation for B1/ })).toBeNull();
  });

  it('cancels an upcoming reservation, removes it, and announces', async () => {
    const user = userEvent.setup();
    let cancelledWith: { id: string; date: string } | undefined;
    renderPage([upcoming], {
      cancel: (id, date) => {
        cancelledWith = { id, date };
        return of(undefined);
      },
    });

    await user.click(await screen.findByRole('button', { name: /Cancel the reservation for A1/ }));

    expect(cancelledWith).toEqual({ id: 'res-up', date: '2026-06-10' });
    expect(await screen.findByText('Reservation cancelled.')).toBeTruthy();
    expect(screen.queryByText('A1')).toBeNull();
  });

  it('surfaces a localized message when a cancel is rejected as past-immutable', async () => {
    const user = userEvent.setup();
    renderPage([upcoming], {
      cancel: () =>
        throwError(() => new HttpErrorResponse({ status: 422, error: { code: 'past_immutable' } })),
    });

    await user.click(await screen.findByRole('button', { name: /Cancel the reservation for A1/ }));

    expect(await screen.findByText('Past reservations cannot be changed.')).toBeTruthy();
  });

  it('changes a reservation by cancelling it and navigating to the reserve flow', async () => {
    const user = userEvent.setup();
    let cancelledWith: { id: string; date: string } | undefined;
    const { navigated } = renderPage([upcoming], {
      cancel: (id, date) => {
        cancelledWith = { id, date };
        return of(undefined);
      },
    });

    await user.click(await screen.findByRole('button', { name: 'Change' }));

    expect(cancelledWith).toEqual({ id: 'res-up', date: '2026-06-10' });
    expect(navigated).toEqual([{ commands: ['..', 'reserve'] }]);
  });

  it('shows an empty state with a link to reserve, which navigates to the reserve flow', async () => {
    const user = userEvent.setup();
    const { navigated } = renderPage([]);

    expect(await screen.findByText('You have no reservations yet.')).toBeTruthy();
    await user.click(screen.getByRole('button', { name: 'Reserve a place' }));

    expect(navigated).toEqual([{ commands: ['..', 'reserve'] }]);
  });

  it('appends the next page when Load more is activated, then stops at the end', async () => {
    const user = userEvent.setup();
    const second = reservation('res-up-2', '2026-06-12', 'C1');
    renderPage([], {
      list: (cursor) =>
        of(cursor === undefined ? page([upcoming], 'cursor-2') : page([second], null)),
    });

    expect(await screen.findByText('A1')).toBeTruthy();
    expect(screen.queryByText('C1')).toBeNull();

    await user.click(screen.getByRole('button', { name: 'Load more' }));

    expect(await screen.findByText('C1')).toBeTruthy();
    expect(screen.getByText('End of list')).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Load more' })).toBeNull();
  });
});
