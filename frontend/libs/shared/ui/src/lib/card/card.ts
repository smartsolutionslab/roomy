import { booleanAttribute, Directive, input } from '@angular/core';

// The design-system card surface (frosted glass) applied to any element. Add `interactive` when the
// card is itself a link/button so it gets the hover lift + accent border. Styling is global; this
// directive is the typed API.
@Directive({
  selector: '[roomyCard]',
  host: {
    class: 'roomy-card',
    '[class.roomy-card--interactive]': 'interactive()',
  },
})
export class Card {
  readonly interactive = input(false, { transform: booleanAttribute });
}
