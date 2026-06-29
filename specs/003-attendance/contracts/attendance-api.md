# Internal REST Contract: Attendance

Internal API of the `attendance` service, reachable only through the YARP gateway/BFF
(ADR-0013/0018). All routes require an authenticated BFF session; the forwarded Keycloak token
identifies the acting user (`sub`) and carries the `administrator` realm role. The acting user's
`EmployeeId` is resolved server-side from the `Employees` read model (research R3) — clients
never send their own employee id for self-actions.

All dates are **Europe/Berlin** calendar dates (`yyyy-MM-dd`).

## `POST /reservations`
Reserve a place in a room for a day (FR-001, scenarios 1–2, 10).

- **Auth:** any authenticated employee (admins are employees too).
- **Body:** `{ officeId, roomId, date, onBehalfOf? }`.
  - `onBehalfOf` (GUID employeeId) is **administrator-only** (FR-011); omitted ⇒ the action is
    for the caller. A non-admin supplying `onBehalfOf` for anyone but themselves ⇒ **403**.
- **201:** `{ reservationId, officeId, roomId, date, employeeId }` — place guaranteed (FR-003).
- **409 `room_full`:** room at capacity for that day (FR-004/FR-007, scenarios 3, 12).
- **409 `already_reserved_today`:** the employee already holds a reservation that day
  (FR-005, scenario 4).
- **409 `concurrency_retry_exhausted`:** lost the optimistic-concurrency race after retries
  (FR-007, scenario 12 fallback) — safe to retry.
- **422 `not_bookable`:** past, weekend, or beyond the 14-day window (FR-002/FR-006,
  scenarios 5–7).
- **404 `unknown_room`:** the room is not (yet) known to attendance.
- **403:** non-admin acting on another employee.

## `DELETE /reservations/{reservationId}?date={yyyy-MM-dd}`
Cancel a reservation, freeing the place (FR-008, scenarios 8–9).

- **`date` (required):** the reservation's day, in the Europe/Berlin calendar. The event-sourced
  store is keyed by company-day (the `AttendanceDay` stream id derives from `CompanyId + Date`,
  ADR-0026), so the reservation id alone cannot address the stream; the client always has the date
  (the list/view shows it). A single-tenant decision — revisit if a reservation→day index is later
  warranted (e.g. occupancy `004`).
- **Auth:** the **owner** or an **administrator** (FR-012, scenario 11).
- **204:** cancelled; the place is freed (raises `ReservationCancelled`).
- **403 `not_authorized`:** an employee cancelling another's reservation.
- **404 `reservation_not_found`:** no such reservation for that company-day.
- **422 `past_immutable`:** the reservation's day is in the past (FR-009).

## `GET /reservations?date={yyyy-MM-dd}`
View the reservations for a company-day (scenario 11 — view-only for everyone).

- **Auth:** any authenticated employee.
- **200:** `[ { reservationId, officeId, roomId, date, employeeId } ]` — replayed from the
  `AttendanceDay` stream (research R6). Empty array for a day with no reservations.

> The occupancy rollup (per-room remaining places, office totals) and the "my reservations"
> overview are **out of scope here** — they are `004-occupancy`.

## Error body

All non-2xx carry `{ code, message }` where `code` is the domain error code in the table above,
mapped from `Result`/`Error` (`ErrorType` → status: Validation→422, Conflict→409,
Forbidden→403, NotFound→404). No domain detail leaks beyond `code` + a human `message`.

## Gateway route

Add an `/attendance/{**}` (or `/reservations/{**}`) route to `backend/apps/gateway/appsettings.json`:
cluster `attendance`, `AuthorizationPolicy: default` — mirroring the `identity-account` route.
