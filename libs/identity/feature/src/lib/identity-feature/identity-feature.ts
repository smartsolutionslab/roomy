import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'roomy-identity-feature',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  templateUrl: './identity-feature.html',
  styleUrl: './identity-feature.css',
})
export class IdentityFeature {}
