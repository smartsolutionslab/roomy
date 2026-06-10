# Feature Specification: Interactive API docs (Scalar + Keycloak OAuth)

**Feature Branch:** `feat/010-api-docs`
**Status:** Draft
**Created:** 2026-06-10
**Updated:** 2026-06-10
**Realizes:** ADR-0042 (Scalar + dev-only Keycloak OAuth try-it-out, with the Scalar.Aspire dashboard update);
issue #135
**Bounded context:** Cross-cutting (platform / developer experience) — the AppHost, the three API hosts'
OpenAPI documents, and the Keycloak realm import. No domain model.

## Summary

Give developers an interactive **Scalar** reference for the backend APIs, reachable from the **Aspire
dashboard**, with request/response **examples** and a **Keycloak OAuth login** for authenticated try-it-out.
The APIs already emit OpenAPI (`/openapi/v1.json`) and validate a realm JWT as a bearer (ADR-0013/0018/0036).

Delivered via the **`Scalar.Aspire` aggregator**: one Scalar reference hosted by the AppHost and listed in the
dashboard that aggregates all three APIs' OpenAPI documents in a single pane, plus a direct **OpenAPI** link on
each API resource. This is **dev-only by construction** — the AppHost is never deployed — so no per-host
environment gating is needed, and production is unaffected. The remaining work adds the OAuth2 security scheme
to the documents, configures the aggregator for the Keycloak authorization-code + PKCE flow, adds a dev-only
public `roomy-scalar` realm client, and curates examples.

## User Scenarios & Testing

### Primary User Story
As a developer, I want to open the API reference from the Aspire dashboard, log in with my Keycloak account,
and try endpoints with real example payloads, so that I can explore and test the APIs without writing a client
or hand-crafting tokens.

### Acceptance Scenarios

1. **Reachable from the dashboard** *(delivered)*
   - GIVEN the stack running via the AppHost
   - WHEN a developer opens the Aspire dashboard
   - THEN a single **Scalar** resource lists all three APIs' references, and each API resource shows a direct
     **OpenAPI** link.
   - *Covered by `AppHostCompositionTests` (the Scalar resource + the per-API OpenAPI URL are composed).*

2. **Dev-only by construction** *(delivered)*
   - GIVEN the docs are hosted by the AppHost (never deployed)
   - THEN there is no docs surface reachable in any deployed environment; production is unaffected (ADR-0030).

3. **OAuth2 login is offered** *(remaining)*
   - GIVEN the aggregated Scalar reference
   - WHEN it loads a document
   - THEN the document declares an OAuth2 **authorization-code (PKCE)** scheme pointing at the realm, and Scalar
     shows an "Authorize" configured with the `roomy-scalar` client.

4. **Authenticated try-it-out** *(remaining — manual/quickstart)*
   - GIVEN a developer who has authorized via Keycloak in Scalar
   - WHEN they execute a secured endpoint (e.g. `GET /reservations/mine`)
   - THEN Scalar sends the obtained token as the bearer and the API accepts it (no API auth change).

5. **Examples are shown** *(remaining)*
   - GIVEN an endpoint with documented examples (e.g. `POST /reservations`)
   - THEN Scalar shows a realistic request body and representative responses (happy path + `409 room_full`).

### Edge Cases
- The Keycloak base address is environment-specific: it MUST come from configuration / service discovery, never
  be baked into a committed OpenAPI spec.
- Adding the OAuth2 security scheme changes each committed `Roomy.*.Api.json`: the typed Angular clients MUST be
  regenerated so the drift gate stays green (ADR-0036).
- `roomy-scalar` is dev-only: it MUST NOT alter the production realm or the confidential `roomy-bff` client.

## Requirements

### Functional

- **FR-001** *(done)* The AppHost MUST compose a single `Scalar.Aspire` reference that aggregates the three
  context APIs' OpenAPI documents and is listed in the Aspire dashboard.
- **FR-002** *(done)* Each context API resource MUST carry a direct OpenAPI dashboard link.
- **FR-003** The docs surface MUST be dev-only — hosted by the AppHost (not deployed); production behaviour is
  unchanged (no docs surface, no new public auth path; ADR-0013/0030).
- **FR-004** Each OpenAPI document MUST declare an OAuth2 **authorization-code with PKCE** security scheme whose
  authorization/token endpoints resolve to the configured Keycloak realm, applied to the secured operations.
- **FR-005** The Scalar aggregator MUST be configured (reading the Keycloak base address from configuration) to
  use the `roomy-scalar` client id and PKCE so "Authorize" performs the realm login.
- **FR-006** A dev-only **public** Keycloak client `roomy-scalar` MUST be added to the realm import (public,
  standard flow + PKCE S256, redirect URIs / web origins for the Scalar callback). It MUST NOT reuse or modify
  `roomy-bff`.
- **FR-007** The main endpoints MUST carry request/response **examples** (happy path + at least one
  representative error) in the OpenAPI documents.
- **FR-008** A short developer note (CONTRIBUTING/README) MUST explain how to open Scalar from the dashboard and
  log in.

### Non-Functional / Constraints
- AppHost composition + host-edge wiring only; no docs concern in `domain`/`application`.
- No new warnings/suppressions; full gate suite green; specs + generated clients regenerated and committed.

## Out of Scope
- A single docs entry point through the gateway (a possible future dev-only ADR-0030 exception).
- Production-facing API documentation / a public developer portal.
- Faithful end-to-end testing of the production BFF token flow (tracked separately, #73).

## Success Criteria
- Scenarios 1–2 covered by `AppHostCompositionTests` (delivered); 3 and 5 by automated checks on the documents
  once added; 4 by the quickstart manual smoke.
- All quality gates green; ADR-0042 moves toward Accepted on completion.
