import { Component, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { AppLayout } from './layout';

@Component({
  imports: [AppLayout],
  template: `<roomy-app-layout>
    <span roomy-brand>BRAND</span>
    <nav roomy-nav>NAV</nav>
    <span roomy-top>TOP</span>
    <small roomy-footer>FOOTER</small>
    <p>MAIN</p>
  </roomy-app-layout>`,
})
class HostComponent {}

describe('AppLayout', () => {
  it('projects each region into its slot, including the routed main content', async () => {
    await render(HostComponent, { providers: [provideZonelessChangeDetection()] });

    for (const text of ['BRAND', 'NAV', 'TOP', 'FOOTER', 'MAIN']) {
      expect(screen.getByText(text)).toBeTruthy();
    }
  });

  it('exposes a focusable main landmark for the skip link target', async () => {
    await render(HostComponent, { providers: [provideZonelessChangeDetection()] });

    const main = screen.getByRole('main');
    expect(main.id).toBe('main-content');
  });
});
