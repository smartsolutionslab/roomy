import { ChangeDetectionStrategy, Component, input } from '@angular/core';

// An inline feedback line for forms/pages. `error` announces assertively (role="alert", danger
// colour); `status` (default) is a polite live region for success/confirmation text. Projects the
// already-localized message. Replaces the per-page `__error` and `__result`/`__status` elements,
// which had ad-hoc roles and a repeated danger colour.
@Component({
  selector: 'roomy-message',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  styleUrl: './message.css',
  host: {
    class: 'message',
    '[class.message--error]': "variant() === 'error'",
    '[attr.role]': "variant() === 'error' ? 'alert' : 'status'",
    '[attr.aria-live]': "variant() === 'error' ? null : 'polite'",
  },
})
export class Message {
  readonly variant = input<'error' | 'status'>('status');
}
