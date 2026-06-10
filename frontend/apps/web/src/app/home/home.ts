import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { SessionService } from '@roomy/shared-data-access';
import { NavigationService } from '@roomy/shared-feature';
import { Card, Icon, RoomyLogo } from '@roomy/shared-ui';

@Component({
  selector: 'roomy-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, RouterLink, Card, Icon, RoomyLogo],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  private readonly session = inject(SessionService);
  private readonly navigation = inject(NavigationService);

  protected readonly currentUser = this.session.currentUser;
  protected readonly navItems = this.navigation.items;
}
