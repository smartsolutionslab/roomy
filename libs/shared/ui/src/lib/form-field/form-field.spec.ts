import { Component, provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';

import { FormField } from './form-field';

@Component({
  imports: [FormField],
  template: `<roomy-form-field label="Email"><input type="email" /></roomy-form-field>`,
})
class HostComponent {}

describe('FormField', () => {
  it('labels the projected control (clicking the label focuses it)', async () => {
    await render(HostComponent, { providers: [provideZonelessChangeDetection()] });

    // Implicit label association: the control is reachable by its label text.
    expect(screen.getByLabelText('Email')).toBeTruthy();
  });
});
