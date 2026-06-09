import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

@Component({
  selector: 'roomy-not-authorized',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective],
  templateUrl: './not-authorized.html',
  styleUrl: './not-authorized.css',
})
export class NotAuthorized {}
