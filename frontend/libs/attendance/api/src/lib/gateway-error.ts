import { HttpErrorResponse } from '@angular/common/http';

// The machine-readable code from a Roomy gateway JSON error body, or undefined for a network / non-Roomy error.
export function errorCode(error: HttpErrorResponse): string | undefined {
  return (error.error as { code?: string } | null)?.code;
}
