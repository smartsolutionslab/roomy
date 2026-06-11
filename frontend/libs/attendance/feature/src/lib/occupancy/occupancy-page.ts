import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import {
  AttendanceGateway,
  OccupancyDay,
  RangePreset,
  errorCode,
  rangeFor,
  todayInBerlin,
} from '@roomy/attendance-api';
import { FormField, UsageBar } from '@roomy/shared-ui';

import { bookableOfficesCatalogue } from '../bookable-offices-catalogue';

import { OccupancyShell } from './occupancy-shell';
import { OccupancyScope } from './office-room-picker';

// The occupancy list: pick an office (optionally a single room) and a day / week / month range, and
// read each day's office rollup + per-room figures. Occupant names render only when the response
// carries them (today/tomorrow); the view is read-only, so a past range simply shows history. `today`
// is an input defaulting to the Europe/Berlin day, so the presets are deterministic under test. The
// office/room selection is owned by roomy-office-room-picker (shared with the calendar); this page
// reacts only to the emitted scope.
@Component({
  selector: 'roomy-occupancy-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, FormField, OccupancyShell, UsageBar],
  templateUrl: './occupancy-page.html',
  styleUrl: './occupancy-page.css',
})
export class OccupancyPage {
  private readonly gateway = inject(AttendanceGateway);
  private readonly destroyRef = inject(DestroyRef);

  readonly today = input<string>(todayInBerlin());

  private readonly catalogue = bookableOfficesCatalogue();
  protected readonly offices = this.catalogue.offices;
  protected readonly loadFailed = this.catalogue.loadFailed;

  protected readonly scope = signal<OccupancyScope | null>(null);
  protected readonly preset = signal<RangePreset>('day');
  // null until the viewer picks a date; the effective anchor falls back to `today()`, read lazily so the
  // signal input is bound by the time it is evaluated (a constructor read would see the default).
  protected readonly anchor = signal<string | null>(null);
  protected readonly days = signal<OccupancyDay[] | null>(null);
  protected readonly errorKey = signal<string | null>(null);

  protected readonly presets: readonly RangePreset[] = ['day', 'week', 'month'];
  protected readonly effectiveAnchor = computed(() => this.anchor() ?? this.today());

  protected onScope(scope: OccupancyScope | null): void {
    this.scope.set(scope);
    this.load();
  }

  protected choosePreset(preset: string): void {
    this.preset.set(preset as RangePreset);
    this.load();
  }

  protected changeAnchor(date: string): void {
    if (date) {
      this.anchor.set(date);
      this.load();
    }
  }

  private load(): void {
    const scope = this.scope();
    if (scope === null) {
      this.days.set(null);
      return;
    }

    this.errorKey.set(null);
    const { from, to } = rangeFor(this.preset(), this.effectiveAnchor());

    this.gateway
      .occupancy(scope, from, to)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (days) => this.days.set(days),
        error: (error: HttpErrorResponse) => {
          const code = errorCode(error);
          if (code === 'unknown_office' || code === 'unknown_room') {
            this.errorKey.set('attendance.occupancy.unknownScope');
            this.catalogue.reload();
          } else {
            this.errorKey.set('attendance.occupancy.loadError');
          }
          this.days.set(null);
        },
      });
  }
}
