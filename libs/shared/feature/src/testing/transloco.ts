import { ModuleWithProviders } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';

// Test-only Transloco setup for this library's components. It carries just the keys the shared
// screens use, so the library's specs stay independent of the app's global translation files
// (importing those would cross the context:web boundary). Excluded from the library build.
const english = {
  shared: {
    notAuthorized: {
      title: 'Not authorized',
      message: 'You do not have permission to view this page.',
    },
    theme: {
      toLight: 'Switch to light theme',
      toDark: 'Switch to dark theme',
    },
  },
};

const german = {
  shared: {
    notAuthorized: {
      title: 'Nicht berechtigt',
      message: 'Sie haben keine Berechtigung, diese Seite anzuzeigen.',
    },
    theme: {
      toLight: 'Zu hellem Design wechseln',
      toDark: 'Zu dunklem Design wechseln',
    },
  },
};

export function importSharedTestTransloco(): ModuleWithProviders<TranslocoTestingModule> {
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
