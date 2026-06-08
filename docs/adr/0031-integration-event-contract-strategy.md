# 0031. Integration-event contract strategy: per-context published language

- **Status:** Proposed
- **Date:** 2026-06-09
- **Deciders:** Heiko Weiß

## Context and problem statement

ADR-0014 left an explicit follow-up open: *"define the integration-event contract
strategy (minimal, versioned)."* The first cross-service flow now forces the decision.
Per ADR-0025 the organization context emits `EmployeeHired`, which the identity context
consumes to run `RegisterUser`, then publishes `UserRegistered` / `UserProvisioningFailed`
back. Contracts are documented in `specs/001-identity-access/contracts/integration-events.md`,
but nothing yet decides **where the contract types live** and **who may reference them**.

The decision is constrained by two existing rules. ADR-0014 warns against *"a fat shared
library that recouples services."* And `CrossContextIsolationConventionTests` (ADR-0003)
forbids any type under `SmartSolutionsLab.Roomy.<Context>` from depending on another
context's namespace — so a consumer cannot reference a producer's event if that event sits
under the producer's context namespace.

## Decision drivers

- ADR-0014: minimal, versioned shared contracts; no central recoupling library.
- ADR-0003 / `CrossContextIsolationConventionTests`: a context must not depend on another
  context's *internal* types. The published language is deliberately not internal.
- Single source of truth per contract — avoid the silent drift of hand-duplicated shapes.
- DDD: the producer owns its **Published Language**; consumers conform to it.

## Considered options

- **A — Per-context published language.** Each context owns a small, versioned `contracts`
  library holding only the integration events it *publishes*. Consumers reference the
  producer's contracts library. One source of truth per contract; ownership is by the
  publishing context.
- **B — Single shared contracts library.** One library holds every cross-context event;
  all contexts reference it. Simplest wiring, but it is exactly the "fat shared library
  that recouples services" ADR-0014 cautions against, and it grows into a kitchen sink.
- **C — Duplicated per context.** No shared types; each context redefines the shapes it
  sends/receives and Wolverine maps by message name. Maximally decoupled, but the same
  contract is maintained in two places and drifts silently.

## Decision

**Option A — per-context published language.** Each publishing context owns a `contracts`
library under its own folder (`libs/<context>/contracts`). A consumer takes a project
reference on the *producer's* contracts library only — never on the producer's domain,
application, or infrastructure.

To satisfy `CrossContextIsolationConventionTests`, the contract types live under a
**context-neutral namespace and assembly**, `SmartSolutionsLab.Roomy.Contracts.<OwningContext>`,
rather than under `SmartSolutionsLab.Roomy.<OwningContext>`. The folder still expresses
ownership (`libs/organization/contracts`), and the namespace's trailing segment still names
the owner (`...Contracts.Organization`), but because the type does **not** reside under
`SmartSolutionsLab.Roomy.Organization`, the isolation rule neither treats it as an
organization-internal type nor forbids identity from referencing it. This is correct by
intent: the published language is shared on purpose, distinct from a context's internal
model. In the Nx tag taxonomy these libraries are `context:shared`.

Contracts are minimal records of **IDs and primitives** (GUIDs, strings, enums, timestamps)
implementing `IIntegrationEvent`. They deliberately do **not** use the owning context's
domain value objects — the wire contract is a serialization-stable boundary, so the
"no primitive obsession" rule (which governs the domain) does not apply to it. Contracts
are additive and versioned per ADR-0014; a breaking change is a new version with a
deprecation window.

For this first flow:

- `libs/organization/contracts` → `SmartSolutionsLab.Roomy.Contracts.Organization`:
  `EmployeeHired`.
- `libs/identity/contracts` → `SmartSolutionsLab.Roomy.Contracts.Identity`:
  `UserRegistered`, `UserProvisioningFailed`.

Mapping a consumed wire event onto an internal command happens at the infrastructure edge
(the Wolverine consumer), so the application layer stays free of other contexts' published
language and depends only on its own commands and its own published events.

## Consequences

**Positive**
- One source of truth per contract, owned and versioned by its producer; no drift.
- No central library that recouples every service (ADR-0014 honoured).
- The isolation architecture test keeps enforcing the real rule — contexts still cannot
  reference each other's *internal* types; only the explicit published language is shared.

**Negative / trade-offs**
- A consumer takes a compile-time dependency on the producer's contracts assembly; a
  breaking contract change ripples to consumers (mitigated by additive versioning).
- The neutral `...Contracts.<Owner>` namespace differs from the `...<Owner>.<Layer>`
  convention of the rest of a context — a deliberate, documented exception.
- Creating a producer's contracts library can slightly precede the producer context itself
  (here, `organization/contracts` exists before the organization service), since the
  consumer needs the type first.

**Follow-ups**
- Add the two contracts libraries and register them in `Roomy.slnx` and
  `Roomy.ArchitectureTests` (so they are loaded for inspection).
- When the organization context is built, it publishes `EmployeeHired` from this same
  library and consumes `identity/contracts` for the acknowledgements.
- If the contracts libraries ever gain Nx `project.json` tags, tag them `context:shared`.
