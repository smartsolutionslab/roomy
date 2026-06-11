# Contracts — Office & Room Management

This slice exposes **one** contract surface: the internal HTTP API in `organization-api.md`.

## Integration events: intentionally none (this slice)

No integration events are published by office/room management in the MVP slice. See
`research.md` D4: the future consumer of office/room capacity is the **attendance** context's
occupancy projection, which does not exist yet. When it does, `OfficeOpened` and `RoomAdded` will be
added to `backend/libs/organization/contracts` (the organization context's *published language*, ADR-0031)
and emitted over the transactional outbox in a dedicated slice.

The `EmployeeHired`/`HiredRole` records already in `backend/libs/organization/contracts` belong to the
separate **employee-provisioning saga** (ADR-0025), not to this office/room slice, and are **not**
touched here.
