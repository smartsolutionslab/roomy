import { provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { RoomyLogo } from './logo';

async function renderLogo(showWordmark = false) {
  return render(RoomyLogo, {
    inputs: { showWordmark },
    providers: [provideZonelessChangeDetection()],
  });
}

describe('RoomyLogo', () => {
  it('carries an accessible "Roomy" name when the wordmark is hidden', async () => {
    await renderLogo(false);

    expect(screen.getByText('Roomy')).toBeTruthy();
  });

  it('renders the visible "Roomy" wordmark when requested', async () => {
    await renderLogo(true);

    const wordmark = screen.getByText('Roomy');
    expect(wordmark.classList.contains('roomy-logo__wordmark')).toBe(true);
  });
});
