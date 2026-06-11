import { provideZonelessChangeDetection, signal } from '@angular/core';
import { provideRouter, withDisabledInitialNavigation } from '@angular/router';
import { CurrentUser, SessionService } from '@roomy/shared-data-access';
import { render, screen } from '@testing-library/angular';

import { appRoutes } from '../app.routes';
import { importTestTransloco } from '../i18n/transloco-testing';

import { Home } from './home';

async function renderHome(session: CurrentUser | null) {
  return render(Home, {
    imports: [importTestTransloco()],
    providers: [
      provideZonelessChangeDetection(),
      // The dashboard cards are built from the same nav model as the sidebar, so the test needs the
      // real routes; initial navigation is disabled to keep it isolated from the feature pages.
      provideRouter(appRoutes, withDisabledInitialNavigation()),
      { provide: SessionService, useValue: { currentUser: signal(session) } },
    ],
  });
}

describe('Home dashboard', () => {
  it('offers the self-service destinations as cards to any signed-in employee', async () => {
    await renderHome({ name: 'Grace Hopper', roles: ['employee'] });

    expect(screen.getByRole('link', { name: /Platz reservieren/ })).toBeTruthy();
    expect(screen.getByRole('link', { name: /Meine Reservierungen/ })).toBeTruthy();
    expect(screen.getByRole('link', { name: /Belegung/ })).toBeTruthy();
    expect(screen.getByRole('link', { name: /Kalender/ })).toBeTruthy();
  });

  it('adds the administrator destinations for an administrator', async () => {
    await renderHome({ name: 'Ada Lovelace', roles: ['employee', 'administrator'] });

    expect(screen.getByRole('link', { name: /Im Namen/ })).toBeTruthy();
    expect(screen.getByRole('link', { name: /Büros/ })).toBeTruthy();
    expect(screen.getByRole('link', { name: /Konten/ })).toBeTruthy();
  });

  it('does not offer the administrator destinations to a non-administrator', async () => {
    await renderHome({ name: 'Grace Hopper', roles: ['employee'] });

    expect(screen.queryByRole('link', { name: /Büros/ })).toBeNull();
    expect(screen.queryByRole('link', { name: /Konten/ })).toBeNull();
  });

  it('shows the sign-in hero, and no destination cards, when signed out', async () => {
    await renderHome(null);

    expect(screen.getByRole('link', { name: 'Anmelden' })).toBeTruthy();
    expect(screen.queryByRole('link', { name: /Platz reservieren/ })).toBeNull();
  });
});
