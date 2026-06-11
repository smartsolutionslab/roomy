import { Component, provideZonelessChangeDetection } from '@angular/core';
import { BookableOffice, officeId, roomId } from '@roomy/attendance-api';
import { render, screen } from '@testing-library/angular';

import { importAttendanceTestTransloco } from '../../testing/transloco';

import { OccupancyShell } from './occupancy-shell';

const munich: BookableOffice = {
  id: officeId('o1'),
  name: 'Munich',
  rooms: [{ id: roomId('r1'), name: 'A1', capacity: 8 }],
};

@Component({
  imports: [OccupancyShell],
  template: `<roomy-occupancy-shell
    [titleKey]="'attendance.occupancy.title'"
    [offices]="offices"
    [loadFailed]="loadFailed"
    [errorKey]="errorKey"
  >
    <p data-testid="projected">projected content</p>
  </roomy-occupancy-shell>`,
})
class HostComponent {
  offices: BookableOffice[] | null = null;
  loadFailed = false;
  errorKey: string | null = null;
}

function renderHost(properties: Partial<HostComponent> = {}) {
  return render(HostComponent, {
    imports: [importAttendanceTestTransloco()],
    providers: [provideZonelessChangeDetection()],
    componentProperties: properties,
  });
}

describe('OccupancyShell', () => {
  it('shows the title and a loading placeholder until the offices resolve', async () => {
    await renderHost({ offices: null });

    expect(screen.getByRole('heading', { name: 'Occupancy' })).toBeTruthy();
    expect(screen.getByText('Loading…')).toBeTruthy();
    expect(screen.queryByTestId('projected')).toBeNull();
  });

  it('announces a load failure', async () => {
    await renderHost({ loadFailed: true });

    expect(screen.getByText('We could not load the occupancy.')).toBeTruthy();
  });

  it('shows an empty state when no offices are bookable', async () => {
    await renderHost({ offices: [] });

    expect(screen.getByText('No offices or rooms are available yet.')).toBeTruthy();
    expect(screen.queryByTestId('projected')).toBeNull();
  });

  it('renders the picker and projects the content once offices are loaded', async () => {
    await renderHost({ offices: [munich] });

    expect(screen.getByTestId('projected')).toBeTruthy();
    expect(screen.getByRole('option', { name: 'Munich' })).toBeTruthy();
  });

  it('shows an error banner for the given errorKey', async () => {
    await renderHost({ offices: [munich], errorKey: 'attendance.occupancy.unknownScope' });

    expect(screen.getByText('That office or room is no longer available.')).toBeTruthy();
  });
});
