import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import {
  ApiConfiguration,
  addRoom,
  changeOfficeLocation,
  createOffice,
  listOffices,
  renameOffice,
  renameRoom,
} from './generated';
import { toOffice, toRoom } from './office';
import type { Office, OfficeId, Room, RoomId } from './office';

@Injectable({ providedIn: 'root' })
export class OfficesGateway {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfiguration);

  listOffices(): Observable<Office[]> {
    return listOffices(this.http, this.config.rootUrl).pipe(
      map((response) => response.body.map(toOffice)),
    );
  }

  createOffice(name: string, location: string): Observable<Office> {
    return createOffice(this.http, this.config.rootUrl, { body: { name, location } }).pipe(
      map((response) => toOffice(response.body)),
    );
  }

  renameOffice(office: OfficeId, name: string): Observable<Office> {
    return renameOffice(this.http, this.config.rootUrl, { officeId: office, body: { name } }).pipe(
      map((response) => toOffice(response.body)),
    );
  }

  relocateOffice(office: OfficeId, location: string): Observable<Office> {
    return changeOfficeLocation(this.http, this.config.rootUrl, {
      officeId: office,
      body: { location },
    }).pipe(map((response) => toOffice(response.body)));
  }

  addRoom(office: OfficeId, name: string, capacity: number): Observable<Room> {
    return addRoom(this.http, this.config.rootUrl, {
      officeId: office,
      body: { name, capacity },
    }).pipe(map((response) => toRoom(response.body)));
  }

  renameRoom(office: OfficeId, room: RoomId, name: string): Observable<Office> {
    return renameRoom(this.http, this.config.rootUrl, {
      officeId: office,
      roomId: room,
      body: { name },
    }).pipe(map((response) => toOffice(response.body)));
  }
}
