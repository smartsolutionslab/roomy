# Internal REST Contract: Occupancy (004)

Adds the **read** surface to the `attendance` service, reachable only through the YARP gateway/BFF
(ADR-0013/0018). All routes require an authenticated BFF session; the forwarded Keycloak token
identifies the acting user (`sub`). The acting user's `EmployeeId` is resolved server-side from the
`Employees` read model — clients never send their own employee id.

All dates are **Europe/Berlin** calendar dates (`yyyy-MM-dd`). These endpoints are **read-only**
(FR-006) and viewable by **any authenticated user** for **any** office or room (FR-005); there is no
owner or admin check on viewing.

## `GET /occupancy?officeId={guid}&from={yyyy-MM-dd}&to={yyyy-MM-dd}`
## `GET /occupancy?roomId={guid}&from={yyyy-MM-dd}&to={yyyy-MM-dd}`

Per-room occupancy and the office rollup for each day in a range (FR-001/002/003/007/008/009;
scenarios 1–4, 7–9). Supplies the data for both the **list** (US6) and the **calendar** (US7).

- **Auth:** any authenticated user (FR-005). No owner/admin check (FR-006).
- **Scope (exactly one required):**
  - `officeId` ⇒ the office and all its rooms, with the office rollup.
  - `roomId` ⇒ a single room (its office rollup reflects that one room).
- **Range:** `from`/`to` inclusive; `from == to` is a single day; `to` omitted ⇒ single day `from`;
  both omitted ⇒ **today** (Europe/Berlin). Past dates are allowed (FR-009). A bounded maximum span
  (e.g. one month) keeps a request bounded — **422 `range_too_large`** beyond it.
- **200:** one entry per day in `[from, to]`:

  ```json
  [
    {
      "date": "2026-06-09",
      "office": { "officeId": "…", "name": "Munich",
                  "occupied": 12, "capacity": 30, "isFull": false },
      "rooms": [
        { "roomId": "…", "name": "A1", "occupied": 3, "capacity": 8, "isFull": false,
          "occupants": [ { "employeeId": "…", "name": "Ada Lovelace" } ] },
        { "roomId": "…", "name": "A2", "occupied": 8, "capacity": 8, "isFull": true,
          "occupants": [ … ] }
      ]
    }
  ]
  ```

  - `occupied`/`capacity` are the figure (e.g. 3/8, rollup 12/30). `isFull` ⇒ `occupied == capacity`
    (FR-008, scenario 9).
  - **`occupants` is present only for today and the next calendar day** (FR-007, scenario 4); for every
    other day (past or further future) the field is **absent** (counts only). `name` is the
    `Employees` display name.
  - A room with no reservations returns `occupied: 0` and is still listed; the rollup includes it
    (edge case).
- **422 `unknown_scope`:** neither `officeId` nor `roomId` supplied, or both.
- **422 `range_too_large`:** the requested span exceeds the bound.
- **404 `unknown_office` / `unknown_room`:** the office/room is not (yet) known to attendance.

> The calendar's **own-day highlight** (FR-003, scenario 5) is computed by the client by intersecting
> these figures with `GET /reservations/mine` — there is no separate calendar endpoint (research R7).

## `GET /reservations/mine`

The caller's own reservations — past, today, and future (FR-004, scenario 6). Drives the "my
reservations" view (US9).

- **Auth:** any authenticated user; the employee is the token `sub`, resolved server-side.
- **200:** every reservation the caller holds, with office, room, and day:

  ```json
  [
    { "reservationId": "…", "officeId": "…", "officeName": "Munich",
      "roomId": "…", "roomName": "A1", "date": "2026-06-12" }
  ]
  ```

  - Includes past reservations as history (FR-004). The client renders **cancel** only for future
    reservations; cancellation itself is the existing `DELETE /reservations/{id}?date=…` (003), past
    days returning **422 `past_immutable`**. Empty array if the caller holds none.
- **404 `unknown_employee`:** the token subject has no `Employees` link yet (provisioning lag).

## Read-your-writes

All figures and lists reflect the **latest committed data at the moment of the request** (FR-010): the
read models are projected in the same transaction that records each reservation (ADR-0038), so a view
opened right after a booking or cancellation already reflects it. No live/push updates (out of scope).

## Error body

All non-2xx carry `{ code, message }` where `code` is the error code above, mapped from `Result`/`Error`
(`ErrorType` → status: Validation→422, NotFound→404). No domain detail leaks beyond `code` + a human
`message`.

## Gateway route

The YARP gateway forwards `/occupancy` and `/reservations/**` to the `attendance` cluster under the
authenticated BFF session (ADR-0013); add the `/occupancy` route alongside the existing
`/reservations` route. The OpenAPI spec emitted by `attendance-api` is the source for the generated
Angular client (ADR-0036) and is drift-gated in CI.
