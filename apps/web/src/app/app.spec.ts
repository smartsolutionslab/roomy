import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoService } from '@jsverse/transloco';
import { render, screen } from '@testing-library/angular';
import axe from 'axe-core';

import { App } from './app';
import { importTestTransloco } from './i18n/transloco-testing';
import { CurrentUser } from './session/current-user';

async function renderShell(session: CurrentUser | null = null) {
  const view = await render(App, {
    imports: [importTestTransloco()],
    providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
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

  it('offers a sign-in link when there is no session', async () => {
    await renderShell(null);

    expect(screen.getByRole('link', { name: 'Sign in' })).toBeTruthy();
  });

  it('shows the signed-in user and a sign-out control when the BFF returns one', async () => {
    await renderShell({ name: 'Ada Lovelace', roles: ['employee'] });

    expect(screen.getByText('Signed in as Ada Lovelace')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Sign out' })).toBeTruthy();
  });

  it('has no detectable accessibility violations', async () => {
    const { container } = await renderShell();

    const results = await axe.run(container);

    expect(results.violations).toEqual([]);
  });
});
