import { ModuleWithProviders } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';

import deTranslations from '../../../public/i18n/de.json';
import enTranslations from '../../../public/i18n/en.json';

import { availableLanguages, defaultLanguage } from './available-languages';

export function importTestTransloco(): ModuleWithProviders<TranslocoTestingModule> {
  return TranslocoTestingModule.forRoot({
    langs: { en: enTranslations, de: deTranslations },
    translocoConfig: {
      availableLangs: [...availableLanguages],
      defaultLang: defaultLanguage,
      reRenderOnLangChange: true,
    },
    preloadLangs: true,
  });
}
