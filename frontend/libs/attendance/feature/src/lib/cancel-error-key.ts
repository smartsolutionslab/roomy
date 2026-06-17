import { HttpErrorResponse } from '@angular/common/http';
import { errorCode } from '@roomy/attendance-api';

// Maps a failed cancel into the page's localised message key. The two cancel surfaces (mine, on-behalf)
// share the rule and differ only by their Transloco namespace.
export function cancelErrorKey(error: HttpErrorResponse, namespace: string): string {
  return errorCode(error) === 'past_immutable'
    ? `${namespace}.errors.pastImmutable`
    : `${namespace}.errors.generic`;
}
