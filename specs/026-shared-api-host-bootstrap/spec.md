# Feature Specification: Share the API-host bootstrap across the three context hosts

**Feature Branch:** `refactor/026-shared-api-host-bootstrap`
**Status:** Draft
**Created:** 2026-06-27
**Updated:** 2026-06-27
**Realizes:** extends ADR-0045 (shared Keycloak/host wiring) and ADR-0050 (one owned seam in `web-http`,
not per-host bespoke config); preserves ADR-0036 (OpenAPI drift gate) and ADR-0042 (Scalar dev docs).

## Summary

A behaviour-preserving backend de-duplication. The three context hosts —
`backend/apps/identity-api/Program.cs`, `backend/apps/organization-api/Program.cs`, and
`backend/apps/attendance-api/Program.cs` — repeat the same ~30-line bootstrap verbatim, **including the
identical explanatory comments**: `builder.AddServiceDefaults()`; `AddOpenApi(options =>
options.CreateSchemaReferenceId = EndpointSchemaIds.ForEndpointDto)`; `AddRoomyExceptionHandling()`;
`AddKeycloakJwtBearer(...)`; the `IsEmittingOpenApiDocument()` → `JasperFxEnvironment.AutoStartHost = true`
emit toggle (with its `getdocument`/AutoStartHost comment); and the middleware tail `MapDefaultEndpoints()`
→ `UseExceptionHandler()` → `UseAuthentication()`/`UseAuthorization()` → `MapOpenApi()` (with its
`/openapi/v1.json` comment) → `RunJasperFxCommands(args)` (with its codegen comment). This slice extracts
two shared helpers into `backend/libs/web-http` — a builder-side `AddRoomyApiDefaults(...)` and an app-side
`UseRoomyApiPipeline(...)` — so each host keeps only its context-specific wiring (persistence, use cases,
messaging, options, seeding, endpoint mapping). No route, status code, response body, or OpenAPI change —
the emitted specs stay byte-identical and no Angular client is regenerated.

## User Scenarios & Testing

### Primary story

As a maintainer, I want the common host bootstrap defined once in `web-http`, so the three hosts cannot
drift apart and adding a fourth context starts from a shared, tested baseline rather than a copy-paste of
thirty lines and their comments.

### Acceptance Scenarios

1. **One bootstrap definition**
   - GIVEN `web-http` exposes `AddRoomyApiDefaults(...)` and `UseRoomyApiPipeline(...)`
   - WHEN the identity, organization, and attendance hosts are built
   - THEN each host calls both helpers, and the duplicated lines **and their explanatory comments**
     (service defaults, OpenAPI schema-id, exception handling, JWT bearer, the emit/AutoStartHost toggle,
     and the `MapDefaultEndpoints → UseExceptionHandler → UseAuthentication/UseAuthorization → MapOpenApi →
     RunJasperFxCommands` tail) appear nowhere in any `Program.cs`.

2. **OpenAPI documents are byte-identical**
   - GIVEN the committed `Roomy.Identity.Api.json`, `Roomy.Organization.Api.json`, and
     `Roomy.Attendance.Api.json`
   - WHEN the build re-emits each host spec (`OpenApiGenerateDocumentsOnBuild=true`, `OpenApi__EmitDocument=true`)
   - THEN every emitted document is unchanged — the drift gate stays green (ADR-0036) and no client is
     regenerated. The `#/components/schemas/AccountResponse` reference and `GetCurrentAccount` operation id
     in `OpenApiDocumentTests` still hold.

3. **The emit / AutoStartHost toggle is preserved**
   - GIVEN `OpenApi:EmitDocument` is `true`
   - WHEN a host bootstraps through `AddRoomyApiDefaults(...)`
   - THEN `JasperFxEnvironment.AutoStartHost` is set to `true` exactly as before, the helper reports the
     emitting state back to the host so it can skip the messaging runtime, and with `EmitDocument` unset the
     toggle is **not** applied.

4. **Authentication behaviour is unchanged (regression)**
   - WHEN the existing integration tests exercise each host over the real stack
   - THEN a valid BFF-forwarded realm JWT authenticates and administrator-only routes still authorize on the
     realm role, missing/invalid tokens still yield `401`, and the Keycloak base address/realm each host
     passes in (the context realm; identity's admin realm) is honoured unchanged.

5. **Exception handling behaviour is unchanged (regression)**
   - WHEN an endpoint throws across any host
   - THEN the response is shaped by the same `ArgumentExceptionHandler` + ProblemDetails pipeline (status
     code and body identical to today).

6. **Scalar dev docs are unchanged**
   - GIVEN the Aspire-hosted Scalar aggregator (ADR-0042) reads each host's `/openapi/v1.json`
   - THEN every host still maps `/openapi/v1.json` in every environment, so the aggregator and OpenAPI
     dashboard links behave exactly as before.

### Edge cases

- The host that wires extra `AddOpenApi`/use-case calls fluently in one chain today (attendance, organization)
  keeps the same registrations and the same effective DI graph after the extraction.
- `RunJasperFxCommands(args)` still returns the process exit code, so `dotnet run -- codegen write` and a
  no-argument Aspire launch behave as before.

## Requirements

### Functional

- **FR-001:** `web-http` MUST expose a builder-side helper (e.g.
  `WebApplicationBuilder AddRoomyApiDefaults(this WebApplicationBuilder builder, Uri keycloakBaseAddress,
  string realm)`) that performs, once, the shared registrations every host repeats today: service defaults,
  `AddOpenApi` with `EndpointSchemaIds.ForEndpointDto`, `AddRoomyExceptionHandling`, and
  `AddKeycloakJwtBearer`.
- **FR-002:** The same helper (or a sibling read) MUST apply the emit toggle — when
  `IConfiguration.IsEmittingOpenApiDocument()` is true, set `JasperFxEnvironment.AutoStartHost = true` — and
  MUST surface the emitting state to the caller so each host can keep gating its messaging runtime on it.
  The toggle MUST NOT be applied when `OpenApi:EmitDocument` is unset/false.
- **FR-003:** `web-http` MUST expose an app-side helper (e.g.
  `Task<int> UseRoomyApiPipeline(this WebApplication app, string[] args)`) that runs the shared middleware
  tail in the same observable order — `MapDefaultEndpoints`, `UseExceptionHandler`,
  `UseAuthentication`/`UseAuthorization`, `MapOpenApi`, and `RunJasperFxCommands(args)` — returning its exit
  code. Mapping each context's own endpoints remains the host's responsibility and MUST stay in the host,
  composed so authentication/authorization still apply to those endpoints exactly as today.
- **FR-004:** `identity-api`, `organization-api`, and `attendance-api` MUST consume FR-001–FR-003; the
  duplicated bootstrap statements **and the duplicated explanatory comments** MUST be removed from all three
  `Program.cs` files. Each host retains only its context-specific wiring (connection string + persistence,
  use cases, options/validation, messaging registration and its emit gate, seeding, and endpoint mapping).
- **FR-005:** `web-http` MUST take **no** domain, application, or context-infrastructure dependency; the
  helpers traffic only in ASP.NET/host primitives and the Keycloak base address + realm string the host
  supplies. The architecture tests and Nx boundary lint MUST stay green.
- **FR-006:** No route, status code, response body, or OpenAPI schema MAY change, and no Angular client
  regeneration is required; the three committed host specs stay byte-identical.

### Non-functional

- **NFR-001:** The extraction MUST be behaviour-preserving — the emitted OpenAPI documents, the
  authentication/authorization outcomes, the exception/ProblemDetails responses, and the JasperFx command
  dispatch are observationally identical before and after.
- **NFR-002:** All existing quality gates stay green (`dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, the architecture tests, the OpenAPI/codegen drift gates, and
  `pnpm nx affected -t lint`). No suppressions, no skipped tests.

## Test-first plan (Red → Green)

- Unit (`web-http` test project): `AddRoomyApiDefaults(...)` registers the OpenAPI schema-id callback
  (`EndpointSchemaIds.ForEndpointDto`), the exception-handling services, and the JWT-bearer/authorization
  services; the emit toggle sets `JasperFxEnvironment.AutoStartHost` only when `OpenApi:EmitDocument` is true
  and reports the emitting state back.
- Integration / document (regression, real stack): the existing `OpenApiDocumentTests` (and its
  organization/attendance counterparts, plus the build-time spec emit) stay green unchanged — the
  byte-identical documents are the contract that behaviour did not move.
- Integration (regression): the existing per-host auth and endpoint integration tests stay green unchanged —
  `401` on missing/invalid token, admin-only authorization, and exception/ProblemDetails bodies are the
  contract that the shared pipeline preserves behaviour.

## Out of scope

- The gateway/BFF host (`backend/apps/gateway`) and the `dev-seeder` host — they do not share this
  context-API bootstrap and are not touched.
- Any change to messaging registration, persistence, options, or seeding — these stay host-local; only the
  shared cross-cutting bootstrap moves.
- Changing the JWT-bearer claim shaping, the realm-role flattening, roles, or Keycloak config.
- Adding or moving any route, examples, or the OAuth2 security scheme (ADR-0042 follow-ups are unaffected).

## Review & Acceptance Checklist

- [ ] Every functional requirement has a test written before its implementation
- [ ] All three hosts call `AddRoomyApiDefaults(...)` and `UseRoomyApiPipeline(...)`; no duplicated bootstrap
      line or comment remains in any `Program.cs`
- [ ] The emit/AutoStartHost toggle is preserved and gates messaging exactly as before
- [ ] `web-http` takes no domain/application/infrastructure dependency
- [ ] The three OpenAPI documents are byte-identical; drift gate green; no client regen
- [ ] Auth, exception handling, and Scalar docs behaviour unchanged
- [ ] All gates green; no suppressions
