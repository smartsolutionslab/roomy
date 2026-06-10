import { HttpErrorResponse } from '@angular/common/http';

import { errorCode } from './gateway-error';

describe('errorCode', () => {
  it('returns the code from the gateway error body', () => {
    const error = new HttpErrorResponse({ error: { code: 'past_immutable' } });

    expect(errorCode(error)).toBe('past_immutable');
  });

  it('is undefined when the body carries no code or is absent', () => {
    expect(errorCode(new HttpErrorResponse({ error: {} }))).toBeUndefined();
    expect(errorCode(new HttpErrorResponse({ error: null }))).toBeUndefined();
  });
});
