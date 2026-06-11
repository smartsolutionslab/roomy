import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { EmployeeRole, EmployeesGateway } from '@roomy/organization-api';
import { Button, FormField, Message, Page, Select } from '@roomy/shared-ui';

@Component({
  selector: 'roomy-hire-employee-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, ReactiveFormsModule, Page, FormField, Select, Message, Button],
  templateUrl: './hire-employee-page.html',
  styleUrl: './hire-employee-page.css',
})
export class HireEmployeePage {
  private readonly employeesGateway = inject(EmployeesGateway);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly hireForm = this.formBuilder.nonNullable.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    role: ['', Validators.required],
    initialPassword: ['', Validators.required],
  });
  protected readonly attempted = signal(false);

  // The hired colleague's name once a 202 lands: provisioning has started, not that they can sign in yet.
  protected readonly hiredName = signal<string | null>(null);
  protected readonly invalid = signal(false);
  protected readonly failed = signal(false);

  protected hire(): void {
    this.attempted.set(true);
    if (this.hireForm.invalid) {
      return;
    }

    this.invalid.set(false);
    this.failed.set(false);
    this.hiredName.set(null);
    const details = this.hireForm.getRawValue();

    this.employeesGateway
      .hire({
        displayName: details.displayName,
        email: details.email,
        role: details.role as EmployeeRole,
        initialPassword: details.initialPassword,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.hiredName.set(details.displayName);
          this.hireForm.reset();
          this.attempted.set(false);
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 400) {
            this.invalid.set(true);
          } else {
            this.failed.set(true);
          }
        },
      });
  }
}
