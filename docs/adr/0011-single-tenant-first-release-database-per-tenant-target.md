# 0011. Single-tenant first release, database-per-tenant as the target

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Roomy is a B2B product, so multi-tenancy is desirable long term — but building it adds
significant scope (tenant routing, isolation, onboarding, per-tenant operations) that the
first release does not need. We want to ship v1 without multi-tenancy while avoiding
decisions that would make adding it expensive later. A `Company` concept is needed in v1
(at least a mutable name) and is the natural future tenant boundary.

## Decision drivers

- Keep v1 scope minimal (simplicity first) — don't build tenancy before a customer needs
  it.
- Preserve a clean path to strong isolation.
- GDPR posture favours strong per-tenant isolation eventually (trivial erasure, residency).

## Considered options

- Build multi-tenancy now — rejected for v1 on scope.
- Single-tenant v1 but "shared-schema ready" (a `TenantId` discriminator everywhere) —
  rejected: that targets the wrong model and adds throwaway plumbing.
- **Single-tenant v1, database-per-tenant as the eventual model, modelling `Company`
  now.** — chosen.

## Decision

v1 is **single-tenant**: one deployment serves one company, one database, with no tenant
routing or isolation machinery. We model a **`Company` aggregate** (mutable name, plus
company-level settings as they emerge); it is the future tenant boundary. The **target
multi-tenancy model is database-per-tenant**. We deliberately do **not** introduce a
shared-schema `TenantId` discriminator. Because persistence sits behind application ports
(ADR-0003) and is wired at the composition root, introducing a per-tenant connection/store
resolver later is an infrastructure + composition-root change, not a domain change — so v1
needs no special tenancy plumbing beyond modelling `Company` cleanly. Tenant provisioning,
routing, and per-tenant operations are explicitly out of v1 scope.

## Consequences

**Positive**
- Smallest v1 scope; no tenancy complexity to build, test, or operate now.
- Clean domain — no tenant-discriminator noise.
- The path to database-per-tenant is preserved and localized to infrastructure and the
  composition root.
- `Company` exists from the start, so adding tenancy won't require reworking the org model.

**Negative / trade-offs**
- Going multi-tenant later is still real work (tenant provisioning, connection resolution
  from the authenticated principal, migrations across tenant databases, per-tenant ops) —
  deferred, not eliminated.
- We must resist shared-schema shortcuts (a `TenantId` column) that would diverge from the
  database-per-tenant target.

**Follow-ups**
- Place `Company` in the organization/identity context when contexts are named.
- Keep DB connection/store acquisition in `infrastructure`, behind the existing ports.
- Revisit tenancy before onboarding the first additional customer.
