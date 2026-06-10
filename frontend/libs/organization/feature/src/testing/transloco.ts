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
    edit: {
      rename: 'Rename',
      relocate: 'Change location',
      nameLabel: 'New name',
      locationLabel: 'New location',
      save: 'Save',
      cancel: 'Cancel',
      nameConflict: 'An office with that name already exists.',
      notFound: 'That office no longer exists.',
      error: 'We could not save the change. Please try again.',
      updated: 'Office {{name}} updated.',
    },
    rooms: {
      add: 'Add room',
      submit: 'Add',
      nameLabel: 'Room name',
      capacityLabel: 'Capacity (places)',
      renameLabel: 'New room name',
      renameRoom: 'Rename room {{name}}',
      nameRequired: 'A room name is required.',
      capacityMin: 'Capacity must be at least 1.',
      nameConflict: 'A room with that name already exists in this office.',
      error: 'We could not save the room. Please try again.',
      added: 'Room {{name}} added.',
      renamed: 'Room {{name}} renamed.',
    },
    hire: {
      title: 'Hire a colleague',
      intro:
        'Record a colleague and provision their login. They can sign in once provisioning completes.',
      nameLabel: 'Display name',
      emailLabel: 'Work email',
      roleLabel: 'Role',
      rolePlaceholder: 'Select a role',
      roleEmployee: 'Employee',
      roleAdministrator: 'Administrator',
      passwordLabel: 'Initial password',
      submit: 'Hire',
      nameRequired: 'A display name is required.',
      emailInvalid: 'A valid work email is required.',
      roleRequired: 'Please choose a role.',
      passwordRequired: 'An initial password is required.',
      started:
        '{{name}} was hired. Their login is being provisioned — they can sign in once it is ready.',
      invalid: 'Please check the details and try again.',
      error: 'We could not complete the hire. Please try again.',
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
    edit: {
      rename: 'Umbenennen',
      relocate: 'Standort ändern',
      nameLabel: 'Neuer Name',
      locationLabel: 'Neuer Standort',
      save: 'Speichern',
      cancel: 'Abbrechen',
      nameConflict: 'Ein Büro mit diesem Namen existiert bereits.',
      notFound: 'Dieses Büro existiert nicht mehr.',
      error: 'Die Änderung konnte nicht gespeichert werden. Bitte erneut versuchen.',
      updated: 'Büro {{name}} aktualisiert.',
    },
    rooms: {
      add: 'Raum hinzufügen',
      submit: 'Hinzufügen',
      nameLabel: 'Raumname',
      capacityLabel: 'Kapazität (Plätze)',
      renameLabel: 'Neuer Raumname',
      renameRoom: 'Raum {{name}} umbenennen',
      nameRequired: 'Ein Raumname ist erforderlich.',
      capacityMin: 'Die Kapazität muss mindestens 1 betragen.',
      nameConflict: 'Ein Raum mit diesem Namen existiert in diesem Büro bereits.',
      error: 'Der Raum konnte nicht gespeichert werden. Bitte erneut versuchen.',
      added: 'Raum {{name}} hinzugefügt.',
      renamed: 'Raum {{name}} umbenannt.',
    },
    hire: {
      title: 'Kollegen einstellen',
      intro:
        'Erfassen Sie einen Kollegen und stellen Sie dessen Zugang bereit. Die Anmeldung ist möglich, sobald die Bereitstellung abgeschlossen ist.',
      nameLabel: 'Anzeigename',
      emailLabel: 'Arbeits-E-Mail',
      roleLabel: 'Rolle',
      rolePlaceholder: 'Rolle auswählen',
      roleEmployee: 'Mitarbeiter',
      roleAdministrator: 'Administrator',
      passwordLabel: 'Initiales Passwort',
      submit: 'Einstellen',
      nameRequired: 'Ein Anzeigename ist erforderlich.',
      emailInvalid: 'Eine gültige Arbeits-E-Mail ist erforderlich.',
      roleRequired: 'Bitte wählen Sie eine Rolle.',
      passwordRequired: 'Ein initiales Passwort ist erforderlich.',
      started:
        '{{name}} wurde eingestellt. Der Zugang wird bereitgestellt — die Anmeldung ist möglich, sobald er bereit ist.',
      invalid: 'Bitte überprüfen Sie die Angaben und versuchen Sie es erneut.',
      error: 'Die Einstellung konnte nicht abgeschlossen werden. Bitte erneut versuchen.',
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
