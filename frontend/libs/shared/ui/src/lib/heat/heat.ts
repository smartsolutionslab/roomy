import { Directive, computed, input } from '@angular/core';

// Maps a fullness ratio to a traffic-light tint: 0 (free) → green, 1 (full) → red, ~0.5 → yellow, by
// sweeping the hue from 120° to 0°. `null` (unknown) yields no tint. The colour is translucent so it
// reads as a subtle cell background over either theme and keeps the cell's text legible. Out-of-range
// input is clamped.
export function heatColor(fullness: number | null): string | null {
  if (fullness === null) {
    return null;
  }
  const clamped = Math.min(1, Math.max(0, fullness));
  const hue = Math.round(120 * (1 - clamped));
  return `hsla(${hue}, 70%, 45%, 0.18)`;
}

// Tints the host element by how full it is (e.g. a calendar day cell): green when free, red when full,
// yellow in between. `null` leaves the host untinted (unknown occupancy). Presentational (type:ui) — the
// caller supplies the fullness ratio; the host's own text stays the accessible source of the figure.
@Directive({
  selector: '[roomyHeat]',
  host: {
    '[style.background-color]': 'background()',
  },
})
export class Heat {
  readonly roomyHeat = input.required<number | null>();

  protected readonly background = computed(() => heatColor(this.roomyHeat()));
}
