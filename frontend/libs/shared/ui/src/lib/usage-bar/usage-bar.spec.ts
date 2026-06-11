import { provideZonelessChangeDetection } from '@angular/core';
import { render } from '@testing-library/angular';

import { UsageBar } from './usage-bar';

function renderBar(inputs: { occupied: number; capacity: number }) {
  return render(UsageBar, { inputs, providers: [provideZonelessChangeDetection()] });
}

function fillStyle(container: HTMLElement): string {
  return container.querySelector('.usage-bar__fill')?.getAttribute('style') ?? '';
}

describe('UsageBar', () => {
  it('fills the bar to the occupied share of capacity', async () => {
    const { container } = await renderBar({ occupied: 1, capacity: 4 });

    expect(fillStyle(container)).toContain('25%');
  });

  it('shows an empty bar when nothing is occupied', async () => {
    const { container } = await renderBar({ occupied: 0, capacity: 8 });

    expect(fillStyle(container)).toContain('0%');
  });

  it('treats zero capacity as empty rather than dividing by zero', async () => {
    const { container } = await renderBar({ occupied: 0, capacity: 0 });

    expect(fillStyle(container)).toContain('0%');
  });
});
