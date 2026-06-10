import { Directive, input } from '@angular/core';

// The standard feature-page container: a vertical stack with consistent gap and an optional
// max-width (`form` ~ a single form, `content` ~ reading width, `wide` ~ tables/calendars). Replaces
// the per-page `.x { display:flex; flex-direction:column; gap; max-width }` blocks. Global styling
// (ADR-0048).
@Directive({
  selector: '[roomyPage]',
  host: {
    class: 'roomy-page',
    '[class.roomy-page--form]': "size() === 'form'",
    '[class.roomy-page--content]': "size() === 'content'",
    '[class.roomy-page--wide]': "size() === 'wide'",
  },
})
export class Page {
  readonly size = input<'default' | 'form' | 'content' | 'wide'>('default');
}
