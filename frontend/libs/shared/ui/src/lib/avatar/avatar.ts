import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

// A circular identity badge showing a person's initials over the brand gradient. Purely presentational
// (no image support yet — Roomy has no avatar uploads); the initials are derived from the display name.
// Decorative by default (`aria-hidden`): the control that wraps it (e.g. the account menu button) carries
// the accessible name, so screen readers are not read a meaningless "AL".
@Component({
  selector: 'roomy-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './avatar.html',
  styleUrl: './avatar.css',
})
export class Avatar {
  readonly name = input.required<string>();
  readonly size = input<'sm' | 'md' | 'lg'>('md');

  protected readonly initials = computed(() => deriveInitials(this.name()));
}

function deriveInitials(name: string): string {
  const parts = name
    .trim()
    .split(/\s+/)
    .filter((part) => part.length > 0);

  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0].charAt(0).toUpperCase();

  return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
}
