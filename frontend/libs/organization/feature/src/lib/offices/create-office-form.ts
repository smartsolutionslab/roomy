import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { Button, FieldError, FormField } from '@roomy/shared-ui';

export type CreateOfficeFormGroup = FormGroup<{
  name: FormControl<string>;
  location: FormControl<string>;
}>;

@Component({
  selector: 'roomy-create-office-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, ReactiveFormsModule, FormField, FieldError, Button],
  templateUrl: './create-office-form.html',
})
export class CreateOfficeForm {
  readonly form = input.required<CreateOfficeFormGroup>();
  readonly conflict = input(false);
  readonly failed = input(false);
  readonly submitted = output();
}
