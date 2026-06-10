import { Component, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { Page } from './page';

@Component({
  imports: [Page],
  template: `<section roomyPage size="form">Form page</section>
    <section roomyPage>Default page</section>`,
})
class HostComponent {}

describe('Page directive', () => {
  it('applies the page class and the size modifier', async () => {
    await render(HostComponent, { providers: [provideZonelessChangeDetection()] });

    const form = screen.getByText('Form page');
    const base = screen.getByText('Default page');

    expect(form.classList.contains('roomy-page')).toBe(true);
    expect(form.classList.contains('roomy-page--form')).toBe(true);
    expect(base.classList.contains('roomy-page')).toBe(true);
    expect(base.classList.contains('roomy-page--form')).toBe(false);
  });
});
