# Research: Office & Room Management

Phase 0 decisions and rationale for the organization context's first slice.

## D1 — Room lives *inside* the `Office` aggregate (not its own aggregate)

**Decision.** `Room` is an entity contained by the `Office` aggregate root; there is no
`Room` aggregate or `IRoomRepository`. Rooms are reached and mutated only through the `Office`.

**Why.** Two spec invariants are office-scoped: room names are unique *within their office*
(FR-010) and an office's capacity is the *sum of its rooms* (FR-008). Both require the office and
its rooms to change under one consistency boundary, which is the definition of an aggregate. A
separate `Room` aggregate would push these invariants into application-level coordination across two
repositories — more code, weaker guarantees. Aggregates are small here (a handful of rooms), so
loading the office with its rooms is cheap.

**Rejected.** `Room` as an independent aggregate referencing `OfficeId` — rejected because it cannot
enforce per-office name uniqueness or the derived capacity within a transaction boundary.

## D2 — `Company` is a minimal seeded root

**Decision.** Introduce a behaviour-light `Company` aggregate (`Identifier` + `Name`), seeded once at
startup from configuration (idempotent), and have `Office` reference it by `CompanyIdentifier`.

**Why.** `CLAUDE.md` designates `Company` the seeded root that offices belong to, and office-name
uniqueness is defined "within the company" (FR-010) — that scope needs a real company key. Seeding
mirrors the existing `DefaultAdminSeeder` pattern (no new mechanism). We deliberately build *no*
company management (create/rename/delete) — the MVP has exactly one company and the spec asks for
none.

**Rejected.** A hard-coded well-known `CompanyIdentifier` constant with no `Company` row — rejected
because it contradicts the documented model and leaves the `offices.company_identifier` FK dangling
(no referential integrity, awkward for the later multi-company story).

## D3 — Uniqueness is enforced at two levels

**Decision.** Office-name uniqueness *within the company* is enforced by a unique index
`(company_identifier, name)` plus an `ExistsByNameAsync` pre-check in the create/rename handlers.
Room-name uniqueness *within the office* is enforced by the `Office` aggregate **and** a unique index
`(office_identifier, name)`.

**Why.** This mirrors identity's `Email` uniqueness exactly: a set-level invariant the aggregate
can't see is owned by persistence, with a friendly pre-check for the expected-conflict path and the
index as the race-safe backstop. Room-name uniqueness is intra-aggregate, so the aggregate owns it,
with the index as defence-in-depth.

## D4 — No integration events, no Wolverine, this slice

**Decision.** `organization-api` wires **no** messaging. No `OfficeOpened`/`RoomAdded` events are
published.

**Why.** ADR-0005 says introduce messaging "as late as the design allows", and there is no consumer
yet — the **attendance** context's occupancy projection is the future consumer of office/room
capacity, and attendance does not exist. Publishing now would be speculative (golden rule:
simplicity-first, no speculative features). When attendance lands, `OfficeOpened`/`RoomAdded` are
added to `libs/organization/contracts` (ADR-0031) and published over the transactional outbox in a
dedicated slice. Avoiding Wolverine here also keeps the host free of the static-codegen step
(ADR-0034) entirely.

**Consequence.** The host is simpler than `identity-api` (no `AddRoomyMessaging`, no generated
handlers). The `libs/organization/contracts` library is **not** modified by this slice.

## D5 — Authorization is mirrored locally, not shared (yet)

**Decision.** The JWT-bearer setup and the Keycloak realm-role → `ClaimTypes.Role` flattening
(`KeycloakRealmRoles`) are copied into `organization-api` from `identity-api`, rather than extracted
to `service-defaults`.

**Why.** Extracting a shared auth helper is a cross-cutting change that would touch `identity-api`
(outside this branch's scope and prone to conflict with other in-flight identity branches) and, per
golden rule 4, a cross-cutting *pattern* change wants its own ADR. Mirroring the small, proven helper
keeps this slice surgical and ADR-free. Consolidating the per-host auth wiring into `service-defaults`
is a clean follow-up refactor (its own ADR + change) once more than two hosts share it.

**Reads vs writes.** FR-009 restricts *create/change* to administrators. Reads are not restricted by
the spec; employees will need to see offices/rooms to plan attendance. So write endpoints use
`RequireRole("administrator")` (→ 403 for employees, FR-009; 401 without a session); read endpoints
use `RequireAuthorization()` (any authenticated account).
