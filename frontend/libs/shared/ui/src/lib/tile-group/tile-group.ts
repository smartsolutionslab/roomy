import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import type { SelectOption } from '../select/select';

// A single-select control rendered as a horizontal group of tiles instead of a dropdown: each option is
// a toggle button, the chosen one marked `aria-pressed`. Presentational (type:ui) and controlled — the
// caller passes the already-translated `label`/options and the selected `value`, and receives the chosen
// value. Use it over roomy-select when the options are few and worth showing at a glance (e.g. offices).
@Component({
  selector: 'roomy-tile-group',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './tile-group.html',
  styleUrl: './tile-group.css',
})
export class TileGroup {
  readonly label = input.required<string>();
  readonly options = input.required<readonly SelectOption[]>();
  readonly value = input<string | null>(null);
  readonly selected = output<string>();
}
