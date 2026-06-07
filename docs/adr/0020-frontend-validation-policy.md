# 0020. Frontend validation: trust the OpenAPI-generated client, validate only untrusted input

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Domain concepts on the frontend are branded types (TS standards), which must be minted
from input. The OpenAPI-generated Angular client (ADR-0018) already provides TypeScript
types for every backend DTO, derived from the contract. The open question was how much
*runtime* validation to add, and whether to introduce a schema library.

## Decision drivers

- Avoid hand-maintaining a second copy of the API shape (drift risk).
- Minimize dependencies and bundle footprint.
- Put validation effort where untrusted data actually enters.

## Considered options

- **Trust the generated client for backend DTOs; runtime-validate only untrusted input.**
- Generate schemas (Zod/Valibot) from OpenAPI to runtime-validate backend responses too.
- Hand-write validators everywhere — rejected: duplicates the contract by hand and drifts.

## Decision

Backend DTOs are **trusted** via the OpenAPI-generated client — no runtime re-validation.
Branded domain values are minted by **mapping the generated DTOs at the data-access
boundary**, validating only where the contract cannot express a domain rule. **Runtime
validation** (and brand minting via smart constructors) applies only to **genuinely
untrusted input**: form input, third-party APIs/webhooks, and values from URLs or storage.
No hand-maintained parallel copy of the API shape — the OpenAPI spec is the single source.

## Consequences

**Positive**
- Minimal footprint; the contract is the single source of truth.
- Validation effort is focused on the surface where it matters.

**Negative / trade-offs**
- Relies on backend-contract correctness — mitigated by keeping the generated client in
  sync (CI) and by contract tests at the BFF/service seam.
- Contract drift is not caught at runtime for backend responses — accepted for a
  same-team, contract-tested backend; revisit if integrating less-trusted services.

**Follow-ups**
- Keep the generated client regenerated/in sync in CI.
- Thin DTO → domain mapping with brand minting at data-access.
- Small validation helpers (or a light validator) for the untrusted-input surface.
- Contract tests at the BFF/service seam.
