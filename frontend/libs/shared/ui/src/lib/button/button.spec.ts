import { Component, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { Button } from './button';

@Component({
  imports: [Button],
  template: `<button roomyButton>Default</button>
    <button roomyButton variant="accent">Accent</button>`,
})
class HostComponent {}

describe('Button directive', () => {
  it('applies the base button class and the accent modifier', async () => {
    await render(HostComponent, { providers: [provideZonelessChangeDetection()] });

    const base = screen.getByRole('button', { name: 'Default' });
    const accent = screen.getByRole('button', { name: 'Accent' });

    expect(base.classList.contains('roomy-button')).toBe(true);
    expect(base.classList.contains('roomy-button--accent')).toBe(false);
    expect(accent.classList.contains('roomy-button--accent')).toBe(true);
  });
});
