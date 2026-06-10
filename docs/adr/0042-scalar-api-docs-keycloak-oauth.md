# 0042. Interactive API docs with Scalar and a dev-only Keycloak OAuth try-it-out

- **Status:** Proposed
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

Each context API (`identity-api`, `organization-api`, `attendance-api`) already emits an OpenAPI document
(`AddOpenApi()`/`MapOpenApi()` → `/openapi/v1.json`), and the committed `Roomy.*.Api.json` specs drive the
typed Angular client (ADR-0018/0036). But there is no **human-facing** way to explore an API: a developer or
tester cannot click an endpoint, see example payloads, and execute a real call. The APIs are also **internal**
— in production they are reached only through the YARP BFF, which forwards the Keycloak token and keeps
tokens out of the browser (ADR-0013); the gateway exposes no `/openapi` route (ADR-0030).

We want an interactive reference per service with curated request/response **examples** and a working
**try-it-out** that calls the live endpoints **authenticated** — without weakening the production security
posture, and without standing up a separate auth path the APIs do not already trust.

## Decision drivers

- Low-friction exploration for developers/testers, including **authenticated** calls (most endpoints require
  a realm JWT).
- Reuse the existing trust boundary: the APIs already validate a Keycloak realm JWT as a bearer; the docs
  tool should obtain a **real** such token rather than a bespoke shortcut.
- Keep production unchanged and safe: internal services stay internal (ADR-0030); no docs surface or extra
  auth flow reachable in production.
- Minimal new moving parts; build on the OpenAPI we already emit (ADR-0018/0036) and the realm we already run.

## Considered options

**Renderer — Scalar vs Swagger UI.** Both render an OpenAPI document and support OAuth2 try-it-out. Scalar
(`Scalar.AspNetCore`) has first-class .NET 10 support, a modern UX, good example rendering, and in-code OAuth
flow configuration. Swagger UI is the long-standing alternative. → **Scalar.**

**Try-it-out auth — three options:**
1. **Through the BFF** with a logged-in session (the gateway attaches the token). Most production-faithful,
   but couples the docs tool to the BFF/SPA origin and session, and the gateway deliberately exposes no docs
   route (ADR-0030) — heavy for a dev tool.
2. **Direct OAuth2 authorization-code + PKCE against Keycloak** from Scalar. The developer logs in, Scalar
   receives a real realm access token (browser-safe public-client flow, no client secret) and sends it as the
   bearer. The APIs validate it unchanged.
3. **Paste a bearer token** into Scalar. Simplest, but no real login and tokens get copied around.

→ **Option 2.** It yields a genuine realm token the APIs already trust, uses the standard browser-safe flow,
and stays a self-contained dev tool. The BFF pattern (ADR-0013) remains the production path for the SPA; this
direct flow is a **dev-only** convenience that changes nothing in production.

## Decision

**1. Scalar per service, Development only.** Add `Scalar.AspNetCore` to each host and map it (e.g. at
`/scalar`) **only when `IHostEnvironment.IsDevelopment()`**, alongside the already-Development OpenAPI
endpoint. It is never mapped in production (internal services; ADR-0030). Each service is reached directly in
dev (via its Aspire endpoint); a single gateway entry point is **not** part of this decision (a later dev-only
ADR-0030 exception could add one).

**2. OAuth2 authorization-code + PKCE against Keycloak.** Scalar is configured (in code, at the composition
root, Development only) with the realm's authorization/token endpoints, the docs client id, and the required
scopes; it runs the authorization-code-with-PKCE flow and sends the resulting access token as the bearer on
"Execute". The APIs' existing JWT-bearer validation is unchanged. The concrete Keycloak base address comes
from configuration, so no environment-specific URL is baked into the committed spec.

**3. A dev-only public Keycloak client `roomy-scalar`.** Added to the realm import: `publicClient: true`,
standard flow + **PKCE (S256)**, redirect URIs / web origins for each service's Scalar OAuth callback. It is
**not** the confidential `roomy-bff` client (different flow, different redirects, different trust). It exists
only in the dev realm import.

**4. Documented OAuth2 security scheme + examples.** Each OpenAPI document declares the OAuth2 security scheme
(via a document transformer) so Scalar shows "Authorize", and the main endpoints carry request/response
**examples** (happy path + key error codes, e.g. `201` and `409 room_full`). Because this changes the
committed specs, the typed Angular clients are regenerated and the drift gate stays green (ADR-0036); security
schemes do not affect `ng-openapi-gen` output.

## Consequences

**Positive**
- Developers/testers get a clickable, example-rich reference per service and can execute **authenticated**
  calls with a real Keycloak login.
- Reuses the existing trust boundary — the APIs validate the same realm JWT they always do; no new server-side
  auth path.
- Production posture is unchanged: nothing docs-related is mapped outside Development, and the BFF remains the
  SPA's path (ADR-0013).

**Negative / trade-offs**
- A second Keycloak client (`roomy-scalar`) to maintain in the realm import, and its redirect URIs must track
  the dev service endpoints.
- The docs try-it-out bypasses the BFF, so it is *not* a faithful test of the production BFF token flow — that
  remains covered separately (issue #73).
- Adding the OAuth2 scheme + examples to the committed specs requires a client regen on change (ADR-0036).

**Follow-ups**
- Spec + tasks for the implementation (this ADR is the prerequisite, golden rule 4): wire Scalar in each host
  behind the Development gate, add the document transformer for the security scheme + examples, add the
  `roomy-scalar` realm client, regenerate clients, and add a check that `/scalar` is absent in production.
- Optionally a single dev-only docs entry point through the gateway (a future ADR-0030 exception).
