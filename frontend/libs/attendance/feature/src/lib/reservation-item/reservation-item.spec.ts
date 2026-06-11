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
        [namespace]="namespace"
        [showActions]="showActions"
        [showChange]="showChange"
        (cancelRequested)="cancelled = cancelled + 1"
        (changeRequested)="changed = changed + 1"
      />
    </li>
  </ul>`,
})
class HostComponent {
  reservation: MyReservation = reservation;
  namespace = 'attendance.mine';
  showActions = true;
  showChange = true;
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
    await renderHost({ showActions: false });

    expect(screen.getByText('Munich')).toBeTruthy();
    expect(screen.getByText('A1')).toBeTruthy();
    expect(screen.getByText('2026-06-10')).toBeTruthy();
  });

  it('offers cancel and change when both are enabled, emitting on click', async () => {
    const { fixture } = await renderHost({ showActions: true, showChange: true });

    await userEvent.click(screen.getByRole('button', { name: /Cancel the reservation for A1/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Change' }));

    expect(fixture.componentInstance.cancelled).toBe(1);
    expect(fixture.componentInstance.changed).toBe(1);
  });

  it('offers cancel without change when change is disabled', async () => {
    await renderHost({
      namespace: 'attendance.onBehalf',
      showActions: true,
      showChange: false,
    });

    expect(screen.getByRole('button', { name: /Cancel the reservation for A1/ })).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Change' })).toBeNull();
  });

  it('uses the given namespace for its action labels', async () => {
    await renderHost({ namespace: 'attendance.onBehalf', showActions: true });

    expect(screen.getByRole('button', { name: /Cancel the reservation for A1/ })).toBeTruthy();
  });

  it('shows no actions when not actionable', async () => {
    await renderHost({ showActions: false });

    expect(screen.queryByRole('button', { name: /Cancel the reservation for A1/ })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Change' })).toBeNull();
  });
});
