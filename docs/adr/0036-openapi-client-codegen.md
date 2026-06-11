# 0036. OpenAPI client codegen: build-time spec emit, ng-openapi-gen, drift-gated in CI

- **Status:** Proposed
- **Date:** 2026-06-09
- **Deciders:** Heiko Weiß

## Context and problem statement

ADR-0018 chose REST/JSON documented with OpenAPI for the synchronous edge and committed to a
**generated** typed Angular client; ADR-0020 made the OpenAPI spec the single source of truth
and put runtime validation only on untrusted input, trusting the generated DTOs. Neither was
implemented: the .NET hosts emit no OpenAPI document, and the SPA talks to the gateway through
hand-written `HttpClient` wrappers that duplicate the contract by hand (the drift risk ADR-0020
set out to avoid).

This decision fixes the concrete toolchain: how the backend produces a spec, which generator
turns it into the Angular client, where generated code lives, and how CI keeps it in sync.

## Decision drivers

- The spec must be producible in CI **without running the app** (no live DB/broker/Keycloak)
  and **without a JRE** — the workspace is pnpm + Node + .NET only.
- The generated client must be idiomatic Angular 22: standalone, zoneless, `HttpClient` +
  `provideHttpClient`, no `NgModule`/`forRoot`; models as plain interfaces (ADR-0020).
- Same-origin through the YARP gateway (ADR-0030) — the client uses relative paths, not an
  absolute service URL.
- Drift between contract and client must fail the build, not be caught by review.
- Reuse the patterns already in the repo rather than invent new ones.

## Considered options

**Spec emission**

- **`Microsoft.AspNetCore.OpenApi` + `Microsoft.Extensions.ApiDescription.Server`** — first-party;
  emits a static JSON document at `dotnet build` time by booting the host's service container far
  enough to resolve the document (no HTTP server, no DB/broker), exactly as ADR-0034's Wolverine
  `codegen write` builds its model offline.
- Swashbuckle — third-party dependency; weaker .NET 10 story; runtime-oriented. Rejected.
- Runtime `/openapi/v1.json` scraped in CI — needs the app and its dependencies up. Rejected.

**Generator**

- **`ng-openapi-gen`** — TS-native (no JRE); purpose-built for Angular `HttpClient`, emits
  `@Injectable({providedIn:'root'})` services + interface models with a configurable root URL.
- `orval` — also TS-native, strong monorepo ergonomics; kept as the fallback.
- `@openapitools/openapi-generator-cli` (typescript-angular) — requires a Java/JRE in CI and its
  templates lag Angular majors (may emit `NgModule`/`forRoot`). Rejected.
- Microsoft Kiota — emits a fluent request-builder with its own HTTP stack, not Angular
  `HttpClient`; bypasses DI/interceptors. Rejected for the frontend.

## Decision

- **Backend** emits a static OpenAPI document per host with `Microsoft.AspNetCore.OpenApi`
  (`AddOpenApi`) and `Microsoft.Extensions.ApiDescription.Server` (`OpenApiGenerateDocuments`),
  written to `backend/apps/<host>/<AssemblyName>.json` and **committed**. Endpoints carry explicit
  `.Produces<T>()`/`.ProducesProblem(...)`/`.WithName(...)` metadata so the document — and thus
  the generated types and method names — are accurate and stable.
- **Client** is generated with **`ng-openapi-gen`** (version-pinned), root URL configured to
  `''` so calls stay relative and same-origin through the gateway. **`orval` is the fallback** if
  ng-openapi-gen's Angular 22 output proves unworkable.
- **Generated code lives in a per-context `api` lib** (ADR-0035; renamed from `data-access` per its
  2026-06-11 amendment), under `frontend/libs/<context>/api/src/lib/generated`, and is **committed**.
  The generated tree is excluded from ESLint and Prettier (it is a build artifact, not authored code)
  but is still type-checked by the lib build. Per ADR-0020 the feature lib never consumes generated
  DTOs directly: a thin facade in the `api` lib maps each DTO to a branded domain type at the
  boundary.
- **Regeneration is an Nx target** — `nx run <context>-api:generate-client` — driven from
  the committed spec; it requires no running app and no JRE.
- **CI gates drift** with the established `git diff --exit-code` pattern (ADR-0034): the .NET job
  re-emits the spec on build and fails if `backend/apps/<host>/<AssemblyName>.json` changed; the Nx job
  re-runs `generate-client` and fails if the committed generated tree changed. The build-time
  spec emit reuses the same dummy-config env block the Wolverine codegen step already uses.

## Consequences

**Positive**
- The contract is the single source of truth (ADR-0020); the hand-maintained client copy and its
  drift risk are removed.
- Spec and client both regenerate offline in CI — no app, no DB/broker, no JRE.
- Reuses two patterns already in the repo: per-context `data-access` libs (ADR-0035) and the
  `git diff --exit-code` codegen gate (ADR-0034).

**Negative / trade-offs**
- Generated code is committed, so a contributor must run `generate-client` after a contract
  change or CI fails — the intended forcing function, but a step to remember.
- ng-openapi-gen is a community tool ahead of Nx's official Angular 22 support (ADR-0027);
  mitigated by pinning the version and compiling the generated lib before any consumer wires it.

**Follow-ups**
- Apply the same pipeline to `organization-api` when its frontend lands (deferred — no consumer
  today).
- Establish branded identifiers + smart constructors on the frontend (first use lands with this
  work) and the DTO → branded mapping at each `data-access` facade.
