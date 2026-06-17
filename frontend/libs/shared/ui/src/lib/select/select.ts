import {
  ChangeDetectionStrategy,
  Component,
  forwardRef,
  input,
  output,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { FormField } from '../form-field/form-field';

// One option of a roomy-select: an opaque value plus its already-translated (or data-derived) label.
// `detail` is an optional secondary line a tile renders under the label (used by the day picker).
export interface SelectOption {
  readonly value: string;
  readonly label: string;
  readonly detail?: string;
}

// A labelled dropdown built on FormField: a placeholder followed by the given options. The caller passes
// the already-translated label and placeholder plus the {value,label} options. It works two ways: bind a
// reactive form (`formControlName`) to drive and read the selection, or listen to `selected` for the chosen
// value (the empty string when the placeholder is reselected). Use roomy-day-select for a plain list of days.
@Component({
  selector: 'roomy-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField],
  templateUrl: './select.html',
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => Select), multi: true }],
})
export class Select implements ControlValueAccessor {
  readonly label = input.required<string>();
  readonly placeholder = input.required<string>();
  readonly options = input.required<readonly SelectOption[]>();
  readonly selected = output<string>();

  protected readonly value = signal('');
  protected readonly disabled = signal(false);

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  protected choose(value: string): void {
    this.value.set(value);
    this.selected.emit(value);
    this.onChange(value);
  }

  writeValue(value: string | null): void {
    this.value.set(value ?? '');
  }

  registerOnChange(onChange: (value: string) => void): void {
    this.onChange = onChange;
  }

  registerOnTouched(onTouched: () => void): void {
    this.onTouched = onTouched;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected markTouched(): void {
    this.onTouched();
  }
}
