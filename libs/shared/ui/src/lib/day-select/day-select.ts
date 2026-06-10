import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { FormField } from '../form-field/form-field';

// A labelled day-selection dropdown built on FormField: a placeholder followed by the selectable day
// values. Presentational (`type:ui`) — the caller passes the already-translated label/placeholder and the
// day strings, and receives the chosen day (the empty string when the placeholder is reselected).
@Component({
  selector: 'roomy-day-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField],
  templateUrl: './day-select.html',
})
export class DaySelect {
  readonly label = input.required<string>();
  readonly placeholder = input.required<string>();
  readonly days = input.required<readonly string[]>();
  readonly daySelected = output<string>();
}
