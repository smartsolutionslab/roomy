import { Component, provideZonelessChangeDetection } from '@angular/core';
import { MyReservation, officeId, reservationId, roomId } from '@roomy/attendance-api';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { importAttendanceTestTransloco } from '../../testing/transloco';

import { ReservationHistory } from './reservation-history';

function reservation(id: string, roomName: string, date: string): MyReservation {
  return {
    id: reservationId(id),
    officeId: officeId('o1'),
    officeName: 'Munich',
    roomId: roomId('r1'),
    roomName,
    date,
  };
}

const upcoming = reservation('up', 'A1', '2026-06-10');
const past = reservation('past', 'B1', '2026-06-05');

@Component({
  imports: [ReservationHistory],
  template: `<roomy-reservation-history
    [upcoming]="upcomingRows"
    [past]="pastRows"
    [namespace]="namespace"
    [showChange]="showChange"
    [hasMore]="hasMore"
    [loading]="loading"
    [headingLevel]="headingLevel"
    (loadMore)="loadMores = loadMores + 1"
    (cancelRequested)="cancelled = $event"
    (changeRequested)="changed = $event"
  />`,
})
class HostComponent {
  upcomingRows: MyReservation[] = [upcoming];
  pastRows: MyReservation[] = [past];
  namespace = 'attendance.mine';
  showChange = true;
  hasMore = false;
  loading = false;
  headingLevel: 2 | 3 = 2;
  loadMores = 0;
  cancelled: MyReservation | null = null;
  changed: MyReservation | null = null;
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    imports: [importAttendanceTestTransloco()],
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('ReservationHistory', () => {
  it('lists the upcoming and past reservations under their headings', async () => {
    await renderHost();

    expect(screen.getByRole('heading', { name: 'Upcoming', level: 2 })).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'Past', level: 2 })).toBeTruthy();
    expect(screen.getByText('A1')).toBeTruthy();
    expect(screen.getByText('B1')).toBeTruthy();
  });

  it('offers cancel and change on upcoming rows and emits the reservation', async () => {
    const { fixture } = await renderHost();

    await userEvent.click(screen.getByRole('button', { name: /Cancel the reservation for A1/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Change' }));

    expect(fixture.componentInstance.cancelled).toBe(upcoming);
    expect(fixture.componentInstance.changed).toBe(upcoming);
  });

  it('shows past rows without actions', async () => {
    await renderHost();

    expect(screen.queryByRole('button', { name: /Cancel the reservation for B1/ })).toBeNull();
  });

  it('omits the change action when showChange is false', async () => {
    await renderHost({ showChange: false });

    expect(screen.queryByRole('button', { name: 'Change' })).toBeNull();
  });

  it('renders the headings at level 3 when requested', async () => {
    await renderHost({ headingLevel: 3 });

    expect(screen.getByRole('heading', { name: 'Upcoming', level: 3 })).toBeTruthy();
  });

  it('omits the past section when there are no past reservations', async () => {
    await renderHost({ pastRows: [] });

    expect(screen.queryByRole('heading', { name: 'Past' })).toBeNull();
  });

  it('emits loadMore from the endless list when more pages remain', async () => {
    const { fixture } = await renderHost({ hasMore: true });

    await userEvent.click(screen.getByRole('button', { name: 'Load more' }));

    expect(fixture.componentInstance.loadMores).toBe(1);
  });
});
