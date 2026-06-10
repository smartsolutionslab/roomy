import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { ThemeToggle } from '@roomy/shared-feature';
import { Button, RoomyLogo } from '@roomy/shared-ui';

import { LanguageSwitcher } from './language-switcher';

// The public top bar shown to signed-out visitors: brand, tagline, and the theme/language controls beside
// a sign-in call to action. App-local because it composes the app's own language switcher and BFF sign-in
// route; the signed-in shell uses the AppLayout top slot (with the account menu) instead.
@Component({
  selector: 'roomy-topbar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslocoDirective, RoomyLogo, ThemeToggle, LanguageSwitcher, Button],
  templateUrl: './topbar.html',
  styleUrl: './topbar.css',
})
export class Topbar {}
