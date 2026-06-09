import { provideZonelessChangeDetection } from '@angular/core';
import { Office, OfficesGateway, officeId, roomId } from '@roomy/organization-data-access';
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
}

function renderPage(offices: Office[], stub: GatewayStub = {}) {
  const gateway = {
    listOffices: stub.list ?? (() => of(offices)),
    createOffice:
      stub.create ??
      ((name: string, location: string) =>
        of<Office>({ id: officeId('created'), name, location, capacity: 0, rooms: [] })),
  };

  return render(OfficesPage, {
    imports: [importOrganizationTestTransloco()],
    providers: [
      provideZonelessChangeDetection(),
      { provide: OfficesGateway, useValue: gateway },
    ],
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
});
