import { ModuleWithProviders } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';

// Test-only Transloco setup for this library's components. It carries just the keys the attendance
// screens use, so the library's specs stay independent of the app's global translation files (importing
// those would cross the context:web boundary). Excluded from the library build. Kept in key parity with
// apps/web/public/i18n/{en,de}.json (FR-010).
const english = {
  attendance: {
    reserve: {
      title: 'Reserve a place',
      loading: 'Loading…',
      loadError: 'We could not load the offices.',
      empty: 'No offices or rooms are available to book yet.',
      officeLabel: 'Office',
      officePlaceholder: 'Select an office',
      dayLabel: 'Day',
      dayPlaceholder: 'Select a day',
      roomsHeading: 'Rooms',
      capacityLabel: '{{capacity}} places',
      placesLeft: '{{remaining}} of {{capacity}} places left',
      full: 'Full',
      confirm: 'Reserve',
      reserved: 'Reserved {{room}} for {{date}}.',
      errors: {
        roomFull: 'That room is full for the chosen day.',
        alreadyReserved: 'You already have a reservation that day.',
        notBookable: 'Only working days within the next two weeks can be booked.',
        unknownRoom: 'That room is no longer available.',
        retry: 'Someone just took the last place. Please try again.',
        generic: 'We could not reserve the place. Please try again.',
      },
    },
    mine: {
      title: 'My reservations',
      loading: 'Loading…',
      loadError: 'We could not load your reservations.',
      empty: 'You have no reservations yet.',
      reserveLink: 'Reserve a place',
      upcomingHeading: 'Upcoming',
      pastHeading: 'Past',
      cancel: 'Cancel',
      cancelReservation: 'Cancel the reservation for {{room}} on {{date}}',
      change: 'Change',
      cancelled: 'Reservation cancelled.',
      errors: {
        pastImmutable: 'Past reservations cannot be changed.',
        generic: 'We could not cancel the reservation. Please try again.',
      },
    },
  },
};

const german = {
  attendance: {
    reserve: {
      title: 'Platz reservieren',
      loading: 'Wird geladen…',
      loadError: 'Die Büros konnten nicht geladen werden.',
      empty: 'Es sind noch keine Büros oder Räume buchbar.',
      officeLabel: 'Büro',
      officePlaceholder: 'Büro auswählen',
      dayLabel: 'Tag',
      dayPlaceholder: 'Tag auswählen',
      roomsHeading: 'Räume',
      capacityLabel: '{{capacity}} Plätze',
      placesLeft: '{{remaining}} von {{capacity}} Plätzen frei',
      full: 'Belegt',
      confirm: 'Reservieren',
      reserved: '{{room}} für {{date}} reserviert.',
      errors: {
        roomFull: 'Dieser Raum ist am gewählten Tag belegt.',
        alreadyReserved: 'Sie haben an diesem Tag bereits eine Reservierung.',
        notBookable: 'Nur Werktage innerhalb der nächsten zwei Wochen sind buchbar.',
        unknownRoom: 'Dieser Raum ist nicht mehr verfügbar.',
        retry: 'Jemand hat gerade den letzten Platz belegt. Bitte erneut versuchen.',
        generic: 'Der Platz konnte nicht reserviert werden. Bitte erneut versuchen.',
      },
    },
    mine: {
      title: 'Meine Reservierungen',
      loading: 'Wird geladen…',
      loadError: 'Ihre Reservierungen konnten nicht geladen werden.',
      empty: 'Sie haben noch keine Reservierungen.',
      reserveLink: 'Platz reservieren',
      upcomingHeading: 'Bevorstehend',
      pastHeading: 'Vergangen',
      cancel: 'Stornieren',
      cancelReservation: 'Reservierung für {{room}} am {{date}} stornieren',
      change: 'Ändern',
      cancelled: 'Reservierung storniert.',
      errors: {
        pastImmutable: 'Vergangene Reservierungen können nicht geändert werden.',
        generic: 'Die Reservierung konnte nicht storniert werden. Bitte erneut versuchen.',
      },
    },
  },
};

export function importAttendanceTestTransloco(): ModuleWithProviders<TranslocoTestingModule> {
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
