import { isDevMode, Provider } from '@angular/core';
import { provideTransloco } from '@jsverse/transloco';

import { availableLanguages, defaultLanguage } from './available-languages';
import { TranslationLoader } from './translation.loader';

export function provideAppTransloco(): Provider[] {
  return [
    provideTransloco({
      config: {
        availableLangs: [...availableLanguages],
        defaultLang: defaultLanguage,
        fallbackLang: defaultLanguage,
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslationLoader,
    }),
  ];
}
