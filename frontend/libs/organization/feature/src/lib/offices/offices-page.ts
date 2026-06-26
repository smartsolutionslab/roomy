import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { Office, OfficeId, OfficesGateway, Room, RoomId } from '@roomy/organization-api';
import { type ResultMessage } from '@roomy/shared-data-access';
import { Card, Message, Page } from '@roomy/shared-ui';
import { Observable, catchError, of } from 'rxjs';

import { CreateOfficeForm } from './create-office-form';
import { ActiveEditor, OfficeCard } from './office-card';

@Component({
  selector: 'roomy-offices-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, Page, Card, Message, CreateOfficeForm, OfficeCard],
  templateUrl: './offices-page.html',
})
export class OfficesPage {
  private readonly officesGateway = inject(OfficesGateway);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly offices = signal<Office[] | null>(null);
  protected readonly loadFailed = signal(false);

  protected readonly createForm = this.formBuilder.nonNullable.group({
    name: ['', Validators.required],
    location: ['', Validators.required],
  });
  protected readonly createConflict = signal(false);
  protected readonly createFailed = signal(false);

  // One inline editor is open at a time (rename/relocate an office, add a room, rename a room).
  protected readonly activeEditor = signal<ActiveEditor | null>(null);
  protected readonly textForm = this.formBuilder.nonNullable.group({
    value: ['', Validators.required],
  });
  protected readonly roomForm = this.formBuilder.nonNullable.group({
    name: ['', Validators.required],
    capacity: [1, [Validators.required, Validators.min(1)]],
  });
  protected readonly roomAttempted = signal(false);
  protected readonly editConflict = signal(false);
  protected readonly editNotFound = signal(false);
  protected readonly editFailed = signal(false);

  protected readonly result = signal<ResultMessage | null>(null);

  constructor() {
    this.loadOffices(true);
  }

  private loadOffices(initial: boolean): void {
    this.officesGateway
      .listOffices()
      .pipe(
        takeUntilDestroyed(initial ? undefined : this.destroyRef),
        catchError(() => {
          this.loadFailed.set(true);
          return of<Office[]>([]);
        }),
      )
      .subscribe((offices) => this.offices.set(offices));
  }

  protected createOffice(): void {
    if (this.createForm.invalid) {
      return;
    }

    this.createConflict.set(false);
    this.createFailed.set(false);
    const { name, location } = this.createForm.getRawValue();

    this.officesGateway
      .createOffice(name, location)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (office) => {
          this.offices.update((items) => [office, ...(items ?? [])]);
          this.result.set({ key: 'organization.create.created', params: { name: office.name } });
          this.createForm.reset();
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 409) {
            this.createConflict.set(true);
          } else {
            this.createFailed.set(true);
          }
        },
      });
  }

  protected openRenameOffice(office: Office): void {
    this.beginEdit({ kind: 'rename-office', officeId: office.id });
    this.textForm.setValue({ value: office.name });
  }

  protected openRelocateOffice(office: Office): void {
    this.beginEdit({ kind: 'relocate-office', officeId: office.id });
    this.textForm.setValue({ value: office.location });
  }

  protected openAddRoom(office: Office): void {
    this.beginEdit({ kind: 'add-room', officeId: office.id });
    this.roomForm.reset({ name: '', capacity: 1 });
    this.roomAttempted.set(false);
  }

  protected openRenameRoom(office: Office, room: Room): void {
    this.beginEdit({ kind: 'rename-room', officeId: office.id, roomId: room.id });
    this.textForm.setValue({ value: room.name });
  }

  protected cancelEdit(): void {
    this.activeEditor.set(null);
  }

  private beginEdit(editor: ActiveEditor): void {
    this.editConflict.set(false);
    this.editNotFound.set(false);
    this.editFailed.set(false);
    this.activeEditor.set(editor);
  }

  protected saveOfficeName(office: OfficeId): void {
    if (this.textFormInvalid()) {
      return;
    }
    this.submitOfficeChange(
      this.officesGateway.renameOffice(office, this.textForm.getRawValue().value),
    );
  }

  protected saveOfficeLocation(office: OfficeId): void {
    if (this.textFormInvalid()) {
      return;
    }
    this.submitOfficeChange(
      this.officesGateway.relocateOffice(office, this.textForm.getRawValue().value),
    );
  }

  protected saveRoomName(office: OfficeId, room: RoomId): void {
    if (this.textFormInvalid()) {
      return;
    }
    const name = this.textForm.getRawValue().value;
    this.submitOfficeChange(this.officesGateway.renameRoom(office, room, name), {
      key: 'organization.rooms.renamed',
      params: { name },
    });
  }

  protected addRoom(office: OfficeId): void {
    this.roomAttempted.set(true);
    if (this.roomForm.invalid) {
      return;
    }

    this.clearEditFeedback();
    const { name, capacity } = this.roomForm.getRawValue();

    this.officesGateway
      .addRoom(office, name, capacity)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (room) => {
          this.offices.update((items) =>
            (items ?? []).map((candidate) =>
              candidate.id === office
                ? {
                    ...candidate,
                    rooms: [...candidate.rooms, room],
                    capacity: candidate.capacity + room.capacity,
                  }
                : candidate,
            ),
          );
          this.result.set({ key: 'organization.rooms.added', params: { name: room.name } });
          this.activeEditor.set(null);
        },
        error: (error: HttpErrorResponse) => this.handleEditError(error),
      });
  }

  private submitOfficeChange(change: Observable<Office>, result?: ResultMessage): void {
    this.clearEditFeedback();
    change.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (office) => {
        this.offices.update((items) =>
          (items ?? []).map((candidate) => (candidate.id === office.id ? office : candidate)),
        );
        this.result.set(
          result ?? { key: 'organization.edit.updated', params: { name: office.name } },
        );
        this.activeEditor.set(null);
      },
      error: (error: HttpErrorResponse) => this.handleEditError(error),
    });
  }

  private textFormInvalid(): boolean {
    return this.textForm.invalid;
  }

  private clearEditFeedback(): void {
    this.editConflict.set(false);
    this.editNotFound.set(false);
    this.editFailed.set(false);
  }

  private handleEditError(error: HttpErrorResponse): void {
    if (error.status === 409) {
      this.editConflict.set(true);
    } else if (error.status === 404) {
      this.editNotFound.set(true);
      this.activeEditor.set(null);
      this.loadOffices(false);
    } else {
      this.editFailed.set(true);
    }
  }
}
