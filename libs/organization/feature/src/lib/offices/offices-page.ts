import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { Office, OfficesGateway } from '@roomy/organization-data-access';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'roomy-offices-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, ReactiveFormsModule],
  templateUrl: './offices-page.html',
  styleUrl: './offices-page.css',
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
  protected readonly createdName = signal<string | null>(null);

  constructor() {
    this.officesGateway
      .listOffices()
      .pipe(
        takeUntilDestroyed(),
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
          this.createdName.set(office.name);
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
}
