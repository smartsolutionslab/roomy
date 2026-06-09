import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { SessionService } from '@roomy/shared-data-access';

import { LanguageSwitcher } from './shell/language-switcher';

@Component({
  selector: 'roomy-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, TranslocoDirective, LanguageSwitcher],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly document = inject(DOCUMENT);
  private readonly transloco = inject(TranslocoService);
  private readonly session = inject(SessionService);

  protected readonly currentUser = this.session.currentUser;

  constructor() {
    this.transloco.langChanges$
      .pipe(takeUntilDestroyed())
      .subscribe((language) => this.document.documentElement.setAttribute('lang', language));

    this.session.load();
  }
}
