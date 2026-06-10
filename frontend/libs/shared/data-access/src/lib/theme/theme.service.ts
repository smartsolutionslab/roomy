import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark';

// Owns the light/dark colour theme. The resolved preference is reflected onto a `data-theme` attribute
// on <html>, which the global token sheet keys off (styles.css). On first visit with no stored choice it
// follows the OS `prefers-color-scheme`; an explicit toggle is persisted to localStorage and wins from
// then on. Access to `localStorage`/`matchMedia` goes through the injected document and is guarded so the
// service is safe under zoneless/SSR where those globals may be absent.
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private static readonly storageKey = 'roomy.theme';

  private readonly document = inject(DOCUMENT);
  private readonly themeState = signal<ThemePreference>(this.resolveInitialTheme());

  readonly theme = this.themeState.asReadonly();

  constructor() {
    this.apply(this.themeState());
  }

  toggle(): void {
    this.set(this.themeState() === 'dark' ? 'light' : 'dark');
  }

  set(theme: ThemePreference): void {
    this.themeState.set(theme);
    this.persist(theme);
    this.apply(theme);
  }

  private resolveInitialTheme(): ThemePreference {
    const stored = this.readStoredTheme();
    if (stored) return stored;

    const prefersDark = this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)');
    return prefersDark?.matches ? 'dark' : 'light';
  }

  private apply(theme: ThemePreference): void {
    this.document.documentElement.setAttribute('data-theme', theme);
  }

  private readStoredTheme(): ThemePreference | null {
    try {
      const stored = this.document.defaultView?.localStorage?.getItem(ThemeService.storageKey);
      return stored === 'light' || stored === 'dark' ? stored : null;
    } catch {
      return null;
    }
  }

  private persist(theme: ThemePreference): void {
    try {
      this.document.defaultView?.localStorage?.setItem(ThemeService.storageKey, theme);
    } catch {
      // Storage unavailable (private mode / SSR): the choice simply is not remembered across reloads.
    }
  }
}
