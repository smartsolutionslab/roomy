import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { FormField } from '../form-field/form-field';

// One option of a roomy-select: an opaque value plus its already-translated (or data-derived) label.
export interface SelectOption {
  readonly value: string;
  readonly label: string;
}

// A labelled dropdown built on FormField: a placeholder followed by the given options. Presentational
// (type:ui) and uncontrolled like roomy-day-select — the caller passes the already-translated label and
// placeholder plus the {value,label} options, and receives the chosen value (the empty string when the
// placeholder is reselected). Use roomy-day-select for a plain list of day strings.
@Component({
  selector: 'roomy-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField],
  templateUrl: './select.html',
})
export class Select {
  readonly label = input.required<string>();
  readonly placeholder = input.required<string>();
  readonly options = input.required<readonly SelectOption[]>();
  readonly selected = output<string>();
}
