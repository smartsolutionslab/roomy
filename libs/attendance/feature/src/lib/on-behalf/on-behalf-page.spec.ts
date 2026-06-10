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
} from '@roomy/attendance-data-access';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Observable, of } from 'rxjs';

import { importAttendanceTestTransloco } from '../../testing/transloco';

import { OnBehalfPage } from './on-behalf-page';

const ada: Employee = { id: employeeId('e1'), name: 'Ada' };

const munich: BookableOffice = {
  id: officeId('o1'),
  name: 'Munich',
  rooms: [{ id: roomId('r1'), name: 'A1', capacity: 8 }],
};

const allFree: RoomAvailability[] = [{ roomId: roomId('r1'), occupied: 0, capacity: 8, isFull: false }];

const adasUpcoming: MyReservation = {
  id: reservationId('res1'),
  officeId: officeId('o1'),
  officeName: 'Munich',
  roomId: roomId('r1'),
  roomName: 'A1',
  date: '2026-06-10',
};

interface Stub {
  employees?: () => Observable<Employee[]>;
  reservationsFor?: (employee: string) => Observable<MyReservation[]>;
  cancel?: (reservation: string, date: string) => Observable<void>;
  reserve?: (...args: unknown[]) => Observable<unknown>;
}

function renderPage(stub: Stub = {}) {
  const gateway = {
    listEmployees: stub.employees ?? (() => of([ada])),
    reservationsFor: stub.reservationsFor ?? (() => of([adasUpcoming])),
    cancel: stub.cancel ?? (() => of(undefined)),
    listBookableOffices: () => of([munich]),
    occupancyForOffice: () => of(allFree),
    reserve: stub.reserve ?? (() => of(reservationId('res9'))),
    myReservations: () => of([]),
  };

  return render(OnBehalfPage, {
    imports: [importAttendanceTestTransloco()],
    inputs: { today: '2026-06-08' },
    providers: [provideZonelessChangeDetection(), { provide: AttendanceGateway, useValue: gateway }],
  });
}

describe('OnBehalfPage', () => {
  it('lists employees and prompts before one is chosen', async () => {
    await renderPage();

    expect(await screen.findByRole('option', { name: 'Ada' })).toBeTruthy();
    expect(screen.getByText('Select an employee to reserve or cancel on their behalf.')).toBeTruthy();
    // The embedded reserve flow is not shown until an employee is chosen.
    expect(screen.queryByRole('heading', { name: 'Reserve a place' })).toBeNull();
  });

  it("shows the embedded reserve flow and the employee's reservations once chosen", async () => {
    const user = userEvent.setup();
    await renderPage();

    await user.selectOptions(await screen.findByLabelText('Employee'), 'e1');

    expect(await screen.findByRole('heading', { name: 'Reserve a place' })).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'Their reservations' })).toBeTruthy();
    expect(screen.getByRole('button', { name: /Cancel the reservation for A1/ })).toBeTruthy();
  });

  it('reserves on behalf of the chosen employee (onBehalfOf is passed)', async () => {
    const user = userEvent.setup();
    let received: unknown[] | undefined;
    await renderPage({
      reservationsFor: () => of([]),
      reserve: (...args: unknown[]) => {
        received = args;
        return of(reservationId('res9'));
      },
    });

    await user.selectOptions(await screen.findByLabelText('Employee'), 'e1');
    await user.selectOptions(await screen.findByLabelText('Office'), 'o1');
    await user.selectOptions(screen.getByLabelText('Day'), '2026-06-08');
    await user.click(await screen.findByRole('button', { name: /A1/ }));
    await user.click(screen.getByRole('button', { name: 'Reserve' }));

    expect(received).toEqual(['o1', 'r1', '2026-06-08', 'e1']);
  });

  it('cancels the employee\'s upcoming reservation and announces', async () => {
    const user = userEvent.setup();
    let cancelledWith: { id: string; date: string } | undefined;
    await renderPage({
      cancel: (reservation, date) => {
        cancelledWith = { id: reservation, date };
        return of(undefined);
      },
    });

    await user.selectOptions(await screen.findByLabelText('Employee'), 'e1');
    await user.click(await screen.findByRole('button', { name: /Cancel the reservation for A1/ }));

    expect(cancelledWith).toEqual({ id: 'res1', date: '2026-06-10' });
    expect(await screen.findByText('Reservation cancelled.')).toBeTruthy();
  });

  it('shows an empty state when there are no employees', async () => {
    await renderPage({ employees: () => of([]) });

    expect(await screen.findByText('There are no employees yet.')).toBeTruthy();
  });
});
