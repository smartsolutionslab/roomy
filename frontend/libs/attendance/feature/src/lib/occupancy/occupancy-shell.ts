import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { BookableOffice } from '@roomy/attendance-api';
import { Message, Page } from '@roomy/shared-ui';

import { OccupancyScope, OfficeRoomPicker } from './office-room-picker';

// The chrome shared by the occupancy list and calendar: the page heading, the load/empty/loading states,
// and the office/room picker. Each page projects its own scope-specific body and reacts to scopeChange;
// `errorKey` is an optional banner for a page-level error (the list uses it for a stale scope).
@Component({
  selector: 'roomy-occupancy-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, Page, Message, OfficeRoomPicker],
  templateUrl: './occupancy-shell.html',
})
export class OccupancyShell {
  readonly titleKey = input.required<string>();
  readonly offices = input<BookableOffice[] | null>(null);
  readonly loadFailed = input(false);
  readonly errorKey = input<string | null>(null);
  readonly scopeChange = output<OccupancyScope | null>();
}
