import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { ThemeService } from '@roomy/shared-data-access';

// A single accessible control that flips the colour theme. The button's accessible name announces the
// action it performs (switch to the other theme) and `aria-pressed` reflects whether dark is active, so
// it works for keyboard and screen-reader users (WCAG 2.2 AA). The icon is decorative (aria-hidden).
@Component({
  selector: 'roomy-theme-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective],
  templateUrl: './theme-toggle.html',
  styleUrl: './theme-toggle.css',
})
export class ThemeToggle {
  private readonly themeService = inject(ThemeService);

  protected readonly isDark = computed(() => this.themeService.theme() === 'dark');

  protected toggle(): void {
    this.themeService.toggle();
  }
}
