import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

// `brand` is the flat burnt-orange tile (shell header, sidebar); `sunset` is the orange→amber→rose
// hero flourish, matching --roomy-gradient-accent.
export type RoomyLogoVariant = 'brand' | 'sunset';

// The Roomy brand mark: a geometric white "R" monogram on a burnt-orange rounded tile, rendered as
// inline SVG so it stays crisp at any size and the wordmark inherits the current text colour (theme
// aware in light/dark). The mark doubles as the leading R, so the visible wordmark reads "oomy"; a
// visually-hidden R keeps the full "Roomy" name for assistive tech. "Roomy" is a product logotype, not
// UI copy, so it is not translated (the same string in every locale). With `showWordmark` off the mark
// still carries an accessible "Roomy" name via a
// visually-hidden label. Size, corner radius and shadow are overridable via the
// --roomy-logo-size/-radius/-shadow custom properties (the hero scales it up).
@Component({
  selector: 'roomy-logo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './logo.html',
  styleUrl: './logo.css',
})
export class RoomyLogo {
  readonly showWordmark = input<boolean>(false);

  // Inverts the mark for use on a coloured (accent) surface: a white tile with an orange "R".
  // The wordmark follows `currentColor`, so the host sets it (e.g. white on an orange sidebar).
  readonly onAccent = input<boolean>(false);

  readonly variant = input<RoomyLogoVariant>('brand');

  // Scoped per variant so a brand and a sunset mark on the same page don't share one <linearGradient>
  // id — SVG resolves url(#id) to the first match document-wide; same-variant marks share an identical
  // definition, so reusing the id between them is harmless.
  protected readonly gradientId = computed(() => `roomyLogoTile-${this.variant()}`);

  protected readonly tileFill = computed(() =>
    this.onAccent() ? '#ffffff' : `url(#${this.gradientId()})`,
  );
}
