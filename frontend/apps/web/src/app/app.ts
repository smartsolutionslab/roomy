import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { SessionService } from '@roomy/shared-data-access';
import { ThemeToggle, UserMenu } from '@roomy/shared-feature';
import { AppLayout, Icon, RoomyLogo } from '@roomy/shared-ui';

import { LanguageSwitcher } from './shell/language-switcher';
import { Topbar } from './shell/topbar';

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
    AppLayout,
    Icon,
    Topbar,
  ],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly document = inject(DOCUMENT);
  private readonly transloco = inject(TranslocoService);
  private readonly session = inject(SessionService);

  protected readonly currentUser = this.session.currentUser;
  private readonly activeLang = toSignal(this.transloco.langChanges$, {
    initialValue: this.transloco.getActiveLang(),
  });

  constructor() {
    // Reflect the active language onto <html lang> for assistive tech; the effect also sets it on the
    // first run, not only on a later change.
    effect(() => this.document.documentElement.setAttribute('lang', this.activeLang()));

    this.session.load();
  }
}
