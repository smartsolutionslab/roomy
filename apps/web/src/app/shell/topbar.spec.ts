import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';

import { importTestTransloco } from '../i18n/transloco-testing';

import { Topbar } from './topbar';

describe('Topbar', () => {
  it('renders a banner with the brand and a sign-in call to action', async () => {
    const { container } = await render(Topbar, {
      imports: [importTestTransloco()],
      providers: [provideZonelessChangeDetection(), provideRouter([])],
    });

    expect(screen.getByRole('banner')).toBeTruthy();
    expect(container.querySelector('a[href="/bff/login?returnUrl=/"]')).toBeTruthy();
  });
});
