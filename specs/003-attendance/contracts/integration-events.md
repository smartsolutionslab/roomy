# Integration-Event Contracts: Attendance

Cross-context contracts carried over Wolverine with the transactional outbox/inbox
(ADR-0005/0014/0015). Minimal, versioned, IDs/primitives only — never domain value objects
(ADR-0031). The attendance write model is **event-sourced**; the events below are how it
*learns* master data, not its own stream events (those stay internal — see `data-model.md`).

## Consumed

### `EmployeeHired` (from `organization`, **existing**)
Feeds the `Employees` read model so the acting user (`sub`) resolves to an `EmployeeId` for
authorization (research R3). Attendance maps it to an internal `LinkEmployee` command at the
edge.

| Field | Type | Notes |
|---|---|---|
| `employeeId` | GUID | organization-side employee identity |
| `userId` | GUID | the linked account (identity `sub`) |
| *(other fields ignored by attendance)* | | role/email/etc. are not needed here |

### `OfficeOpened` (from `organization`, **NEW — added in this slice**)
Feeds the office name onto the `Rooms` read model.

| Field | Type | Notes |
|---|---|---|
| `officeId` | GUID | |
| `companyId` | GUID | tenant (single company v1) |
| `name` | string | office display name |
| `location` | string | |
| `occurredAt` | timestamptz | |

### `RoomAdded` (from `organization`, **NEW — added in this slice**)
The capacity feed — the no-overbooking ceiling (FR-004/FR-007). Without it attendance cannot
enforce the invariant.

| Field | Type | Notes |
|---|---|---|
| `roomId` | GUID | |
| `officeId` | GUID | the office the room belongs to |
| `companyId` | GUID | |
| `name` | string | room display name |
| `capacity` | int | **places (≥ 1)** — the per-(room, day) ceiling |
| `occurredAt` | timestamptz | |

> **Producer-side note (organization / 002).** `OfficeOpened` and `RoomAdded` are organization's
> *published language* and must be **emitted by the organization context** when an office/room is
> created (`libs/organization/contracts` + a publish in organization's create handlers). 002's
> Office/Room domain is in PR **#113** (not yet on `main`) — see the dependency note in `plan.md`.
> Consumers reference only `libs/organization/contracts` (ADR-0031).

## Published

**None in this slice.** `ReservationPlaced` / `ReservationCancelled` are the attendance stream's
*internal* events (the event-sourced source of truth); the occupancy projection (`004`) folds
the same stream **locally inside the attendance service** — no cross-context publish. If a future
context needs reservation facts, they are promoted to a published contract then, not now.
