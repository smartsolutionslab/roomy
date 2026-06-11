import { provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { RoomyLogo, RoomyLogoVariant } from './logo';

async function renderLogo(showWordmark = false) {
  return render(RoomyLogo, {
    inputs: { showWordmark },
    providers: [provideZonelessChangeDetection()],
  });
}

async function renderVariant(variant: RoomyLogoVariant) {
  return render(RoomyLogo, {
    inputs: { variant },
    providers: [provideZonelessChangeDetection()],
  });
}

describe('RoomyLogo', () => {
  it('carries an accessible "Roomy" name when the wordmark is hidden', async () => {
    await renderLogo(false);

    expect(screen.getByText('Roomy')).toBeTruthy();
  });

  it('shows the visible "oomy" wordmark — the mark supplies the R — when requested', async () => {
    await renderLogo(true);

    const wordmark = screen.getByText('oomy');
    expect(wordmark.classList.contains('roomy-logo__wordmark')).toBe(true);
    // The mark stands in for the leading R, but the wordmark still reads "Roomy" to assistive tech.
    expect(wordmark.textContent).toBe('Roomy');
  });

  it('defaults to the flat brand mark — a two-stop gradient', async () => {
    const { container } = await renderVariant('brand');

    const mark = container.querySelector('.roomy-logo__mark');
    expect(mark?.classList.contains('roomy-logo__mark--sunset')).toBe(false);
    expect(container.querySelectorAll('.roomy-logo__mark stop').length).toBe(2);
  });

  it('renders the sunset mark — a three-stop gradient with a distinct fill id', async () => {
    const { container } = await renderVariant('sunset');

    const mark = container.querySelector('.roomy-logo__mark');
    expect(mark?.classList.contains('roomy-logo__mark--sunset')).toBe(true);
    expect(container.querySelectorAll('.roomy-logo__mark stop').length).toBe(3);

    const fillId = container.querySelector('rect')?.getAttribute('fill');
    const gradientId = container.querySelector('linearGradient')?.getAttribute('id');
    expect(fillId).toBe(`url(#${gradientId})`);
    expect(gradientId).toBe('roomyLogoTile-sunset');
  });
});
