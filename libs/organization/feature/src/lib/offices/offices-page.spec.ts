import { provideZonelessChangeDetection } from '@angular/core';
import {
  Office,
  OfficeId,
  OfficesGateway,
  Room,
  RoomId,
  officeId,
  roomId,
} from '@roomy/organization-data-access';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Observable, of, throwError } from 'rxjs';

import { importOrganizationTestTransloco } from '../../testing/transloco';

import { OfficesPage } from './offices-page';

const berlin: Office = {
  id: officeId('0199a0b0-0000-7000-8000-000000000010'),
  name: 'Berlin',
  location: 'Berlin, DE',
  capacity: 12,
  rooms: [
    { id: roomId('0199a0b0-0000-7000-8000-000000000020'), name: 'Sky', capacity: 8 },
    { id: roomId('0199a0b0-0000-7000-8000-000000000021'), name: 'Ground', capacity: 4 },
  ],
};

interface GatewayStub {
  list?: () => Observable<Office[]>;
  create?: (name: string, location: string) => Observable<Office>;
  rename?: (office: OfficeId, name: string) => Observable<Office>;
  relocate?: (office: OfficeId, location: string) => Observable<Office>;
  addRoom?: (office: OfficeId, name: string, capacity: number) => Observable<Room>;
  renameRoom?: (office: OfficeId, room: RoomId, name: string) => Observable<Office>;
}

function renderPage(offices: Office[], stub: GatewayStub = {}) {
  const gateway = {
    listOffices: stub.list ?? (() => of(offices)),
    createOffice:
      stub.create ??
      ((name: string, location: string) =>
        of<Office>({ id: officeId('created'), name, location, capacity: 0, rooms: [] })),
    renameOffice: stub.rename ?? ((_office: OfficeId, name: string) => of<Office>({ ...berlin, name })),
    relocateOffice:
      stub.relocate ?? ((_office: OfficeId, location: string) => of<Office>({ ...berlin, location })),
    addRoom:
      stub.addRoom ??
      ((_office: OfficeId, name: string, capacity: number) =>
        of<Room>({ id: roomId('created-room'), name, capacity })),
    renameRoom: stub.renameRoom ?? (() => of<Office>(berlin)),
  };

  return render(OfficesPage, {
    imports: [importOrganizationTestTransloco()],
    providers: [provideZonelessChangeDetection(), { provide: OfficesGateway, useValue: gateway }],
  });
}

describe('OfficesPage', () => {
  it('lists each office with its location, derived capacity and rooms', async () => {
    await renderPage([berlin]);

    expect(await screen.findByRole('heading', { name: 'Berlin' })).toBeTruthy();
    expect(screen.getByText('Berlin, DE')).toBeTruthy();
    expect(screen.getByText('Sky')).toBeTruthy();
    expect(screen.getByText('Ground')).toBeTruthy();
    expect(screen.getByText('12')).toBeTruthy();
  });

  it('shows an empty state when there are no offices', async () => {
    await renderPage([]);

    expect(await screen.findByText('No offices yet.')).toBeTruthy();
  });

  it('announces an error when the offices cannot be loaded', async () => {
    await renderPage([], { list: () => throwError(() => new Error('boom')) });

    expect(await screen.findByText('We could not load the offices.')).toBeTruthy();
  });

  it('creates an office, shows it in the list and announces the result', async () => {
    const munich: Office = {
      id: officeId('0199a0b0-0000-7000-8000-000000000030'),
      name: 'Munich',
      location: 'Munich, DE',
      capacity: 0,
      rooms: [],
    };
    await renderPage([berlin], { create: () => of(munich) });

    await userEvent.type(screen.getByLabelText('Name'), 'Munich');
    await userEvent.type(screen.getByLabelText('Location'), 'Munich, DE');
    await userEvent.click(screen.getByRole('button', { name: 'Create office' }));

    expect(await screen.findByRole('heading', { name: 'Munich' })).toBeTruthy();
    expect(await screen.findByText('Office Munich created.')).toBeTruthy();
  });

  it('shows a field-level conflict and adds nothing when the name is taken', async () => {
    await renderPage([berlin], { create: () => throwError(() => ({ status: 409 })) });

    await userEvent.type(screen.getByLabelText('Name'), 'Berlin');
    await userEvent.type(screen.getByLabelText('Location'), 'Berlin, DE');
    await userEvent.click(screen.getByRole('button', { name: 'Create office' }));

    expect(await screen.findByText('An office with that name already exists.')).toBeTruthy();
    expect(screen.getAllByRole('heading', { name: 'Berlin' })).toHaveLength(1);
  });

  it('announces a non-blocking error when creating fails', async () => {
    await renderPage([berlin], { create: () => throwError(() => ({ status: 500 })) });

    await userEvent.type(screen.getByLabelText('Name'), 'Munich');
    await userEvent.type(screen.getByLabelText('Location'), 'Munich, DE');
    await userEvent.click(screen.getByRole('button', { name: 'Create office' }));

    expect(
      await screen.findByText('We could not create the office. Please try again.'),
    ).toBeTruthy();
  });

  // US3 — edit office (rename + relocate)

  it('renames an office and reflects the new name with an announced result', async () => {
    await renderPage([berlin], { rename: (_office, name) => of({ ...berlin, name }) });

    await userEvent.click(screen.getByRole('button', { name: 'Rename' }));
    const input = screen.getByLabelText('New name');
    await userEvent.clear(input);
    await userEvent.type(input, 'Berlin HQ');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByRole('heading', { name: 'Berlin HQ' })).toBeTruthy();
    expect(await screen.findByText('Office Berlin HQ updated.')).toBeTruthy();
  });

  it('shows a field-level conflict when renaming to a taken name', async () => {
    await renderPage([berlin], { rename: () => throwError(() => ({ status: 409 })) });

    await userEvent.click(screen.getByRole('button', { name: 'Rename' }));
    await userEvent.clear(screen.getByLabelText('New name'));
    await userEvent.type(screen.getByLabelText('New name'), 'Munich');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('An office with that name already exists.')).toBeTruthy();

    // The editor stays open so the name can be fixed; the office itself is unchanged.
    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.getByRole('heading', { name: 'Berlin' })).toBeTruthy();
    expect(screen.queryByRole('heading', { name: 'Munich' })).toBeNull();
  });

  it('changes an office location', async () => {
    await renderPage([berlin], { relocate: (_office, location) => of({ ...berlin, location }) });

    await userEvent.click(screen.getByRole('button', { name: 'Change location' }));
    const input = screen.getByLabelText('New location');
    await userEvent.clear(input);
    await userEvent.type(input, 'Berlin, Mitte');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('Berlin, Mitte')).toBeTruthy();
  });

  it('tells the admin when the office no longer exists', async () => {
    await renderPage([berlin], { rename: () => throwError(() => ({ status: 404 })) });

    await userEvent.click(screen.getByRole('button', { name: 'Rename' }));
    await userEvent.clear(screen.getByLabelText('New name'));
    await userEvent.type(screen.getByLabelText('New name'), 'Berlin HQ');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('That office no longer exists.')).toBeTruthy();
  });

  // US4 — rooms (add + rename)

  it('adds a room and grows the office capacity, announcing the result', async () => {
    await renderPage([berlin], {
      addRoom: (_office, name, capacity) => of<Room>({ id: roomId('room-3'), name, capacity }),
    });

    await userEvent.click(screen.getByRole('button', { name: 'Add room' }));
    await userEvent.type(screen.getByLabelText('Room name'), 'Lab');
    const capacity = screen.getByLabelText('Capacity (places)');
    await userEvent.clear(capacity);
    await userEvent.type(capacity, '6');
    await userEvent.click(screen.getByRole('button', { name: 'Add' }));

    expect(await screen.findByText('Lab')).toBeTruthy();
    expect(await screen.findByText('Room Lab added.')).toBeTruthy();
    expect(screen.getByText('18')).toBeTruthy();
  });

  it('rejects a room with capacity below 1 without calling the gateway', async () => {
    const addRoom = vi.fn();
    await renderPage([berlin], { addRoom });

    await userEvent.click(screen.getByRole('button', { name: 'Add room' }));
    await userEvent.type(screen.getByLabelText('Room name'), 'Lab');
    const capacity = screen.getByLabelText('Capacity (places)');
    await userEvent.clear(capacity);
    await userEvent.type(capacity, '0');
    await userEvent.click(screen.getByRole('button', { name: 'Add' }));

    expect(await screen.findByText('Capacity must be at least 1.')).toBeTruthy();
    expect(addRoom).not.toHaveBeenCalled();
  });

  it('rejects a blank room name without calling the gateway', async () => {
    const addRoom = vi.fn();
    await renderPage([berlin], { addRoom });

    await userEvent.click(screen.getByRole('button', { name: 'Add room' }));
    const capacity = screen.getByLabelText('Capacity (places)');
    await userEvent.clear(capacity);
    await userEvent.type(capacity, '4');
    await userEvent.click(screen.getByRole('button', { name: 'Add' }));

    expect(await screen.findByText('A room name is required.')).toBeTruthy();
    expect(addRoom).not.toHaveBeenCalled();
  });

  it('shows a field-level conflict when a room name is taken', async () => {
    await renderPage([berlin], { addRoom: () => throwError(() => ({ status: 409 })) });

    await userEvent.click(screen.getByRole('button', { name: 'Add room' }));
    await userEvent.type(screen.getByLabelText('Room name'), 'Sky');
    await userEvent.click(screen.getByRole('button', { name: 'Add' }));

    expect(
      await screen.findByText('A room with that name already exists in this office.'),
    ).toBeTruthy();
  });

  it('renames a room and reflects the new name', async () => {
    await renderPage([berlin], {
      renameRoom: (_office, room, name) =>
        of<Office>({ ...berlin, rooms: berlin.rooms.map((r) => (r.id === room ? { ...r, name } : r)) }),
    });

    await userEvent.click(screen.getByRole('button', { name: 'Rename room Sky' }));
    const input = screen.getByLabelText('New room name');
    await userEvent.clear(input);
    await userEvent.type(input, 'Sky Lounge');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('Sky Lounge')).toBeTruthy();
    expect(await screen.findByText('Room Sky Lounge renamed.')).toBeTruthy();
  });
});
