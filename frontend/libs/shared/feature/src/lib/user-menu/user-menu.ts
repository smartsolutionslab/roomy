import { ChangeDetectionStrategy, Component, ElementRef, inject, signal } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { SessionService } from '@roomy/shared-data-access';
import { Avatar } from '@roomy/shared-ui';

// The signed-in user's account control: an avatar button that toggles a dropdown with the user's name
// and a sign-out action. The dropdown closes on outside click or Escape (WCAG 2.2 AA keyboard support);
// the trigger exposes `aria-haspopup`/`aria-expanded`. Reads the user from the session; renders nothing
// when signed out, so it is safe to place unconditionally in the shell.
@Component({
  selector: 'roomy-user-menu',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, Avatar],
  templateUrl: './user-menu.html',
  styleUrl: './user-menu.css',
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'close()',
  },
})
export class UserMenu {
  private readonly session = inject(SessionService);
  private readonly host = inject(ElementRef);

  protected readonly currentUser = this.session.currentUser;
  protected readonly isAdmin = this.session.isAdministrator;
  protected readonly open = signal(false);

  protected toggle(): void {
    this.open.update((isOpen) => !isOpen);
  }

  protected close(): void {
    this.open.set(false);
  }

  protected onDocumentClick(event: MouseEvent): void {
    const element = this.host.nativeElement as HTMLElement;
    if (this.open() && !element.contains(event.target as Node)) {
      this.open.set(false);
    }
  }
}
