# Phase 0 Research: Hire Employee

Decisions resolving the design unknowns for the organization side of the provisioning saga. The
cross-cutting decision (organization-led, eventual consistency, compensation) is **ADR-0025 (Accepted)**;
this file records the finer-grained choices the plan builds on. No new ADR is required.

## R1 — Entry point and consistency model (reaffirming ADR-0025)

**Decision:** `HireEmployee` in **organization** is the single entry point; it persists the `Employee`
and emits `EmployeeHired`, and a process manager drives `RegisterUser` in identity. Login becomes
possible **once the identity step completes** — eventual consistency, not within the hire request.

**Rationale:** ADR-0025 already chose Option A (organization-led) over identity-led or a merged context,
because hiring is the real-world trigger and organization owns the master-data lifecycle. ADR-0014 forbids
distributed transactions. Re-deciding here would contradict an accepted ADR.

**Alternatives considered:** Identity-led (rejected by ADR-0025 — inverts the domain trigger);
synchronous two-phase creation (rejected — distributed transaction, ADR-0014).

## R2 — `Employee` provisioning state machine

**Decision:** The `Employee` aggregate carries a **`ProvisioningState`**: `Provisioning` → `Active`
(on `UserRegistered`) or `Provisioning` → `Failed` (on `UserProvisioningFailed`, the compensation).
Transitions are aggregate methods — `Hire` (creates in `Provisioning`, raises the `EmployeeHired` domain
event), `CompleteProvisioning` (→ `Active`), `FailProvisioning(reason)` (→ `Failed`). `Active`/`Failed`
are terminal for a given hire.

**Rationale:** The aggregate is the consistency boundary (ADR-0003); the state machine makes the
no-half-account guarantee (FR-007) an enforced invariant rather than a convention. Behaviour lives in the
aggregate (constitution II), and the terminal states make the saga outcome observable (SC-003).

**Alternatives considered:** A boolean `IsActive` (rejected — cannot express *failed* vs *still
provisioning*, hiding the half-account window); a separate saga/state table outside the aggregate
(rejected — moves the invariant out of the consistency boundary).

## R3 — Compensation: mark failed, not hard-delete

**Decision:** An abandoned or failed provisioning leaves the employee in **`Failed`** (retained, visible,
with a coarse reason), not deleted. ADR-0025's "removes **or** flags" is realized as **flags failed**.

**Rationale:** Retaining the failed employee gives the administrator an audit trail and something to act
on (re-hire after fixing the cause), and keeps the compensation a simple state transition rather than a
cascading delete. The spec adopts this (Assumptions); it materially shapes US2's observable outcome.

**Alternatives considered:** Hard-delete the dangling employee (rejected for v1 — loses the failure
signal and the audit trail; revisit if `Failed` rows accumulate). A "retry from the org side" command is
out of scope (re-hire covers it).

## R4 — Email uniqueness is owned by the credential side

**Decision:** Work-email uniqueness is enforced where login accounts live (identity/Keycloak). The hire
proceeds without a cross-service email reservation; a taken email surfaces as `UserProvisioningFailed`
(`EmailTaken`) → the employee is marked `Failed` (FR-010). Organization does **not** call identity to
pre-check.

**Rationale:** Organization cannot atomically reserve an email it does not own without a synchronous
cross-service call (forbidden, ADR-0014). The identity handler already returns `email_taken` as a coarse
reason (`RegisterUserHandler`), so the compensation path already exists end-to-end. This keeps hire a
local, fast operation and routes the authoritative check to its owner.

**Alternatives considered:** A synchronous "is this email free?" call to identity (rejected — cross-service
read, race-prone, ADR-0014). A **local** uniqueness check among organization's own non-failed employees
as a fast-fail (deferred — a cheap UX improvement that catches org-visible duplicates, but not
Keycloak-only accounts; can be added later without changing the contract).

## R5 — Publish on hire, consume the acks: reuse the existing seams

**Decision:** `HireEmployee` raises the `EmployeeHired` **domain event**; the existing
`OrganizationUnitOfWork` drains it at commit and `OrganizationIntegrationEventMap` maps it to the existing
`EmployeeHired` **integration contract** (carrying `EmployeeId`, the **pre-allocated** `UserId`, email,
display name, role, and the transient initial password), staged in the outbox (ADR-0037). The acks are
**inbox consumers** (`UserRegisteredConsumer`, `UserProvisioningFailedConsumer`) that map identity's
contracts to the internal `CompleteEmployeeProvisioning` / `FailEmployeeProvisioning` commands at the
infrastructure edge (ADR-0031), mirroring how identity's `EmployeeHiredConsumer` maps inbound.

**Rationale:** Both seams already exist and are proven (organization publishes `OfficeOpened`/`RoomAdded`
the same way; identity/attendance consume by mapping to internal commands). The `UserId` is pre-allocated
at hire so it is the stable 1:1 correlation key across both sides (FR-006), exactly as the identity
consumer expects (`UserIdentifier.From(message.UserId)`). The initial password rides the integration
event only (never persisted on the employee, FR-009), as the identity handler already consumes it.

**Alternatives considered:** A bespoke saga/process-manager framework (rejected — the
domain-event-drain + inbox-consumer pattern already covers it, constitution VII); letting identity
allocate the `UserId` (rejected — the contract and identity consumer are built around an
organization-supplied `UserId`, and ADR-0031's `EmployeeHired` already carries it).

## R6 — organization-api becomes a consumer (inbox + codegen)

**Decision:** organization-api, currently **publish-only**, now also **consumes**: `AddRoomyMessaging`
scans the organization **infrastructure** assembly for the two new consumers, the durable **inbox** is
enabled on the organization database, and the two generated Wolverine handlers are committed and added to
the CI `codegen verify` step for organization-api.

**Rationale:** The acks must be handled transactionally (the state transition + inbox dedup commit
together, ADR-0012). Identity and attendance already run this consumer shape; organization simply gains
it. The Wolverine static-codegen policy (ADR-0034) requires committing the regenerated handlers and
gating them in CI.

**Alternatives considered:** Polling identity for outcomes (rejected — cross-service read + latency,
ADR-0014); a separate consumer host (rejected — needless service for two handlers; organization owns the
Employee it must transition).

## Cross-spec follow-up (noted, not in this feature's scope)

ADR-0025 calls for **amending `001-identity-access`** (scenario 4 / FR-006) so the login criterion reads
as *eventual* ("can log in once provisioning completes") rather than synchronous. `001` is already
implemented; this is a spec-text alignment, tracked as a small follow-up, not part of the hire-employee
build. The seeded `DefaultAdmin` startup provisioning is likewise out of scope (spec) and remains an
ADR-0025 follow-up.
