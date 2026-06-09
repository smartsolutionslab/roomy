import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import {
  reservationId,
  toBookableOffices,
  toMyReservation,
  toRoomAvailability,
} from './booking';
import type {
  BookableOffice,
  MyReservation,
  OfficeId,
  ReservationId,
  RoomAvailability,
  RoomId,
} from './booking';
import {
  ApiConfiguration,
  cancelReservation,
  reserve,
  viewBookableRooms,
  viewMyReservations,
  viewOccupancy,
} from './generated';
import { toOccupancyDays } from './occupancy';
import type { OccupancyDay } from './occupancy';

// Reads and mutates attendance through the gateway (`/rooms`, `/reservations**`, `/occupancy`) using the
// generated client (ADR-0036), mapping the trusted DTOs to branded view models at this boundary
// (ADR-0020). The generated client defaults to a relative root URL, so calls stay same-origin (ADR-0030)
// and the BFF forwards the token — the SPA never sees one (ADR-0013). Every endpoint is self-service;
// the acting employee is resolved server-side from the session (no onBehalfOf here — AT-6 is deferred).
@Injectable({ providedIn: 'root' })
export class AttendanceGateway {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfiguration);

  // The bookable catalogue (GET /rooms), grouped into offices for the picker's first step (AT-1).
  listBookableOffices(): Observable<BookableOffice[]> {
    return viewBookableRooms(this.http, this.config.rootUrl).pipe(
      map((response) => toBookableOffices(response.body)),
    );
  }

  // Each room's availability in an office for one day, to show remaining places and grey out a full room
  // before submitting (AT-3, FR-002).
  occupancyForOffice(office: OfficeId, day: string): Observable<RoomAvailability[]> {
    return viewOccupancy(this.http, this.config.rootUrl, { officeId: office, from: day, to: day }).pipe(
      map((response) => toRoomAvailability(response.body)),
    );
  }

  // Occupancy for an office or room over a date range (GET /occupancy), as full day figures — the office
  // rollup, per-room occupied/capacity, and (today/tomorrow only) occupants (008 OC-1/2/4/6). Exactly one
  // of officeId/roomId is set; the caller keeps the range within the backend's 31-day bound.
  occupancy(
    scope: { officeId?: OfficeId; roomId?: RoomId },
    from: string,
    to: string,
  ): Observable<OccupancyDay[]> {
    return viewOccupancy(this.http, this.config.rootUrl, {
      officeId: scope.officeId,
      roomId: scope.roomId,
      from,
      to,
    }).pipe(map((response) => toOccupancyDays(response.body)));
  }

  // Reserve a place (POST /reservations). Resolves to the new reservation id; rejections (room_full,
  // already_reserved_today, not_bookable, unknown_room, concurrency_retry_exhausted) surface as the
  // HttpErrorResponse the page maps to a localized message (FR-004).
  reserve(office: OfficeId, room: RoomId, date: string): Observable<ReservationId> {
    return reserve(this.http, this.config.rootUrl, {
      body: { officeId: office, roomId: room, date },
    }).pipe(map((response) => reservationId(response.body.reservationId)));
  }

  // The signed-in employee's own reservations, past and upcoming (GET /reservations/mine, AT-4).
  myReservations(): Observable<MyReservation[]> {
    return viewMyReservations(this.http, this.config.rootUrl).pipe(
      map((response) => response.body.map(toMyReservation)),
    );
  }

  // Cancel a reservation (DELETE /reservations/{id}?date=). The date locates the company-day stream; a
  // past day is rejected server-side as past_immutable (FR-007).
  cancel(reservation: ReservationId, date: string): Observable<void> {
    return cancelReservation(this.http, this.config.rootUrl, {
      reservationId: reservation,
      date,
    }).pipe(map(() => undefined));
  }
}
