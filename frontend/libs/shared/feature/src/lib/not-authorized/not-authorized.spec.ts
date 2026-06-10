import { provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { importSharedTestTransloco } from '../../testing/transloco';

import { NotAuthorized } from './not-authorized';

describe('NotAuthorized', () => {
  it('explains that the page is not permitted', async () => {
    await render(NotAuthorized, {
      imports: [importSharedTestTransloco()],
      providers: [provideZonelessChangeDetection()],
    });

    expect(await screen.findByText('You do not have permission to view this page.')).toBeTruthy();
  });
});
