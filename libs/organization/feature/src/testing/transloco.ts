import { ModuleWithProviders } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';

// Test-only Transloco setup for this library's components. It carries just the keys the organization
// screens use, so the library's specs stay independent of the app's global translation files
// (importing those would cross the context:web boundary). Excluded from the library build.
const english = {
  organization: {
    offices: {
      title: 'Offices',
      loading: 'Loading offices…',
      empty: 'No offices yet.',
      loadError: 'We could not load the offices.',
      locationLabel: 'Location',
      capacityLabel: 'Capacity',
      roomsHeading: 'Rooms',
      noRooms: 'No rooms yet.',
    },
  },
};

const german = {
  organization: {
    offices: {
      title: 'Büros',
      loading: 'Büros werden geladen…',
      empty: 'Noch keine Büros.',
      loadError: 'Die Büros konnten nicht geladen werden.',
      locationLabel: 'Standort',
      capacityLabel: 'Kapazität',
      roomsHeading: 'Räume',
      noRooms: 'Noch keine Räume.',
    },
  },
};

export function importOrganizationTestTransloco(): ModuleWithProviders<TranslocoTestingModule> {
  return TranslocoTestingModule.forRoot({
    langs: { en: english, de: german },
    translocoConfig: {
      availableLangs: ['en', 'de'],
      defaultLang: 'en',
      reRenderOnLangChange: true,
    },
    preloadLangs: true,
  });
}
