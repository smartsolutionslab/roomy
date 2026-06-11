import { provideZonelessChangeDetection } from '@angular/core';
import {
  AttendanceGateway,
  BookableOffice,
  Employee,
  MyReservation,
  RoomAvailability,
  employeeId,
  officeId,
  reservationId,
  roomId,
} from '@roomy/attendance-api';
import type { Page } from '@roomy/shared-data-access';
import { render, screen } from '@testing-library/angular';
import userEvent, { type UserEvent } from '@testing-library/user-event';
import { Observable, of } from 'rxjs';

import { importAttendanceTestTransloco } from '../../testing/transloco';

import { OnBehalfPage } from './on-behalf-page';

const ada: Employee = { id: employeeId('e1'), name: 'Ada' };
const hannah: Employee = { id: employeeId('e2'), name: 'Hannah' };

const munich: BookableOffice = {
  id: officeId('o1'),
  name: 'Munich',
  rooms: [{ id: roomId('r1'), name: 'A1', capacity: 8 }],
};

const allFree: RoomAvailability[] = [
  { roomId: roomId('r1'), occupied: 0, capacity: 8, isFull: false },
];

const adasUpcoming: MyReservation = {
  id: reservationId('res1'),
  officeId: officeId('o1'),
  officeName: 'Munich',
  roomId: roomId('r1'),
  roomName: 'A1',
  date: '2026-06-10',
};

function page<T>(items: T[], nextCursor: string | null = null): Page<T> {
  return { items, nextCursor };
}

interface Stub {
  employees?: (query?: string, cursor?: string) => Observable<Page<Employee>>;
  reservationsFor?: (employee: string, cursor?: string) => Observable<Page<MyReservation>>;
  cancel?: (reservation: string, date: string) => Observable<void>;
  reserve?: (...args: unknown[]) => Observable<unknown>;
}

function renderPage(stub: Stub = {}) {
  const gateway = {
    listEmployees: stub.employees ?? (() => of(page([ada]))),
    reservationsFor: stub.reservationsFor ?? (() => of(page([adasUpcoming]))),
    cancel: stub.cancel ?? (() => of(undefined)),
    listBookableOffices: () => of([munich]),
    occupancyForOffice: () => of(allFree),
    reserve: stub.reserve ?? (() => of(reservationId('res9'))),
    myReservations: () => of(page<MyReservation>([])),
  };

  return render(OnBehalfPage, {
    imports: [importAttendanceTestTransloco()],
    inputs: { today: '2026-06-08' },
    providers: [
      provideZonelessChangeDetection(),
      { provide: AttendanceGateway, useValue: gateway },
    ],
  });
}

// Pick an employee through the search combobox: open it, then click the named match.
async function pickEmployee(user: UserEvent, name: string) {
  await user.click(await screen.findByRole('combobox', { name: 'Employee' }));
  await user.click(await screen.findByRole('option', { name }));
}

describe('OnBehalfPage', () => {
  it('lists employees and prompts before one is chosen', async () => {
    const user = userEvent.setup();
    await renderPage();

    await user.click(await screen.findByRole('combobox', { name: 'Employee' }));
    expect(await screen.findByRole('option', { name: 'Ada' })).toBeTruthy();
    expect(
      screen.getByText('Select an employee to reserve or cancel on their behalf.'),
    ).toBeTruthy();
    // The embedded reserve flow is not shown until an employee is chosen.
    expect(screen.queryByRole('heading', { name: 'Reserve a place' })).toBeNull();
  });

  it("shows the embedded reserve flow and the employee's reservations once chosen", async () => {
    const user = userEvent.setup();
    await renderPage();

    await pickEmployee(user, 'Ada');

    expect(await screen.findByRole('heading', { name: 'Reserve a place' })).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'Their reservations' })).toBeTruthy();
    expect(screen.getByRole('button', { name: /Cancel the reservation for A1/ })).toBeTruthy();
  });

  it('reserves on behalf of the chosen employee (onBehalfOf is passed)', async () => {
    const user = userEvent.setup();
    let received: unknown[] | undefined;
    await renderPage({
      reservationsFor: () => of(page<MyReservation>([])),
      reserve: (...args: unknown[]) => {
        received = args;
        return of(reservationId('res9'));
      },
    });

    await pickEmployee(user, 'Ada');
    await user.click(await screen.findByRole('button', { name: 'Munich' }));
    await user.selectOptions(screen.getByRole('combobox', { name: 'Day' }), '2026-06-08');
    await user.click(await screen.findByRole('button', { name: /A1/ }));
    await user.click(screen.getByRole('button', { name: 'Reserve' }));

    expect(received).toEqual(['o1', 'r1', '2026-06-08', 'e1']);
  });

  it("cancels the employee's upcoming reservation and announces", async () => {
    const user = userEvent.setup();
    let cancelledWith: { id: string; date: string } | undefined;
    await renderPage({
      cancel: (reservation, date) => {
        cancelledWith = { id: reservation, date };
        return of(undefined);
      },
    });

    await pickEmployee(user, 'Ada');
    await user.click(await screen.findByRole('button', { name: /Cancel the reservation for A1/ }));

    expect(cancelledWith).toEqual({ id: 'res1', date: '2026-06-10' });
    expect(await screen.findByText('Reservation cancelled.')).toBeTruthy();
  });

  it('shows an empty state when there are no employees', async () => {
    await renderPage({ employees: () => of(page<Employee>([])) });

    expect(await screen.findByText('There are no employees yet.')).toBeTruthy();
  });

  it("appends the next page of the chosen employee's reservations on Load more", async () => {
    const user = userEvent.setup();
    const later: MyReservation = {
      ...adasUpcoming,
      id: reservationId('res2'),
      roomName: 'C1',
      date: '2026-06-12',
    };
    await renderPage({
      reservationsFor: (_employee, cursor) =>
        of(cursor === undefined ? page([adasUpcoming], 'cursor-2') : page([later], null)),
    });

    await pickEmployee(user, 'Ada');
    expect(await screen.findByText('A1')).toBeTruthy();
    expect(screen.queryByText('C1')).toBeNull();

    await user.click(screen.getByRole('button', { name: 'Load more' }));

    expect(await screen.findByText('C1')).toBeTruthy();
  });

  it('offers a labelled search combobox for the employee directory', async () => {
    await renderPage();

    expect(await screen.findByRole('combobox', { name: 'Employee' })).toBeTruthy();
  });

  it('narrows the directory by name, passing the query to the gateway', async () => {
    const user = userEvent.setup();
    const queries: string[] = [];
    await renderPage({
      employees: (query) => {
        queries.push(query ?? '');
        return of(page(query ? [hannah] : [ada]));
      },
    });

    const search = await screen.findByRole('combobox', { name: 'Employee' });
    await user.click(search);
    expect(await screen.findByRole('option', { name: 'Ada' })).toBeTruthy();

    await user.type(search, 'han');

    // reset() replaces the list, so once Hannah is present the unmatched Ada is gone.
    expect(await screen.findByRole('option', { name: 'Hannah' })).toBeTruthy();
    expect(screen.queryByRole('option', { name: 'Ada' })).toBeNull();
    expect(queries).toContain('han');
  });

  it('restores the full directory when the search is cleared', async () => {
    const user = userEvent.setup();
    await renderPage({
      employees: (query) => of(page(query ? [hannah] : [ada])),
    });

    const search = await screen.findByRole('combobox', { name: 'Employee' });
    await user.type(search, 'han');
    expect(await screen.findByRole('option', { name: 'Hannah' })).toBeTruthy();

    await user.clear(search);

    expect(await screen.findByRole('option', { name: 'Ada' })).toBeTruthy();
  });

  it('shows a no-match message and keeps the search box when nothing matches', async () => {
    const user = userEvent.setup();
    await renderPage({
      employees: (query) => of(page(query ? [] : [ada])),
    });

    await user.type(await screen.findByRole('combobox', { name: 'Employee' }), 'zzz');

    expect(await screen.findByText('No employees match your search.')).toBeTruthy();
    // The search box stays so the query can be refined or cleared.
    expect(screen.getByRole('combobox', { name: 'Employee' })).toBeTruthy();
  });
});
