import { Component, provideZonelessChangeDetection } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Room, roomId } from '@roomy/organization-api';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { importOrganizationTestTransloco } from '../../testing/transloco';

import { RoomRow } from './room-row';

const sky: Room = { id: roomId('0199a0b0-0000-7000-8000-000000000020'), name: 'Sky', capacity: 8 };

@Component({
  imports: [RoomRow],
  template: `<ul>
    <li class="offices__room">
      <roomy-room-row
        [room]="room"
        [renaming]="renaming"
        [form]="form"
        [conflict]="conflict"
        [failed]="failed"
        (rename)="renamed = renamed + 1"
        (save)="saved = saved + 1"
        (cancelEdit)="cancelled = cancelled + 1"
      />
    </li>
  </ul>`,
})
class HostComponent {
  readonly form = new FormGroup({
    value: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });
  room: Room = sky;
  renaming = false;
  conflict = false;
  failed = false;
  renamed = 0;
  saved = 0;
  cancelled = 0;
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    imports: [importOrganizationTestTransloco()],
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('RoomRow', () => {
  it('shows the room name, capacity and a labelled rename button in the read view', async () => {
    await renderHost({ renaming: false });

    expect(screen.getByText('Sky')).toBeTruthy();
    expect(screen.getByText('8')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Rename room Sky' })).toBeTruthy();
  });

  it('emits rename when the rename button is clicked', async () => {
    const { fixture } = await renderHost({ renaming: false });

    await userEvent.click(screen.getByRole('button', { name: 'Rename room Sky' }));

    expect(fixture.componentInstance.renamed).toBe(1);
  });

  it('shows the rename form with Save and Cancel when renaming', async () => {
    await renderHost({ renaming: true });

    expect(screen.getByLabelText('New room name')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Save' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeTruthy();
  });

  it('emits save on submit and cancel on cancel while renaming', async () => {
    const { fixture } = await renderHost({ renaming: true });

    await userEvent.type(screen.getByLabelText('New room name'), 'Sky Lounge');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    expect(fixture.componentInstance.saved).toBe(1);

    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(fixture.componentInstance.cancelled).toBe(1);
  });

  it('shows the name-conflict message when conflict is set while renaming', async () => {
    await renderHost({ renaming: true, conflict: true });

    expect(screen.getByText('A room with that name already exists in this office.')).toBeTruthy();
  });

  it('shows the generic error when failed is set while renaming', async () => {
    await renderHost({ renaming: true, failed: true });

    expect(screen.getByText('We could not save the change. Please try again.')).toBeTruthy();
  });
});
