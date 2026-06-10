import { provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { Avatar } from './avatar';

async function renderAvatar(name: string) {
  return render(Avatar, {
    inputs: { name },
    providers: [provideZonelessChangeDetection()],
  });
}

describe('Avatar', () => {
  it('shows the first and last initial of a full name', async () => {
    await renderAvatar('Ada Lovelace');

    expect(screen.getByText('AL')).toBeTruthy();
  });

  it('shows a single initial for a one-word name', async () => {
    await renderAvatar('Ada');

    expect(screen.getByText('A')).toBeTruthy();
  });
});
