# 0045. Share the Keycloak JWT bearer composition and the IUnitOfWork port

- **Status:** Accepted
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

Each context API host (`identity-api`, `organization-api`, `attendance-api`) validates the BFF-forwarded
Keycloak access token the same way (ADR-0013): a JWT bearer against the realm, audience not validated, with
realm roles flattened from `realm_access.roles` to `ClaimTypes.Role` claims. That ~40-line composition block
and the `KeycloakRealmRoles` claims-transformer were **copied verbatim into all three hosts** (the organization
copy even carried a comment that it was "mirrored from identity-api… to keep this slice surgical"). Separately,
the `IUnitOfWork` commit-seam port was **defined identically** in both `backend/libs/identity/application` and
`backend/libs/organization/application` (the attendance context is event-sourced and has no such seam). Three copies of
the auth wiring and two of the port mean three/two places to change and a real chance of drift.

## Decision

**1. A shared Keycloak JWT auth composition lib.** Introduce `backend/libs/infrastructure-authentication`
(`SmartSolutionsLab.Roomy.Infrastructure.Authentication`) holding the single `KeycloakRealmRoles` and an
`AddKeycloakJwtBearer(this IServiceCollection, Uri keycloakBaseAddress, string realm)` extension that
encapsulates the JWT-bearer registration + `AddAuthorization()`. Each host reads its Keycloak base address and
realm from configuration (kept inline) and calls the extension. The lib depends only on
`Microsoft.AspNetCore.Authentication.JwtBearer` (moved off the three hosts) — **no messaging/EF dependency**, so
it cannot perturb a host's Wolverine static codegen (ADR-0034). It is referenced by the API hosts only.

**2. `IUnitOfWork` moves to the owned-abstractions lib.** The identical port moves to
`backend/libs/application-contracts` (`SmartSolutionsLab.Roomy.Application.Contracts.Messaging`), next to
`ICommandHandler`/`IQueryHandler`. The two context application libs already reference that lib, and the handlers
already import its namespace, so no handler changes are needed; each context's infrastructure still implements
the port over its own DbContext.

## Consequences

**Positive**
- One definition of the Keycloak token validation + claims transformation, and one `IUnitOfWork` — drift across
  hosts/contexts is no longer possible; a change to either is one edit.
- The auth lib is a clean composition seam reused by every host and easily covered by its own tests.
- No behaviour change: the extension reproduces the exact JWT options, and the moved port keeps its signature.
  Verified — the three hosts' committed Wolverine codegen is byte-identical after the change.

**Negative / trade-offs**
- One more shared lib and one more package owner (`JwtBearer` now lives in the auth lib). Acceptable for the
  de-duplication; the lib stays messaging-free to protect codegen.
- `KeycloakRealmRoles` is public on the shared lib (as it was on each host) so its existing unit test can target
  it; ideally that test moves into an auth-lib test project (a small follow-up).

## Notes
Introduced together with the implementing refactor (PR for ADR-0045). Part of the backend de-duplication pass;
follows PR for the test-utility consolidation and precedes the error→HTTP standardization.
