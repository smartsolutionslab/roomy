import { Component, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { Message } from './message';

@Component({
  imports: [Message],
  template: `<roomy-message variant="error">Something went wrong</roomy-message>
    <roomy-message>Saved</roomy-message>`,
})
class HostComponent {}

describe('Message', () => {
  it('announces errors assertively and status politely', async () => {
    await render(HostComponent, { providers: [provideZonelessChangeDetection()] });

    const error = screen.getByRole('alert');
    expect(error.textContent).toContain('Something went wrong');
    expect(error.classList.contains('message--error')).toBe(true);

    const status = screen.getByRole('status');
    expect(status.textContent).toContain('Saved');
    expect(status.getAttribute('aria-live')).toBe('polite');
  });
});
