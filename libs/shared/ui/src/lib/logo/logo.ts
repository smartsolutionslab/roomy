import { ChangeDetectionStrategy, Component, input } from '@angular/core';

// The Roomy brand mark: a geometric white "R" monogram on a burnt-orange rounded tile, rendered as
// inline SVG so it stays crisp at any size and the wordmark inherits the current text colour (theme
// aware in light/dark). "Roomy" is a product logotype, not UI copy, so it is not translated (it is the
// same string in every locale — see shell.appName, ADR-0024). With `showWordmark` off the mark still
// carries an accessible "Roomy" name via a visually-hidden label.
@Component({
  selector: 'roomy-logo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './logo.html',
  styleUrl: './logo.css',
})
export class RoomyLogo {
  readonly showWordmark = input<boolean>(false);
}
