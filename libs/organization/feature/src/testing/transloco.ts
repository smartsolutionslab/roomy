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
    create: {
      heading: 'Add an office',
      nameLabel: 'Name',
      locationLabel: 'Location',
      submit: 'Create office',
      nameConflict: 'An office with that name already exists.',
      error: 'We could not create the office. Please try again.',
      created: 'Office {{name}} created.',
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
    create: {
      heading: 'Büro hinzufügen',
      nameLabel: 'Name',
      locationLabel: 'Standort',
      submit: 'Büro erstellen',
      nameConflict: 'Ein Büro mit diesem Namen existiert bereits.',
      error: 'Das Büro konnte nicht erstellt werden. Bitte erneut versuchen.',
      created: 'Büro {{name}} erstellt.',
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
