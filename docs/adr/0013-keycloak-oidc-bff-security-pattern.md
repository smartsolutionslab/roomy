# 0013. Authentication via self-hosted Keycloak (OIDC) with the BFF security pattern

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Roomy needs authentication and authorization across the YARP BFF, the context APIs, and
the Angular app. We self-host (GDPR, Hetzner) and want standards-based identity with a
growth path to SSO/federation for future multi-tenant customers. Holding tokens in the
browser is an avoidable risk worth designing out from the start.

## Decision drivers

- Self-hosting and GDPR alignment.
- Standards-based OIDC, with a path to SSO/federation.
- Keep access/refresh tokens out of the browser.
- Keep auth logic out of the SPA.

## Considered options

- **Self-hosted Keycloak (OIDC) with the BFF security pattern.**
- Microsoft Entra ID — rejected: managed and less aligned with self-hosting.
- ASP.NET Core Identity — rejected: lowest infra now, but no growth path to SSO and a
  likely later migration.

## Decision

**Keycloak** (self-hosted) is the OIDC provider. The **BFF security pattern** runs at
YARP: the BFF performs the OIDC Authorization Code flow with PKCE as a confidential
client, holds the session server-side, and issues the browser only a secure, HTTP-only,
SameSite cookie — no access or refresh tokens reach the SPA. The BFF attaches the access
token to downstream context-API calls; context APIs validate the JWT as resource servers.
Authorization uses Keycloak roles/claims mapped to API authorization policies. v1 uses a
single realm for the one company; the multi-tenant realm strategy is deferred (ADR-0011).
Keycloak runs as a container in local development, orchestrated by .NET Aspire.

## Consequences

**Positive**
- Tokens never reach the browser — a smaller XSS/exfiltration surface.
- Standards-based, self-hosted, GDPR-aligned; a clear SSO/federation path for the
  multi-tenant future.
- The Angular app carries no token or OIDC logic — cookie-authenticated calls to the BFF,
  redirect to the BFF login endpoint on 401.

**Negative / trade-offs**
- Keycloak is another service to operate and to run locally (mitigated by Aspire).
- The BFF must correctly handle sessions, token refresh, and secure-cookie configuration.

**Follow-ups**
- Configure the Keycloak realm and a confidential BFF client (PKCE).
- JWT validation on the context APIs; map roles/claims to authorization policies.
- Run Keycloak via Aspire for local development.
- Secure cookie configuration (HTTP-only, Secure, SameSite).
- Decide the multi-tenant realm strategy when tenancy is built.
