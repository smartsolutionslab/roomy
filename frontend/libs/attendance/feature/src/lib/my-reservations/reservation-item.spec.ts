import { Component, provideZonelessChangeDetection } from '@angular/core';
import { MyReservation, officeId, reservationId, roomId } from '@roomy/attendance-api';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { importAttendanceTestTransloco } from '../../testing/transloco';

import { ReservationItem } from './reservation-item';

const reservation: MyReservation = {
  id: reservationId('res-up'),
  officeId: officeId('o1'),
  officeName: 'Munich',
  roomId: roomId('r1'),
  roomName: 'A1',
  date: '2026-06-10',
};

@Component({
  imports: [ReservationItem],
  template: `<ul class="roomy-list">
    <li class="roomy-list-item">
      <roomy-reservation-item
        [reservation]="reservation"
        [showActions]="showActions"
        (cancelRequested)="cancelled = cancelled + 1"
        (changeRequested)="changed = changed + 1"
      />
    </li>
  </ul>`,
})
class HostComponent {
  reservation: MyReservation = reservation;
  showActions = true;
  cancelled = 0;
  changed = 0;
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    imports: [importAttendanceTestTransloco()],
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('ReservationItem', () => {
  it('renders the office, room and date', async () => {
    await renderHost();

    expect(screen.getByText('Munich')).toBeTruthy();
    expect(screen.getByText('A1')).toBeTruthy();
    expect(screen.getByText('2026-06-10')).toBeTruthy();
  });

  it('offers cancel and change actions when actionable, emitting on click', async () => {
    const { fixture } = await renderHost({ showActions: true });

    await userEvent.click(screen.getByRole('button', { name: /Cancel the reservation for A1/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Change' }));

    expect(fixture.componentInstance.cancelled).toBe(1);
    expect(fixture.componentInstance.changed).toBe(1);
  });

  it('shows no actions when not actionable', async () => {
    await renderHost({ showActions: false });

    expect(screen.queryByRole('button', { name: /Cancel the reservation for A1/ })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Change' })).toBeNull();
  });
});
