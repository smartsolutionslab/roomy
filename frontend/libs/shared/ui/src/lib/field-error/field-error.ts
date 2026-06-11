import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { Message } from '../message/message';

// An inline error note for a form field or form: renders the already-translated message as an error
// roomy-message while `show` is true, nothing otherwise. The caller owns the condition (e.g. a touched
// invalid control or a server conflict flag) and the translation, per the design-system convention.
@Component({
  selector: 'roomy-field-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Message],
  template: `@if (show()) {
    <roomy-message variant="error">{{ message() }}</roomy-message>
  }`,
})
export class FieldError {
  readonly show = input(false);
  readonly message = input.required<string>();
}
