import { Component, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { Card } from './card';

@Component({
  imports: [Card],
  template: `<div roomyCard>Plain</div>
    <a roomyCard interactive href="#">Link</a>`,
})
class HostComponent {}

describe('Card directive', () => {
  it('applies the card class and the interactive modifier only when requested', async () => {
    await render(HostComponent, { providers: [provideZonelessChangeDetection()] });

    const plain = screen.getByText('Plain');
    const link = screen.getByText('Link');

    expect(plain.classList.contains('roomy-card')).toBe(true);
    expect(plain.classList.contains('roomy-card--interactive')).toBe(false);
    expect(link.classList.contains('roomy-card--interactive')).toBe(true);
  });
});
