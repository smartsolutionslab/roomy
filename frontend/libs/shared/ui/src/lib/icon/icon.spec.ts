import { provideZonelessChangeDetection } from '@angular/core';
import { render } from '@testing-library/angular';

import { Icon, IconName } from './icon';

const SVG_NAMESPACE = 'http://www.w3.org/2000/svg';

async function renderIcon(name: IconName) {
  const { container } = await render(Icon, {
    inputs: { name },
    providers: [provideZonelessChangeDetection()],
  });
  return container;
}

describe('Icon', () => {
  it('renders a decorative 24x24 stroke icon', async () => {
    const container = await renderIcon('reserve');

    const svg = container.querySelector('svg');
    expect(svg?.getAttribute('viewBox')).toBe('0 0 24 24');
    expect(svg?.getAttribute('aria-hidden')).toBe('true');
  });

  it('creates the inner shapes in the SVG namespace so they actually render', async () => {
    const container = await renderIcon('reserve');

    expect(container.querySelector('rect')?.namespaceURI).toBe(SVG_NAMESPACE);
    expect(container.querySelector('path')?.namespaceURI).toBe(SVG_NAMESPACE);
  });

  it('draws the calendar-based reserve icon with a rect', async () => {
    const reserve = await renderIcon('reserve');

    expect(reserve.querySelector('rect')).toBeTruthy();
  });

  it('draws a different geometry for another name', async () => {
    const admin = await renderIcon('admin');

    // admin is a shield outline — paths only, no rect — proving name() selects the geometry.
    expect(admin.querySelector('rect')).toBeNull();
    expect(admin.querySelector('path')).toBeTruthy();
  });

  it('renders the filled theme icons opting out of the stroke wrapper', async () => {
    const moon = await renderIcon('moon');

    const path = moon.querySelector('path');
    expect(path?.getAttribute('fill')).toBe('currentColor');
    expect(path?.getAttribute('stroke')).toBe('none');
  });
});
