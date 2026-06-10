import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { SessionService } from '@roomy/shared-data-access';
import { ThemeToggle, UserMenu } from '@roomy/shared-feature';
import { RoomyLogo } from '@roomy/shared-ui';

import { LanguageSwitcher } from './shell/language-switcher';

@Component({
  selector: 'roomy-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslocoDirective,
    LanguageSwitcher,
    ThemeToggle,
    UserMenu,
    RoomyLogo,
  ],
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
