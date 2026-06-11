import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { Room } from '@roomy/organization-api';
import { Button, FormField, Message } from '@roomy/shared-ui';

export type TextFormGroup = FormGroup<{ value: FormControl<string> }>;

@Component({
  selector: 'roomy-room-row',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, ReactiveFormsModule, FormField, Message, Button],
  templateUrl: './room-row.html',
  styleUrl: './room-row.css',
})
export class RoomRow {
  readonly room = input.required<Room>();
  readonly renaming = input(false);
  readonly form = input.required<TextFormGroup>();
  readonly conflict = input(false);
  readonly failed = input(false);
  readonly rename = output();
  readonly save = output();
  readonly cancelEdit = output();
}
