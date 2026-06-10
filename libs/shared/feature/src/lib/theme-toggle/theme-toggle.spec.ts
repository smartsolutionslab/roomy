import { provideZonelessChangeDetection } from '@angular/core';
import { ThemeService } from '@roomy/shared-data-access';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { importSharedTestTransloco } from '../../testing/transloco';

import { ThemeToggle } from './theme-toggle';

async function renderToggle() {
  const result = await render(ThemeToggle, {
    imports: [importSharedTestTransloco()],
    providers: [provideZonelessChangeDetection()],
  });
  return { ...result, themeService: result.fixture.debugElement.injector.get(ThemeService) };
}

describe('ThemeToggle', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('offers switching to dark while the light theme is active', async () => {
    await renderToggle();

    const button = await screen.findByRole('button', { name: 'Switch to dark theme' });
    expect(button.getAttribute('aria-pressed')).toBe('false');
  });

  it('flips the theme and its accessible name when activated', async () => {
    const { themeService } = await renderToggle();

    await userEvent.click(screen.getByRole('button', { name: 'Switch to dark theme' }));

    expect(themeService.theme()).toBe('dark');
    const button = await screen.findByRole('button', { name: 'Switch to light theme' });
    expect(button.getAttribute('aria-pressed')).toBe('true');
  });
});
