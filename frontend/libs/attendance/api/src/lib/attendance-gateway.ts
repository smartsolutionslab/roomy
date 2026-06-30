import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Page, mapPage } from '@roomy/shared-data-access';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { reservationId, toBookableOffices, toMyReservation, toRoomAvailability } from './booking';
import type {
  BookableOffice,
  EmployeeId,
  MyReservation,
  OfficeId,
  ReservationId,
  RoomAvailability,
  RoomId,
} from './booking';
import { toEmployee } from './employee';
import type { Employee } from './employee';
import {
  ApiConfiguration,
  cancelReservation,
  reserve,
  viewBookableRooms,
  viewEmployees,
  viewMyReservations,
  viewOccupancy,
  viewReservationsForEmployee,
} from './generated';
import { toOccupancyDays } from './occupancy';
import type { OccupancyDay } from './occupancy';

@Injectable({ providedIn: 'root' })
export class AttendanceGateway {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfiguration);

  listBookableOffices(): Observable<BookableOffice[]> {
    return viewBookableRooms(this.http, this.config.rootUrl).pipe(
      map((response) => toBookableOffices(response.body)),
    );
  }

  occupancyForOffice(office: OfficeId, day: string): Observable<RoomAvailability[]> {
    return viewOccupancy(this.http, this.config.rootUrl, {
      officeId: office,
      from: day,
      to: day,
    }).pipe(map((response) => toRoomAvailability(response.body)));
  }

  // Set exactly one of officeId / roomId.
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

  reserve(
    office: OfficeId,
    room: RoomId,
    date: string,
    onBehalfOf?: EmployeeId,
  ): Observable<ReservationId> {
    return reserve(this.http, this.config.rootUrl, {
      body: {
        officeId: office,
        roomId: room,
        date,
        onBehalfOf,
      },
    }).pipe(map((response) => reservationId(response.body.reservationId)));
  }

  myReservations(cursor?: string): Observable<Page<MyReservation>> {
    return viewMyReservations(this.http, this.config.rootUrl, { cursor }).pipe(
      map((response) => mapPage(response.body, toMyReservation)),
    );
  }

  // A blank query sends no `q`, returning the full directory rather than a similarity ranking.
  listEmployees(query = '', cursor?: string): Observable<Page<Employee>> {
    const trimmed = query.trim();
    return viewEmployees(this.http, this.config.rootUrl, {
      q: trimmed || undefined,
      cursor,
    }).pipe(map((response) => mapPage(response.body, toEmployee)));
  }

  reservationsFor(employee: EmployeeId, cursor?: string): Observable<Page<MyReservation>> {
    return viewReservationsForEmployee(this.http, this.config.rootUrl, {
      employeeId: employee,
      cursor,
    }).pipe(map((response) => mapPage(response.body, toMyReservation)));
  }

  // The date locates the company-day event stream the reservation lives in.
  cancel(reservation: ReservationId, date: string): Observable<void> {
    return cancelReservation(this.http, this.config.rootUrl, {
      reservationId: reservation,
      date,
    }).pipe(map(() => undefined));
  }
}
