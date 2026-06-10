import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { SessionService } from '@roomy/shared-data-access';

@Component({
  selector: 'roomy-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  private readonly session = inject(SessionService);

  protected readonly currentUser = this.session.currentUser;
}
