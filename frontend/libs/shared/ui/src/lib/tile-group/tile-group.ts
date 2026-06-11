import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import type { SelectOption } from '../select/select';

// A single-select control rendered as a group of tiles instead of a dropdown: each option is a toggle
// button, the chosen one filled with the primary accent (`aria-pressed`). On small screens (smartphones,
// <=640px) it falls back to a native dropdown of the same options. Presentational (type:ui) and
// controlled — the caller passes the already-translated `label`/options and the selected `value`, and
// receives the chosen value. `orientation` lays the tiles out in a row (default) or a column; an optional
// `placeholder` adds an empty entry to the dropdown fallback for the "nothing chosen yet" state.
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
  readonly placeholder = input<string | null>(null);
  readonly orientation = input<'horizontal' | 'vertical'>('horizontal');
  readonly selected = output<string>();
}
