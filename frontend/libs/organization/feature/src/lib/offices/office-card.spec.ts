import { Component, provideZonelessChangeDetection } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Office, Room, officeId, roomId } from '@roomy/organization-api';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { importOrganizationTestTransloco } from '../../testing/transloco';

import { ActiveEditor, OfficeCard } from './office-card';

const sky: Room = { id: roomId('0199a0b0-0000-7000-8000-000000000020'), name: 'Sky', capacity: 8 };
const ground: Room = {
  id: roomId('0199a0b0-0000-7000-8000-000000000021'),
  name: 'Ground',
  capacity: 4,
};
const berlin: Office = {
  id: officeId('0199a0b0-0000-7000-8000-000000000010'),
  name: 'Berlin',
  location: 'Berlin, DE',
  capacity: 12,
  rooms: [sky, ground],
};

@Component({
  imports: [OfficeCard],
  template: `<ul class="roomy-list">
    <li roomyCard>
      <roomy-office-card
        [office]="office"
        [editor]="editor"
        [textForm]="textForm"
        [roomForm]="roomForm"
        [roomAttempted]="roomAttempted"
        [editConflict]="editConflict"
        [editFailed]="editFailed"
        (renameOpen)="renameOpen = renameOpen + 1"
        (relocateOpen)="relocateOpen = relocateOpen + 1"
        (addRoomOpen)="addRoomOpen = addRoomOpen + 1"
        (nameSave)="nameSave = nameSave + 1"
        (locationSave)="locationSave = locationSave + 1"
        (roomAdd)="roomAdd = roomAdd + 1"
        (roomRenameOpen)="roomRenameOpenRoom = $event"
        (roomNameSave)="roomNameSaveRoom = $event"
        (editCancel)="editCancel = editCancel + 1"
      />
    </li>
  </ul>`,
})
class HostComponent {
  readonly textForm = new FormGroup({
    value: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });
  readonly roomForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: Validators.required }),
    capacity: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1)],
    }),
  });
  office: Office = berlin;
  editor: ActiveEditor | null = null;
  roomAttempted = false;
  editConflict = false;
  editFailed = false;
  renameOpen = 0;
  relocateOpen = 0;
  addRoomOpen = 0;
  nameSave = 0;
  locationSave = 0;
  roomAdd = 0;
  editCancel = 0;
  roomRenameOpenRoom: Room | null = null;
  roomNameSaveRoom: Room | null = null;
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    imports: [importOrganizationTestTransloco()],
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('OfficeCard', () => {
  it('renders the office name, location, derived capacity and its rooms', async () => {
    await renderHost();

    expect(screen.getByRole('heading', { name: 'Berlin' })).toBeTruthy();
    expect(screen.getByText('Berlin, DE')).toBeTruthy();
    expect(screen.getByText('12')).toBeTruthy();
    expect(screen.getByText('Sky')).toBeTruthy();
    expect(screen.getByText('Ground')).toBeTruthy();
  });

  it('emits the open intents for rename, relocate and add room', async () => {
    const { fixture } = await renderHost();

    await userEvent.click(screen.getByRole('button', { name: 'Rename' }));
    await userEvent.click(screen.getByRole('button', { name: 'Change location' }));
    await userEvent.click(screen.getByRole('button', { name: 'Add room' }));

    expect(fixture.componentInstance.renameOpen).toBe(1);
    expect(fixture.componentInstance.relocateOpen).toBe(1);
    expect(fixture.componentInstance.addRoomOpen).toBe(1);
  });

  it('shows the rename-office editor and emits nameSave / editCancel', async () => {
    const { fixture } = await renderHost({
      editor: { kind: 'rename-office', officeId: berlin.id },
    });

    expect(screen.getByLabelText('New name')).toBeTruthy();
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    expect(fixture.componentInstance.nameSave).toBe(1);

    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(fixture.componentInstance.editCancel).toBe(1);
  });

  it('shows the relocate-office editor and emits locationSave', async () => {
    const { fixture } = await renderHost({
      editor: { kind: 'relocate-office', officeId: berlin.id },
    });

    expect(screen.getByLabelText('New location')).toBeTruthy();
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    expect(fixture.componentInstance.locationSave).toBe(1);
  });

  it('shows the add-room editor with validation messages and emits roomAdd', async () => {
    const { fixture } = await renderHost({
      editor: { kind: 'add-room', officeId: berlin.id },
      roomAttempted: true,
    });

    expect(screen.getByLabelText('Room name')).toBeTruthy();
    expect(screen.getByLabelText('Capacity (places)')).toBeTruthy();
    expect(screen.getByText('A room name is required.')).toBeTruthy();

    await userEvent.click(screen.getByRole('button', { name: 'Add' }));
    expect(fixture.componentInstance.roomAdd).toBe(1);
  });

  it('emits roomRenameOpen with the room when a room rename is requested', async () => {
    const { fixture } = await renderHost();

    await userEvent.click(screen.getByRole('button', { name: 'Rename room Sky' }));

    expect(fixture.componentInstance.roomRenameOpenRoom).toBe(sky);
  });

  it('shows the rename editor for the targeted room and emits roomNameSave with it', async () => {
    const { fixture } = await renderHost({
      editor: { kind: 'rename-room', officeId: berlin.id, roomId: sky.id },
    });

    expect(screen.getByLabelText('New room name')).toBeTruthy();
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(fixture.componentInstance.roomNameSaveRoom).toBe(sky);
  });
});
