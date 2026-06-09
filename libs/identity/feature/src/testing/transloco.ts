import { ModuleWithProviders } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';

// Test-only Transloco setup for this library's components. It carries just the keys the identity
// screens use, so the library's specs stay independent of the app's global translation files
// (importing those would cross the context:web boundary). Excluded from the library build.
const english = {
  account: {
    title: 'My account',
    nameLabel: 'Name',
    emailLabel: 'Email',
    roleLabel: 'Role',
    loading: 'Loading your account…',
    error: 'We could not load your account.',
    role: {
      employee: 'Employee',
      administrator: 'Administrator',
    },
  },
  admin: {
    users: {
      title: 'Accounts',
      loading: 'Loading accounts…',
      empty: 'No accounts yet.',
      loadError: 'We could not load the accounts.',
      grantError: 'We could not grant administrator. Please try again.',
      grantSuccess: 'Granted administrator to {{name}}.',
      grant: 'Grant administrator',
      confirm: 'Confirm',
      cancel: 'Cancel',
      nameHeader: 'Name',
      emailHeader: 'Email',
      roleHeader: 'Role',
      statusHeader: 'Status',
      actionsHeader: 'Actions',
      status: {
        provisioning: 'Provisioning',
        active: 'Active',
      },
    },
    notAuthorized: {
      title: 'Not authorized',
      message: 'You do not have permission to view this page.',
    },
  },
};

const german = {
  account: {
    title: 'Mein Konto',
    nameLabel: 'Name',
    emailLabel: 'E-Mail',
    roleLabel: 'Rolle',
    loading: 'Konto wird geladen…',
    error: 'Konto konnte nicht geladen werden.',
    role: {
      employee: 'Mitarbeiter',
      administrator: 'Administrator',
    },
  },
  admin: {
    users: {
      title: 'Konten',
      loading: 'Konten werden geladen…',
      empty: 'Noch keine Konten.',
      loadError: 'Die Konten konnten nicht geladen werden.',
      grantError: 'Administratorrechte konnten nicht vergeben werden. Bitte erneut versuchen.',
      grantSuccess: 'Administratorrolle an {{name}} vergeben.',
      grant: 'Administrator vergeben',
      confirm: 'Bestätigen',
      cancel: 'Abbrechen',
      nameHeader: 'Name',
      emailHeader: 'E-Mail',
      roleHeader: 'Rolle',
      statusHeader: 'Status',
      actionsHeader: 'Aktionen',
      status: {
        provisioning: 'Wird bereitgestellt',
        active: 'Aktiv',
      },
    },
    notAuthorized: {
      title: 'Nicht berechtigt',
      message: 'Sie haben keine Berechtigung, diese Seite anzuzeigen.',
    },
  },
};

export function importIdentityTestTransloco(): ModuleWithProviders<TranslocoTestingModule> {
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
