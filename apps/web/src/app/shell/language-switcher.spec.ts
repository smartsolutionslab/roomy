import { provideZonelessChangeDetection } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';

import { importTestTransloco } from '../i18n/transloco-testing';

import { LanguageSwitcher } from './language-switcher';

async function renderSwitcher() {
  const renderResult = await render(LanguageSwitcher, {
    imports: [importTestTransloco()],
    providers: [provideZonelessChangeDetection()],
  });

  const transloco = renderResult.fixture.debugElement.injector.get(TranslocoService);

  return { ...renderResult, transloco };
}

describe('LanguageSwitcher', () => {
  it('marks the active language as pressed', async () => {
    await renderSwitcher();

    // German is the default language, so its option is active and the labels render in German.
    expect(screen.getByRole('button', { name: 'Deutsch', pressed: true })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Englisch', pressed: false })).toBeTruthy();
  });

  it('activates the chosen language when its option is clicked', async () => {
    const { transloco } = await renderSwitcher();

    await userEvent.click(screen.getByRole('button', { name: 'Englisch' }));

    expect(transloco.getActiveLang()).toBe('en');
  });
});
