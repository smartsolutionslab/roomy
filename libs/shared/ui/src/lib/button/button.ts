import { Directive, input } from '@angular/core';

// Applies the design-system button styling to a native <button> or <a> (keeping native semantics —
// form submit, links). `variant="accent"` is the primary call-to-action. The classes live in the
// global design-system stylesheet (ADR-0048); this directive is the typed, discoverable API.
@Directive({
  selector: '[roomyButton]',
  host: {
    class: 'roomy-button',
    '[class.roomy-button--accent]': "variant() === 'accent'",
  },
})
export class Button {
  readonly variant = input<'default' | 'accent'>('default');
}
