import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import type { SelectOption } from '../select/select';
import { TileGroup } from '../tile-group/tile-group';

// One selectable day: the opaque `value` (the day the caller works with, e.g. an ISO date) plus the
// already-localized `weekday` (first line) and `date` (second line) shown on its tile.
export interface DayOption {
  readonly value: string;
  readonly weekday: string;
  readonly date: string;
}

// A single-select day picker: a TileGroup whose tiles show the weekday over the date. Presentational
// (`type:ui`) and controlled — the caller passes the already-localized `label`/`placeholder` and
// `options` and the selected `value`, and receives the chosen day.
@Component({
  selector: 'roomy-day-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TileGroup],
  template: `<roomy-tile-group
    [label]="label()"
    [placeholder]="placeholder()"
    [options]="tileOptions()"
    [value]="value()"
    (selected)="selected.emit($event)"
  />`,
})
export class DaySelect {
  readonly label = input.required<string>();
  readonly placeholder = input.required<string>();
  readonly options = input.required<readonly DayOption[]>();
  readonly value = input<string | null>(null);
  readonly selected = output<string>();

  protected readonly tileOptions = computed<readonly SelectOption[]>(() =>
    this.options().map((option) => ({
      value: option.value,
      label: option.weekday,
      detail: option.date,
    })),
  );
}
