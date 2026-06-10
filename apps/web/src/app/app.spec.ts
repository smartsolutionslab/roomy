import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { CurrentUser } from '@roomy/shared-data-access';
import { render, screen } from '@testing-library/angular';
import axe from 'axe-core';

import { App } from './app';
import { importTestTransloco } from './i18n/transloco-testing';

async function renderShell(session: CurrentUser | null = null) {
  const view = await render(App, {
    imports: [importTestTransloco()],
    providers: [
      provideZonelessChangeDetection(),
      provideRouter([]),
      provideHttpClient(),
      provideHttpClientTesting(),
    ],
  });

  // The shell asks the BFF who is signed in on init (ADR-0030); answer that one request.
  const request = TestBed.inject(HttpTestingController).expectOne('/bff/user');
  if (session) {
    request.flush(session);
  } else {
    request.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
  }
  await view.fixture.whenStable();

  return view;
}

describe('App shell', () => {
  it('renders the branding and a skip link', async () => {
    await renderShell();

    // German is the default language (ADR-0045 / spec 012), so the shell renders in German.
    expect(screen.getByText('Planen Sie, wer an welchem Tag in welchem Büro ist.')).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Zum Hauptinhalt springen' })).toBeTruthy();
    expect(screen.getByRole('main')).toBeTruthy();
  });

  it('switches the rendered language at runtime', async () => {
    const { fixture } = await renderShell();

    expect(screen.getByText('Planen Sie, wer an welchem Tag in welchem Büro ist.')).toBeTruthy();

    fixture.debugElement.injector.get(TranslocoService).setActiveLang('en');
    await fixture.whenStable();

    expect(screen.getByText('Plan who is in which office, on which day.')).toBeTruthy();
  });

  it('offers a sign-in link when there is no session', async () => {
    await renderShell(null);

    expect(screen.getByRole('link', { name: 'Anmelden' })).toBeTruthy();
  });

  it('shows the signed-in user and a sign-out control when the BFF returns one', async () => {
    await renderShell({ name: 'Ada Lovelace', roles: ['employee'] });

    expect(screen.getByText('Angemeldet als Ada Lovelace')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Abmelden' })).toBeTruthy();
  });

  it('offers an administration link to administrators', async () => {
    await renderShell({ name: 'Ada Lovelace', roles: ['employee', 'administrator'] });

    expect(screen.getByRole('link', { name: 'Verwaltung' })).toBeTruthy();
  });

  it('does not offer the administration link to a non-administrator', async () => {
    await renderShell({ name: 'Grace Hopper', roles: ['employee'] });

    expect(screen.queryByRole('link', { name: 'Verwaltung' })).toBeNull();
  });

  it('has no detectable accessibility violations', async () => {
    const { container } = await renderShell();

    const results = await axe.run(container);

    expect(results.violations).toEqual([]);
  });
});
