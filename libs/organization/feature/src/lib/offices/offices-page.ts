import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { Office, OfficesGateway } from '@roomy/organization-data-access';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'roomy-offices-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective],
  templateUrl: './offices-page.html',
  styleUrl: './offices-page.css',
})
export class OfficesPage {
  private readonly officesGateway = inject(OfficesGateway);

  protected readonly offices = signal<Office[] | null>(null);
  protected readonly loadFailed = signal(false);

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
}
