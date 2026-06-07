# 0025. User/Employee provisioning across Identity and Organization

- **Status:** Proposed <!-- direction decided (Option A); → Accepted on merge -->
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Every account in Roomy is a `User` (email, password, role) in the **identity** context
and an `Employee` (company assignment) in the **organization** context, in a 1:1
relationship "created together" (`001-identity-access`, the context map). Because the two
sides live in different services with their own databases (ADR-0014), creating "an
employee account" is inherently a **two-service** operation, yet the artifacts describe it
two different ways:

- `001-identity-access` scenario 4 reads as a **synchronous, identity-led** flow: an admin
  "creates an account with the Employee role… *and that person can log in*" — implying
  immediate consistency.
- The event-storming diagram routes it the other way and **organization-led**:
  `HireEmployee` (organization) → *policy: provision the User* → `RegisterUser` (identity).

ADR-0014 forbids distributed transactions ("use sagas / eventual consistency") and ADR-0003
forbids one context referencing another's aggregates. So the flow cannot be both atomic and
cross-service. We must pick the entry point and the consistency model, and decide what
"can log in immediately" means when the two records are created in separate services.

## Decision drivers

- ADR-0014: no distributed transactions; cross-service workflows use sagas + eventual
  consistency, integrating only by ID and integration events.
- ADR-0003: no cross-context aggregate references; the dependency is by `UserId`/`EmployeeId`.
- Domain truth: an admin "hires" a colleague (organization concern), and that colleague
  needs credentials to log in (identity concern). The two must converge.
- Testability of the `001` acceptance criteria — particularly "can log in immediately".
- Avoiding orphaned half-accounts (a `User` with no `Employee`, or vice versa) on failure.

## Considered options

- **A — Organization-led saga (matches the event storming).** `HireEmployee` is the entry
  point in **organization**; it emits `EmployeeHired`, and a process manager drives
  `RegisterUser` in **identity**. Login becomes possible once the `User` side completes.
- **B — Identity-led saga.** "Create account" is the entry point in **identity**; it emits
  `UserRegistered`, and **organization** reacts by creating the `Employee`. The admin's
  hiring intent is modelled as an identity command.
- **C — Merge identity and organization into one context** so creation is a single local
  transaction. Rejected on sight — it collapses a generic subdomain into a supporting one
  and contradicts ADR-0014's three-service topology.

## Decision

**Option A — organization-led saga.** `HireEmployee` in **organization** is the single
entry point; it persists the
`Employee` and, via the transactional outbox, emits `EmployeeHired`. A process manager
consumes it and issues `RegisterUser` to **identity**, which creates the `User` with the
chosen role and the admin-set initial password. The 1:1 link is by `UserId`/`EmployeeId`
only.

The `001` acceptance criteria are re-cast to **eventual** consistency: "the account is
created and the colleague can log in **once provisioning completes**", rather than
synchronously within one request. Failure of the identity step leaves the saga incomplete
and retryable (inbox/outbox), and a compensating action removes or flags the dangling
`Employee` if provisioning is abandoned.

This is recommended because hiring is the real-world trigger and the supporting context
(organization) naturally owns the master-data lifecycle, with identity as the downstream
credential provider.

## Consequences

**Positive**
- One unambiguous entry point; the event storming and the specs agree.
- No distributed transaction; aligns with ADR-0014 sagas + outbox/inbox (ADR-0005/0015).
- The half-account failure mode is handled explicitly by the saga, not left implicit.

**Negative / trade-offs**
- "Log in immediately" is no longer literally true; `001` scenario 4 and its tests must be
  rewritten to assert *eventual* login, with a defined convergence expectation.
- A new process manager + integration-event contracts (`EmployeeHired`, and an identity-side
  acknowledgement) to build and test.
- A brief window where an `Employee` exists without a usable `User`; UI/UX must account for it.

**Follow-ups**
- Amend `001-identity-access` (scenario 4, FR-006) and align `002-office-management` so the
  entry point is `HireEmployee` (organization) and the login criterion reads as *eventual*.
- Define the integration-event contracts and the compensating action for an abandoned saga.
- Decide how the seeded `DefaultAdmin` (which must exist before any saga runs) provisions
  both its `User` and its `Employee` record at startup.
