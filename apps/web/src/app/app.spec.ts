import { provideZonelessChangeDetection } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { render, screen } from '@testing-library/angular';
import axe from 'axe-core';

import { App } from './app';
import { importTestTransloco } from './i18n/transloco-testing';

async function renderShell() {
  return render(App, {
    imports: [importTestTransloco()],
    providers: [provideZonelessChangeDetection()],
  });
}

describe('App shell', () => {
  it('renders the branding and a skip link', async () => {
    await renderShell();

    expect(screen.getByText('Plan who is in which office, on which day.')).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Skip to main content' })).toBeTruthy();
    expect(screen.getByRole('main')).toBeTruthy();
  });

  it('switches the rendered language at runtime', async () => {
    const { fixture } = await renderShell();

    expect(screen.getByText('Plan who is in which office, on which day.')).toBeTruthy();

    fixture.debugElement.injector.get(TranslocoService).setActiveLang('de');
    await fixture.whenStable();

    expect(screen.getByText('Planen Sie, wer an welchem Tag in welchem Büro ist.')).toBeTruthy();
  });

  it('has no detectable accessibility violations', async () => {
    const { container } = await renderShell();

    const results = await axe.run(container);

    expect(results.violations).toEqual([]);
  });
});
