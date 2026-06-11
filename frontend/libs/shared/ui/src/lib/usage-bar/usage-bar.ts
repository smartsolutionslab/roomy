import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

// A slim occupancy/usage meter: a horizontal bar filled to the occupied share of capacity. Decorative
// (aria-hidden) — an adjacent figure carries the numbers — and inherits currentColor so it adapts to its
// surface (e.g. the on-accent colour on a selected tile). Shared by the reserve tiles and the occupancy
// views.
@Component({
  selector: 'roomy-usage-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './usage-bar.html',
  styleUrl: './usage-bar.css',
})
export class UsageBar {
  readonly occupied = input.required<number>();
  readonly capacity = input.required<number>();

  protected readonly percent = computed(() => {
    const capacity = this.capacity();
    return capacity > 0 ? Math.round((this.occupied() / capacity) * 100) : 0;
  });
}
