import { ModuleWithProviders } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';

// Test-only Transloco setup for this library's components. It carries just the keys the shared UI
// primitives use, so the library's specs stay independent of the app's global translation files
// (importing those would cross the context:web boundary). Excluded from the library build. Kept in key
// parity with apps/web/public/i18n/{en,de}.json under shared.list (ADR-0024).
const english = {
  shared: {
    list: {
      loadMore: 'Load more',
      loading: 'Loading…',
      endOfList: 'End of list',
    },
  },
};

const german = {
  shared: {
    list: {
      loadMore: 'Mehr laden',
      loading: 'Wird geladen…',
      endOfList: 'Ende der Liste',
    },
  },
};

export function importSharedUiTestTransloco(): ModuleWithProviders<TranslocoTestingModule> {
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
