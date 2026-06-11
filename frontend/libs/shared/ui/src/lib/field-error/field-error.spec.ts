import { provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { FieldError } from './field-error';

function renderField(inputs: { show: boolean; message: string }) {
  return render(FieldError, {
    inputs,
    providers: [provideZonelessChangeDetection()],
  });
}

describe('FieldError', () => {
  it('renders nothing while the field is valid', async () => {
    await renderField({ show: false, message: 'A name is required.' });

    expect(screen.queryByText('A name is required.')).toBeNull();
  });

  it('shows the error message when the field is in error', async () => {
    await renderField({ show: true, message: 'A name is required.' });

    expect(screen.getByText('A name is required.')).toBeTruthy();
  });
});
