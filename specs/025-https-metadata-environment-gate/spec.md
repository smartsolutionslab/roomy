# Feature Specification: Gate HTTPS metadata enforcement by environment

**Feature Branch:** `refactor/025-https-metadata-environment-gate`
**Status:** Draft
**Created:** 2026-06-27
**Updated:** 2026-06-27
**Realizes:** ADR-0013 (Keycloak OIDC resource-server validation), ADR-0045 (shared Keycloak JWT
bearer composition)

## Summary

A security-hardening refactor of the shared Keycloak JWT bearer composition. Today
`AddKeycloakJwtBearer` (in `backend/libs/infrastructure-authentication`) sets
`options.RequireHttpsMetadata = false` **unconditionally**, and all three context API hosts
(`identity-api`, `organization-api`, `attendance-api`) inherit that single setting (ADR-0045). The
flag exists for one good reason — local Keycloak runs over plain `http` under Aspire — but baking it
to `false` for *every* environment means a production resource server will fetch its signing-key
metadata over an unauthenticated channel, a needless downgrade even behind the BFF (ADR-0013).

This slice flips the default: HTTPS metadata enforcement is `true` everywhere, relaxed **only** in
Development (or by explicit configuration). The behaviour change is confined to non-Development
environments; local-dev startup against `http` Keycloak is unchanged. No route, status code, or
response body changes — the wire contract and the OpenAPI specs are untouched, so no client
regeneration.

## User Scenarios & Testing

### Primary story

As a maintainer, I want HTTPS metadata enforced by default and relaxed only where a developer needs
it, so that a non-Development deployment cannot silently fetch Keycloak metadata over plain HTTP, and
the relaxation lives in exactly one shared place rather than drifting per host.

### Acceptance Scenarios

1. **Secure by default outside Development**
   - GIVEN the shared extension is composed under a non-Development environment (e.g. `Production`,
     `Staging`) with no override configured
   - WHEN the JWT bearer options are built
   - THEN `RequireHttpsMetadata` is `true`.

2. **Relaxed in Development**
   - GIVEN the shared extension is composed under the `Development` environment with no override
     configured
   - WHEN the JWT bearer options are built
   - THEN `RequireHttpsMetadata` is `false`, so local Keycloak over `http` continues to work.

3. **Explicit configuration overrides the environment default**
   - GIVEN a configuration value that explicitly sets the requirement
   - WHEN the shared extension is composed (in any environment)
   - THEN `RequireHttpsMetadata` takes the configured value, overriding the environment-derived
     default (so a non-Development environment can opt out deliberately, and Development can opt in).

4. **All three hosts inherit the gate from the shared extension**
   - GIVEN the identity, organization, and attendance hosts
   - THEN none of them sets `RequireHttpsMetadata` locally; each derives the value solely through
     `AddKeycloakJwtBearer`, so the three cannot diverge.

5. **Local-dev startup is unchanged (regression)**
   - GIVEN the Aspire-orchestrated local stack pointing at `http` Keycloak (Development)
   - WHEN the hosts start and validate a BFF-forwarded token
   - THEN startup and validation behave exactly as today — no new HTTPS requirement is imposed
     locally.

### Edge cases

- The host environment is the empty/unset default (ASP.NET treats an unset `Environment` as
  `Production`) → the secure default (`true`) applies, matching scenario 1.
- An override value that is present but unparsable as a boolean → the behaviour is defined and
  tested (the environment-derived default applies); a malformed override never silently disables the
  requirement.

## Requirements

### Functional

- **FR-001:** `AddKeycloakJwtBearer` MUST set `RequireHttpsMetadata = true` by default for any
  non-Development environment with no explicit override.
- **FR-002:** `AddKeycloakJwtBearer` MUST set `RequireHttpsMetadata = false` in the `Development`
  environment with no explicit override, preserving local Keycloak-over-`http` ergonomics.
- **FR-003:** An explicit configuration value MUST override the environment-derived default in either
  direction (relax in non-Development, enforce in Development); the configuration key MUST live under
  the existing `Keycloak` section and be read in the shared extension only.
- **FR-004:** The current environment and the configuration value MUST reach the extension (e.g. via
  the host's `IHostEnvironment`/`IConfiguration`); the extension MUST NOT hardcode the requirement and
  MUST NOT read ambient process state outside the supplied environment/configuration.
- **FR-005:** `identity-api`, `organization-api`, and `attendance-api` MUST obtain the requirement
  solely through `AddKeycloakJwtBearer`; no host may set `RequireHttpsMetadata` itself, so all three
  stay in lockstep with the shared definition (ADR-0045).
- **FR-006:** No route, status code, response body, or OpenAPI schema MAY change; no Angular client
  regeneration is required. The only observable behaviour change is the metadata-fetch transport
  requirement in non-Development environments.

### Non-functional

- **NFR-001:** The extension MUST stay messaging/EF-free so it cannot perturb a host's Wolverine
  static codegen (ADR-0045); the change introduces no new package dependency beyond what is needed to
  read environment/configuration.
- **NFR-002:** All existing quality gates stay green (`dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, the architecture tests, and `pnpm nx affected -t lint`).

## Test-first plan (Red → Green)

- Unit (auth-lib test coverage): compose `AddKeycloakJwtBearer` under a fake non-Development
  environment with no override and assert the resolved `JwtBearerOptions.RequireHttpsMetadata` is
  `true`; under `Development` with no override assert `false`; with an explicit override assert it wins
  in both directions. Resolve the options from the built `IServiceProvider`
  (`IOptionsMonitor<JwtBearerOptions>` for the bearer scheme) rather than asserting on source.
- Unit (regression): the existing `KeycloakRealmRoles` claims-transformation behaviour is unaffected
  by the gate (the `OnTokenValidated` shaping still runs).
- Integration (regression, real stack): the existing identity/organization/attendance integration
  tests run under Development and stay green unchanged — they are the contract that local behaviour
  did not move.

## Out of scope

- Changing audience validation, issuer/authority resolution, or the realm-role flattening — only the
  HTTPS-metadata requirement is in scope.
- Serving the context APIs themselves over HTTPS, or any TLS/cert configuration for Keycloak or the
  BFF — this slice concerns only the resource server's *metadata-fetch* requirement.
- Standing up a dedicated auth-lib test project if one does not yet exist beyond what these tests
  need (the ADR-0045 follow-up); reuse the existing auth-lib test coverage home.
- Any change to the BFF session, cookies, or Keycloak realm configuration (ADR-0013).

## Review & Acceptance Checklist

- [ ] Every functional requirement has a test written before its implementation
- [ ] `RequireHttpsMetadata` is `true` by default and `false` only in Development (or by explicit config)
- [ ] The override is read once, in the shared extension, under the `Keycloak` section
- [ ] No host sets `RequireHttpsMetadata` locally; all three inherit it
- [ ] Local-dev startup over `http` Keycloak is unchanged
- [ ] Wire contract unchanged; no OpenAPI re-emit, no client regen
- [ ] Extension stays messaging/EF-free; all gates green; no suppressions
