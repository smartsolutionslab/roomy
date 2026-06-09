import { provideZonelessChangeDetection } from '@angular/core';
import { Office, OfficesGateway, officeId, roomId } from '@roomy/organization-data-access';
import { render, screen } from '@testing-library/angular';
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

function renderPage(offices: Office[], list: () => Observable<Office[]> = () => of(offices)) {
  return render(OfficesPage, {
    imports: [importOrganizationTestTransloco()],
    providers: [
      provideZonelessChangeDetection(),
      { provide: OfficesGateway, useValue: { listOffices: list } },
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
    await renderPage([], () => throwError(() => new Error('boom')));

    expect(await screen.findByText('We could not load the offices.')).toBeTruthy();
  });
});
