import { DestroyRef, Signal, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AttendanceGateway, BookableOffice } from '@roomy/attendance-api';

// The bookable-offices catalogue shared by the reserve and occupancy pages: loads the catalogue once
// and exposes it with a load-failed flag, so each page declares only how it uses the offices, not how
// they are fetched. `reload()` re-fetches after a stale-scope error invalidates the catalogue.
export interface BookableOfficesCatalogue {
  // Loaded offices; null until the first load resolves, so views can show a loading placeholder. An
  // empty array once a load fails (the catalogue is "known empty", not "still loading").
  readonly offices: Signal<BookableOffice[] | null>;
  readonly loadFailed: Signal<boolean>;
  reload(): void;
}

export function bookableOfficesCatalogue(): BookableOfficesCatalogue {
  const gateway = inject(AttendanceGateway);
  const destroyRef = inject(DestroyRef);

  const offices = signal<BookableOffice[] | null>(null);
  const loadFailed = signal(false);

  function reload(): void {
    loadFailed.set(false);
    gateway
      .listBookableOffices()
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe({
        next: (loaded) => offices.set(loaded),
        error: () => {
          loadFailed.set(true);
          offices.set([]);
        },
      });
  }

  reload();
  return { offices, loadFailed, reload };
}
