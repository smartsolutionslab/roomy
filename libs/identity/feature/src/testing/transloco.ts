import { ModuleWithProviders } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';

// Test-only Transloco setup for this library's components. It carries just the keys the identity
// screens use, so the library's specs stay independent of the app's global translation files
// (importing those would cross the context:web boundary). Excluded from the library build.
const english = {
  account: {
    title: 'My account',
    nameLabel: 'Name',
    emailLabel: 'Email',
    roleLabel: 'Role',
    loading: 'Loading your account…',
    error: 'We could not load your account.',
    role: {
      employee: 'Employee',
      administrator: 'Administrator',
    },
  },
};

const german = {
  account: {
    title: 'Mein Konto',
    nameLabel: 'Name',
    emailLabel: 'E-Mail',
    roleLabel: 'Rolle',
    loading: 'Konto wird geladen…',
    error: 'Konto konnte nicht geladen werden.',
    role: {
      employee: 'Mitarbeiter',
      administrator: 'Administrator',
    },
  },
};

export function importIdentityTestTransloco(): ModuleWithProviders<TranslocoTestingModule> {
  return TranslocoTestingModule.forRoot({
    langs: { en: english, de: german },
    translocoConfig: {
      availableLangs: ['en', 'de'],
      defaultLang: 'en',
      reRenderOnLangChange: true,
    },
    preloadLangs: true,
  });
}
