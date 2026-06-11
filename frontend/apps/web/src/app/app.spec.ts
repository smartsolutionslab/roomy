import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withDisabledInitialNavigation } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { CurrentUser } from '@roomy/shared-data-access';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import axe from 'axe-core';

import { App } from './app';
import { appRoutes } from './app.routes';
import { importTestTransloco } from './i18n/transloco-testing';

async function renderShell(session: CurrentUser | null = null) {
  const view = await render(App, {
    imports: [importTestTransloco()],
    providers: [
      provideZonelessChangeDetection(),
      // The real routes carry the nav metadata the sidebar is built from; initial navigation is
      // disabled so the shell test stays isolated from the feature pages' lazy loads.
      provideRouter(appRoutes, withDisabledInitialNavigation()),
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

    // The public view offers sign-in both in the top bar and as the landing hero call to action.
    expect(screen.getAllByRole('link', { name: 'Anmelden' }).length).toBeGreaterThan(0);
  });

  it('shows the signed-in user and a sign-out control behind the account menu', async () => {
    await renderShell({ name: 'Ada Lovelace', roles: ['employee'] });

    // The account avatar opens a menu revealing the user's name and a sign-out action.
    await userEvent.click(screen.getByRole('button', { name: 'Kontomenü' }));

    expect(screen.getByText('Ada Lovelace')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Abmelden' })).toBeTruthy();
  });

  it('shows the self-service destinations to any signed-in employee', async () => {
    await renderShell({ name: 'Grace Hopper', roles: ['employee'] });

    expect(screen.getByRole('link', { name: 'Platz reservieren' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Meine Reservierungen' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Belegung' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Kalender' })).toBeTruthy();
  });

  it('offers the administration section to administrators', async () => {
    await renderShell({ name: 'Ada Lovelace', roles: ['employee', 'administrator'] });

    expect(screen.getByRole('link', { name: 'Im Namen' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Büros' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Verwaltung' })).toBeTruthy();
  });

  it('collapses and expands the administration group of sub-items', async () => {
    await renderShell({ name: 'Ada Lovelace', roles: ['employee', 'administrator'] });

    const toggle = screen.getByRole('button', { name: 'Verwaltung' });
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    expect(screen.getByRole('link', { name: 'Büros' })).toBeTruthy();

    await userEvent.click(toggle);

    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(screen.queryByRole('link', { name: 'Büros' })).toBeNull();

    await userEvent.click(toggle);

    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    expect(screen.getByRole('link', { name: 'Büros' })).toBeTruthy();
  });

  it('does not offer the administrator-only links to a non-administrator', async () => {
    await renderShell({ name: 'Grace Hopper', roles: ['employee'] });

    expect(screen.queryByRole('link', { name: 'Verwaltung' })).toBeNull();
    expect(screen.queryByRole('link', { name: 'Im Namen' })).toBeNull();
    expect(screen.queryByRole('link', { name: 'Büros' })).toBeNull();
  });

  it('has no detectable accessibility violations', async () => {
    const { container } = await renderShell();

    const results = await axe.run(container);

    expect(results.violations).toEqual([]);
  });
});
