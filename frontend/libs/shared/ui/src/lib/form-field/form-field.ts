import { ChangeDetectionStrategy, Component, input } from '@angular/core';

// A labelled form control: renders a <label> wrapping the projected control, so clicking the label
// focuses it (implicit association — no id wiring needed). Replaces the repeated per-page
// `.x__field { display:flex; flex-direction:column; gap }` label/input blocks. The label text is
// passed in by the page (already localized); the directive stays locale-agnostic.
@Component({
  selector: 'roomy-form-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './form-field.html',
  styleUrl: './form-field.css',
})
export class FormField {
  readonly label = input.required<string>();
}
