import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

// One selectable day: the opaque `value` (the day the caller works with, e.g. an ISO date) plus the
// already-localized `weekday` (first line) and `date` (second line) shown on its tile.
export interface DayOption {
  readonly value: string;
  readonly weekday: string;
  readonly date: string;
}

// A single-select day picker rendered as a row of tiles — each a toggle button showing the weekday over
// the date, the chosen one filled with the accent (`aria-pressed`). On small screens (smartphones,
// <=640px) it falls back to a native dropdown of the same days. Presentational (`type:ui`) and
// controlled — the caller passes the already-localized `label`/`placeholder` and `options` and the
// selected `value`, and receives the chosen day (the empty string when the placeholder is reselected).
@Component({
  selector: 'roomy-day-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './day-select.html',
  styleUrl: './day-select.css',
})
export class DaySelect {
  readonly label = input.required<string>();
  readonly placeholder = input.required<string>();
  readonly options = input.required<readonly DayOption[]>();
  readonly value = input<string | null>(null);
  readonly daySelected = output<string>();
}
