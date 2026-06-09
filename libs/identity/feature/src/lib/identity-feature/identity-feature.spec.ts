import { provideZonelessChangeDetection } from '@angular/core';
import { render } from '@testing-library/angular';

import { IdentityFeature } from './identity-feature';

describe('IdentityFeature', () => {
  it('renders', async () => {
    const { container } = await render(IdentityFeature, {
      providers: [provideZonelessChangeDetection()],
    });

    expect(container).toBeTruthy();
  });
});
