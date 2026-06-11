import { Component, input, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { Heat, heatColor } from './heat';

describe('heatColor', () => {
  it('is green when free (fullness 0)', () => {
    expect(heatColor(0)).toBe('hsla(120, 70%, 45%, 0.18)');
  });

  it('is red when full (fullness 1)', () => {
    expect(heatColor(1)).toBe('hsla(0, 70%, 45%, 0.18)');
  });

  it('is yellow halfway (fullness 0.5)', () => {
    expect(heatColor(0.5)).toBe('hsla(60, 70%, 45%, 0.18)');
  });

  it('has no tint when occupancy is unknown (null)', () => {
    expect(heatColor(null)).toBeNull();
  });

  it('clamps an out-of-range ratio to the green/red ends', () => {
    expect(heatColor(-1)).toBe('hsla(120, 70%, 45%, 0.18)');
    expect(heatColor(2)).toBe('hsla(0, 70%, 45%, 0.18)');
  });
});

@Component({
  imports: [Heat],
  template: `<div data-testid="cell" [roomyHeat]="fullness()">day</div>`,
})
class HeatHost {
  readonly fullness = input<number | null>(0);
}

describe('Heat directive', () => {
  it('tints the host element when a fullness is given', async () => {
    await render(HeatHost, {
      inputs: { fullness: 0.5 },
      providers: [provideZonelessChangeDetection()],
    });

    const style = screen.getByTestId('cell').getAttribute('style') ?? '';
    expect(style).toContain('background-color');
  });

  it('leaves the host untinted when occupancy is unknown', async () => {
    await render(HeatHost, {
      inputs: { fullness: null },
      providers: [provideZonelessChangeDetection()],
    });

    const style = screen.getByTestId('cell').getAttribute('style') ?? '';
    expect(style).not.toContain('background-color');
  });
});
