import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { Office, OfficeId, Room, RoomId } from '@roomy/organization-api';
import { Button, FormField, Message } from '@roomy/shared-ui';

import { RoomRow, TextFormGroup } from './room-row';

export type ActiveEditor =
  | { kind: 'rename-office'; officeId: OfficeId }
  | { kind: 'relocate-office'; officeId: OfficeId }
  | { kind: 'add-room'; officeId: OfficeId }
  | { kind: 'rename-room'; officeId: OfficeId; roomId: RoomId };

export type RoomFormGroup = FormGroup<{
  name: FormControl<string>;
  capacity: FormControl<number>;
}>;

@Component({
  selector: 'roomy-office-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, ReactiveFormsModule, FormField, Message, Button, RoomRow],
  templateUrl: './office-card.html',
  styleUrl: './office-card.css',
})
export class OfficeCard {
  readonly office = input.required<Office>();
  readonly editor = input<ActiveEditor | null>(null);
  readonly textForm = input.required<TextFormGroup>();
  readonly roomForm = input.required<RoomFormGroup>();
  readonly roomAttempted = input(false);
  readonly editConflict = input(false);
  readonly editFailed = input(false);

  readonly renameOpen = output();
  readonly relocateOpen = output();
  readonly addRoomOpen = output();
  readonly nameSave = output();
  readonly locationSave = output();
  readonly roomAdd = output();
  readonly roomRenameOpen = output<Room>();
  readonly roomNameSave = output<Room>();
  readonly editCancel = output();

  protected readonly isRenamingOffice = computed(() => this.isOfficeEditor('rename-office'));
  protected readonly isRelocatingOffice = computed(() => this.isOfficeEditor('relocate-office'));
  protected readonly isAddingRoom = computed(() => this.isOfficeEditor('add-room'));

  protected isRenamingRoom(room: Room): boolean {
    const editor = this.editor();
    return (
      editor?.kind === 'rename-room' &&
      editor.officeId === this.office().id &&
      editor.roomId === room.id
    );
  }

  private isOfficeEditor(kind: 'rename-office' | 'relocate-office' | 'add-room'): boolean {
    const editor = this.editor();
    return editor?.kind === kind && editor.officeId === this.office().id;
  }
}
