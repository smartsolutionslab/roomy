import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let documentRef: Document;

  function inject(): ThemeService {
    const service = TestBed.inject(ThemeService);
    documentRef = TestBed.inject(DOCUMENT);
    return service;
  }

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('defaults to light and reflects it onto the document when nothing is stored', () => {
    const theme = inject();

    expect(theme.theme()).toBe('light');
    expect(documentRef.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('toggles to dark, updates the document attribute, and persists the choice', () => {
    const theme = inject();

    theme.toggle();

    expect(theme.theme()).toBe('dark');
    expect(documentRef.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem('roomy.theme')).toBe('dark');
  });

  it('restores a previously stored preference on initialization', () => {
    localStorage.setItem('roomy.theme', 'dark');

    const theme = inject();

    expect(theme.theme()).toBe('dark');
    expect(documentRef.documentElement.getAttribute('data-theme')).toBe('dark');
  });
});
