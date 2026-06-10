import { ChangeDetectionStrategy, Component } from '@angular/core';

// The authenticated application shell: a fixed brand+navigation sidebar beside a content column made of
// a sticky top bar, the scrolling main region, and a footer. It is pure structure — every region is a
// projection slot, so the app supplies the brand, the navigation items, the top-bar controls (where the
// account avatar lives), the footer, and the routed main content. Marker attributes select each slot:
//   [roomy-brand]  · [roomy-nav]  · [roomy-top]  · [roomy-footer]  · (default) → main content
@Component({
  selector: 'roomy-app-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './layout.html',
  styleUrl: './layout.css',
})
export class AppLayout {}
