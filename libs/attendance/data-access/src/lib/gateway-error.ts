import { HttpErrorResponse } from '@angular/common/http';

// The machine-readable `code` a Roomy gateway returns in its JSON error body (ADR-0046), or undefined
// when the response carries none (a network failure or a non-Roomy error). Callers map the code to a
// localized message — the parsing of the envelope lives here, once.
export function errorCode(error: HttpErrorResponse): string | undefined {
  return (error.error as { code?: string } | null)?.code;
}
