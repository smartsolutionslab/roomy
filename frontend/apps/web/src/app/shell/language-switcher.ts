import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';

import { availableLanguages, defaultLanguage, LanguageCode } from '../i18n/available-languages';

@Component({
  selector: 'roomy-language-switcher',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective],
  templateUrl: './language-switcher.html',
  styleUrl: './language-switcher.css',
})
export class LanguageSwitcher {
  private readonly transloco = inject(TranslocoService);

  protected readonly languages = availableLanguages;
  protected readonly activeLanguage = signal<LanguageCode>(
    (this.transloco.getActiveLang() as LanguageCode) ?? defaultLanguage,
  );

  protected selectLanguage(language: LanguageCode): void {
    this.transloco.setActiveLang(language);
    this.activeLanguage.set(language);
  }
}
