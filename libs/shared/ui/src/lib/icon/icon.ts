import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type IconName =
  | 'reserve'
  | 'my-reservations'
  | 'occupancy'
  | 'calendar'
  | 'on-behalf'
  | 'offices'
  | 'admin'
  | 'sun'
  | 'moon';

// A decorative icon from the 24x24 set used across the shell, dashboard, and theme toggle. Purely
// presentational (`type:ui`): the wrapper attributes live here once and `name` selects the geometry,
// replacing the verbose <svg> that was duplicated in the sidebar nav and the dashboard cards. Most icons
// are stroked line icons; the filled sun/moon mark their shapes `fill="currentColor" stroke="none"` to
// opt out of the shared stroke wrapper. Icons are decorative (`aria-hidden`) — the accessible name comes
// from the adjacent link/card/button text. Size with the `--roomy-icon-size` custom property on any
// ancestor (it inherits across the component boundary).
@Component({
  selector: 'roomy-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './icon.html',
  styleUrl: './icon.css',
})
export class Icon {
  readonly name = input.required<IconName>();
}
