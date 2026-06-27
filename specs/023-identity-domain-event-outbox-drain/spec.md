# Feature Specification: Identity domain events drain into the outbox

**Feature Branch**: `023-identity-domain-event-outbox-drain`

**Created**: 2026-06-27

**Status**: Draft

**Realizes**: the ADR-0037 follow-up — *"if a third state-based publisher appears, consider lifting the
drain into a shared base unit of work"*. Identity is that second state-based publisher; this slice lifts
the commit-time drain out of `OrganizationUnitOfWork` into a shared base and has identity use it.

## Summary

A behaviour-preserving infrastructure correction that closes a **latent publish gap** in the identity
context. `OrganizationUnitOfWork.SaveChangesAsync` drains its aggregates' `DomainEvents`, maps the
publishable ones to integration-event contracts, and stages them into the transactional outbox in the
same commit as the aggregate write (ADR-0037). `IdentityUnitOfWork.SaveChangesAsync` does **none** of
this — it is a bare `context.SaveChangesAsync` with no drain
(`backend/libs/identity/infrastructure/Persistence/IdentityUnitOfWork.cs:7-8`).

Because EF Core `Ignore`s the `DomainEvents` property and the identity unit of work never drains it, any
domain event raised by an identity aggregate is **silently dropped at commit**. `User.GrantAdministrator`
raises `AdministratorGranted` (`backend/libs/identity/domain/Users/User.cs:50`), persisted via
`GrantAdministratorHandler` through `unitOfWork.SaveChangesAsync`
(`backend/libs/identity/application/Commands/Handlers/GrantAdministratorHandler.cs:25-26`) — so the event
is raised, the role change commits, and the event vanishes. No consumer subscribes to it today, so the
defect is **latent**: the publish seam is simply missing, and the failure mode is silent (no error, no
warning).

This feature makes identity domain events flow through the transactional outbox exactly as
organization's do, by **lifting the drain into a shared base unit of work** that both contexts use — so
the two cannot drift, and a future identity domain event is published by construction rather than
needing the drain re-implemented. The concrete first beneficiary is `AdministratorGranted`, which gains
an identity integration-event contract and reaches the outbox transactionally. This is **backend only**
and **behaviour-preserving** for every existing flow (organization's publishes, identity's existing
saga/consumer publishes, and any save that raises no event).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Identity domain events reach the outbox transactionally (Priority: P1)

When an identity aggregate raises a domain event with cross-context significance and its unit of work
commits, that event MUST be staged into the transactional outbox in the **same** commit as the aggregate
write — never dropped. The observable first case is `AdministratorGranted`: granting administrator both
persists the role change and enqueues the corresponding integration event atomically.

**Why this priority**: This is the correctness defect. A raised domain event being silently discarded
defeats the outbox guarantee (ADR-0012) for the identity context and means any future consumer of an
identity event would never receive it — with no error to reveal the gap. The publish seam must exist
before any consumer can rely on it.

**Independent Test**: Drive `GrantAdministrator` against a provisioning-then-active user through the
real identity stack (Postgres + outbox); assert the user's role is administrator **and** that exactly one
`AdministratorGranted` integration event is present in the outbox, committed with the same transaction —
and that the aggregate's `DomainEvents` are cleared afterward (not re-published on a later save).

**Acceptance Scenarios**:

1. **Given** an active user who is not yet an administrator, **When** `GrantAdministrator` is handled and
   the unit of work commits, **Then** the role change and exactly one `AdministratorGranted` integration
   event are persisted in a single transaction (both present, or — on a forced commit failure — neither).
2. **Given** the same grant, **When** the commit fails (e.g. the transaction rolls back), **Then**
   **neither** the role change **nor** the outbox row persists — there is no aggregate-without-event and
   no event-without-aggregate outcome.
3. **Given** a successful grant, **When** the user's unit of work commits a second, unrelated change
   later, **Then** `AdministratorGranted` is **not** published again (the drained events were cleared).
4. **Given** the grant is already an idempotent no-op (the user is already an administrator, so
   `GrantAdministrator` raises no event per `User.cs:47`), **When** it is handled, **Then** no
   `AdministratorGranted` event is staged.

---

### User Story 2 - Existing flows are unchanged and the two contexts cannot drift (Priority: P1)

Lifting the drain MUST NOT change any existing behaviour. Organization MUST keep publishing
`OfficeOpened` / `RoomAdded` / `EmployeeHired` exactly as before; a commit that raises no domain event
MUST still persist its aggregate write; and both contexts MUST share **one** drain implementation so a
future change to the drain applies to both and they cannot silently diverge.

**Why this priority**: The drain is correctness-critical and runs on every state-based save. A regression
here would break organization's already-relied-upon capacity feed, or could re-introduce the silent-drop
gap in one context while fixing the other. Sharing one implementation is the structural guarantee that
the fix is permanent for both.

**Independent Test**: Run organization's existing integration tests (office/room creation publishing
their events) unchanged and assert they stay green; run a no-event save through each context's unit of
work and assert the aggregate write commits with an empty outbox; assert by construction (a single shared
base type) that both contexts' units of work obtain their drain from the same place.

**Acceptance Scenarios**:

1. **Given** organization's existing publish flows, **When** an office/room is created (or an employee
   hired) and committed, **Then** the same integration events are staged into the outbox as today — no
   change in which events, their payloads, or their transactional guarantee.
2. **Given** any state-based aggregate change that raises **no** domain event, **When** the unit of work
   commits, **Then** the aggregate write persists and **no** outbox row is created (an empty drain is a
   normal commit, not an error).
3. **Given** the identity and organization units of work, **Then** both delegate the drain to a single
   shared base unit of work (one implementation of "collect `DomainEvents` → map → stage in the outbox in
   the same commit → clear"), so the two cannot diverge.
4. **Given** identity's existing saga/consumer publishes (`UserRegistered`, `UserProvisioningFailed`
   emitted from inside Wolverine handlers via the publisher port), **When** those flows run, **Then** they
   are unaffected — this slice adds the *commit-time drain* path and does not alter the
   publish-from-a-handler path.

---

### Edge Cases

- **Multiple events in one commit**: if more than one identity aggregate (or more than one event) is
  drained in a single save, all map and stage in the one transaction; ordering follows the existing
  organization behaviour (no new ordering guarantee is introduced).
- **An unmapped domain event**: a domain event with no integration-event mapping is drained and cleared
  but stages nothing (the map returns "no integration event"), matching organization's `_ => null`
  behaviour — it is **not** an error. Only events with cross-context significance get a mapping.
- **No consumer yet for `AdministratorGranted`**: publishing it with zero subscribers is intentional and
  safe — the outbox relays at-least-once; the absence of a consumer does not make the publish a defect.
- **Concurrency / retry**: the drain is part of the state-based `SaveChangesAsync`; it inherits the
  existing transaction and outbox semantics — this slice introduces no new retry or concurrency loop.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A **shared base unit of work** (in shared infrastructure — persistence/messaging) MUST own
  the commit-time drain: collect the tracked aggregates' `DomainEvents`, map each to its integration-event
  contract, stage the mapped events into the transactional outbox **enrolled on the same `DbContext`**,
  commit aggregate rows and outbox rows in **one** transaction, then clear the drained events. The
  per-context domain→integration mapping MUST remain a context-supplied infrastructure-edge concern (as
  organization's map is today, ADR-0031/0037).
- **FR-002**: `OrganizationUnitOfWork` MUST be re-expressed in terms of the shared base with **no change**
  to which integration events it publishes, their payloads, or their transactional atomicity (its current
  behaviour is the regression contract).
- **FR-003**: `IdentityUnitOfWork` MUST use the shared base so that identity aggregates' `DomainEvents` are
  drained, mapped, staged in the outbox, and cleared on commit — replacing today's bare
  `context.SaveChangesAsync`.
- **FR-004**: Granting administrator MUST cause exactly one `AdministratorGranted` integration event to be
  staged into the outbox in the same commit as the role change. This requires an identity integration-event
  contract for `AdministratorGranted` (IDs/primitives only, ADR-0031) and an identity domain→integration
  map entry; the existing `AdministratorGranted` **domain** event and `GrantAdministratorHandler` MUST be
  unchanged (the handler still calls only `unitOfWork.SaveChangesAsync`, owning no drain logic).
- **FR-005**: A commit that drains **no** mapped event MUST still persist its aggregate write and create no
  outbox row (an empty drain is a normal, successful commit in both contexts).
- **FR-006**: Drained events MUST be cleared after a successful commit so a subsequent unrelated save does
  not re-publish them; the drain MUST stage each raised, mapped event **exactly once**.
- **FR-007**: The drain MUST preserve atomicity: aggregate write and outbox rows commit together or not at
  all — no publish-after-commit gap and no event without its aggregate (ADR-0012).
- **FR-008**: No `domain` or `application` code MUST reference Wolverine, the broker, or the
  `Contracts.*` types; the new mapping and the shared base live at the infrastructure edge (ADR-0005). The
  identity infrastructure project MUST take whatever messaging/outbox references organization already takes
  (it currently takes none), without leaking them inward.
- **FR-009**: No route, status code, response body, or OpenAPI schema MAY change; no Angular client
  regeneration is required (this is a backend infrastructure change with no public-surface impact).

### Non-functional Requirements

- **NFR-001**: All existing quality gates stay green — `dotnet build -warnaserror`, `dotnet test`
  (unit + integration + architecture), `dotnet format --verify-no-changes`, and `pnpm nx affected -t lint`
  — with **no** new suppression, disabled rule, or skipped test.
- **NFR-002**: If the shared base is a new project (or the identity layers gain an infrastructure
  dependency), the architecture tests MUST still pass and any new context/library MUST be referenced by
  `Roomy.ArchitectureTests` so the dependency rule is enforced, not vacuously green.

### Key Entities *(include if feature involves data)*

- **Shared base unit of work**: the single infrastructure type owning "drain `DomainEvents` → map → stage
  in the outbox in the same commit → clear", consumed by both `IdentityUnitOfWork` and
  `OrganizationUnitOfWork`.
- **`AdministratorGranted` (domain event → integration event)**: an existing identity domain event
  (`UserId`, `OccurredAt`); this slice adds its published-language integration contract (IDs/primitives)
  and the identity map entry that lets it reach the outbox.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After a successful `GrantAdministrator`, the outbox contains **exactly one**
  `AdministratorGranted` integration event, committed in the same transaction as the role change — verified
  against the real identity stack.
- **SC-002**: A forced commit failure during a grant leaves **zero** role changes **and** **zero**
  `AdministratorGranted` outbox rows (all-or-nothing).
- **SC-003**: Organization's existing publish integration tests pass **unchanged**, proving the lift
  preserved its behaviour.
- **SC-004**: Exactly **one** drain implementation exists, referenced by both contexts' units of work
  (zero duplicated drain code, so the contexts cannot drift).
- **SC-005**: A no-event save in either context commits its aggregate write with an **empty** outbox, and a
  repeat grant on an already-administrator user stages **zero** events.

## Test-first plan (Red → Green)

- **Red** — write failing tests first (ADR-0052; Shouldly assertions, NSubstitute for unit doubles):
  - Identity integration test: `GrantAdministrator` stages exactly one `AdministratorGranted` in the
    outbox in the same commit (fails today — bare save drops it).
  - Identity integration test: forced commit failure stages neither the role change nor the event.
  - Unit/integration: a no-event identity save commits with an empty outbox; a second unrelated save does
    not re-publish a previously drained event.
  - Regression: organization's existing publish tests stay green after the lift.
- **Green** — lift the drain into the shared base, re-express both units of work over it, add the
  `AdministratorGranted` integration contract + identity map entry, wire the identity infrastructure
  references. Minimum code to pass; no speculative generality.
- **Refactor** — under green, remove the now-duplicated drain shape from organization; confirm the map is
  the only per-context glue.

## ADR impact

No new ADR is required — this realizes the explicit ADR-0037 follow-up. **ADR-0037 SHOULD be updated** in
the same change: record that the second state-based publisher (identity) landed, that the drain was lifted
into a shared base unit of work consumed by both contexts (closing the anticipated follow-up), and that
identity now publishes `AdministratorGranted` through that seam. Documentation drift is a defect (CLAUDE.md).

## Assumptions

- **Latent, not live**: no consumer subscribes to `AdministratorGranted` today; the fix restores the
  publish seam and atomic guarantee, not a currently-broken downstream feature.
- **Messaging already wired in identity-api**: identity already runs the outbox/transport for its
  saga/consumer publishes; this slice adds the state-based drain path, not the messaging backbone.
- **Cross-context significance**: `AdministratorGranted` is the one identity event mapped in this slice;
  other identity domain events without cross-context meaning stay unmapped (drained, staged nothing).

## Out of Scope

- Building a consumer for `AdministratorGranted` (none exists; this slice only restores the publish seam).
- Changing identity's existing publish-from-a-Wolverine-handler path (`UserRegistered`,
  `UserProvisioningFailed`).
- Any new retry/concurrency semantics for the drain beyond what the existing state-based save provides.
- Any public API, gateway, OpenAPI, or frontend change.

## Review & Acceptance Checklist

- [ ] Every functional requirement has a test written **before** its implementation and now passes
- [ ] `AdministratorGranted` reaches the outbox transactionally with the role-change commit (FR-004/FR-007)
- [ ] A no-event save still commits its aggregate write with an empty outbox (FR-005)
- [ ] One shared base unit of work is used by **both** identity and organization (FR-001/FR-003; SC-004)
- [ ] Organization's existing publish behaviour is unchanged (FR-002; SC-003)
- [ ] No Wolverine/broker/`Contracts.*` reference leaks into `domain`/`application` (FR-008)
- [ ] Wire contract unchanged; no OpenAPI re-emit, no client regen (FR-009)
- [ ] ADR-0037 updated to record the lift and identity as the second publisher
- [ ] All gates green; no suppressions or skipped tests
- [ ] No open clarification markers remain
