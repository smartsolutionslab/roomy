import { Component, provideZonelessChangeDetection } from '@angular/core';
import { BookableRoom, RoomAvailability, roomId } from '@roomy/attendance-api';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { importAttendanceTestTransloco } from '../../testing/transloco';

import { RoomCell } from './room-cell';

const room: BookableRoom = { id: roomId('r1'), name: 'A1', capacity: 5 };

@Component({
  imports: [RoomCell],
  template: `<ul class="reserve__rooms">
    <li>
      <roomy-room-cell
        [room]="room"
        [availability]="availability"
        [selected]="selected"
        (chosen)="chosen = chosen + 1"
      />
    </li>
  </ul>`,
})
class HostComponent {
  room: BookableRoom = room;
  availability: RoomAvailability | undefined = undefined;
  selected = false;
  chosen = 0;
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    imports: [importAttendanceTestTransloco()],
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('RoomCell', () => {
  it('shows the room and no availability bar until availability is known', async () => {
    const { container } = await renderHost({ availability: undefined });

    expect(screen.getByText('A1')).toBeTruthy();
    expect(container.querySelector('.reserve__room-bar')).toBeNull();
    expect((screen.getByRole('button', { name: /A1/ }) as HTMLButtonElement).disabled).toBe(false);
  });

  it('shows remaining places and an availability bar when not full', async () => {
    const { container } = await renderHost({
      availability: { roomId: room.id, occupied: 1, capacity: 5, isFull: false },
    });

    expect(screen.getByText('4 of 5 places left')).toBeTruthy();
    expect(container.querySelector('.reserve__room-bar')).not.toBeNull();
    expect((screen.getByRole('button', { name: /A1/ }) as HTMLButtonElement).disabled).toBe(false);
  });

  it('marks a full room as Full and disables it', async () => {
    await renderHost({
      availability: { roomId: room.id, occupied: 5, capacity: 5, isFull: true },
    });

    expect(screen.getByText('Full')).toBeTruthy();
    expect((screen.getByRole('button', { name: /A1/ }) as HTMLButtonElement).disabled).toBe(true);
  });

  it('reflects the selected state via aria-pressed', async () => {
    await renderHost({ selected: true });

    expect(screen.getByRole('button', { name: /A1/ }).getAttribute('aria-pressed')).toBe('true');
  });

  it('emits chosen when an available room is clicked', async () => {
    const { fixture } = await renderHost({
      availability: { roomId: room.id, occupied: 0, capacity: 5, isFull: false },
    });

    await userEvent.click(screen.getByRole('button', { name: /A1/ }));

    expect(fixture.componentInstance.chosen).toBe(1);
  });
});
