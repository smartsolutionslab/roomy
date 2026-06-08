# 0030. Serve the Angular SPA through the gateway as a single origin

- **Status:** Accepted
- **Date:** 2026-06-08
- **Deciders:** Heiko Weiß

## Context and problem statement

The Angular app (ADR-0016) and the YARP gateway/BFF (ADR-0013) exist but were not wired
together: the SPA was not run by the Aspire app host, and it had no way to reach the BFF.
ADR-0016 already states "the SPA talks only to the single YARP BFF origin"; ADR-0013's BFF
security pattern keeps the session in an `HttpOnly`, `SameSite`, `__Host-` cookie and never
exposes a token to the browser. Both only hold if the SPA and the BFF share one origin — a
cross-origin SPA cannot carry the BFF cookie on its API calls, and the OIDC login/callback
would land on a different origin than the app.

We need to decide how the browser reaches the SPA and the BFF in local development.

## Decision drivers

- The BFF cookie + OIDC callback require the SPA and the API to be **same-origin** (ADR-0013).
- One browser-facing origin keeps the client simple (ADR-0016).
- Local dev should still get Angular's hot reload.
- The Aspire app host should bring the whole system up with one command (ADR/issue #17).

## Considered options

- **Gateway is the single origin; it proxies the SPA.** The browser hits the gateway. YARP
  proxies non-API routes to the Angular dev server (hot reload in dev; built static files in
  production — a later slice), and serves `/bff` (and future `/api`) itself.
- **Angular dev server is the entry, proxying `/bff`+`/api` to the gateway.** Simpler Angular
  config, but OIDC login/logout redirect the browser to the gateway origin, so the BFF
  session cookie is set cross-origin — fragile `SameSite` handling.
- **SPA calls the gateway cross-origin with CORS.** Simplest wiring, but breaks the BFF
  no-tokens / `__Host-` cookie model (ADR-0013).

## Decision

**The gateway is the single browser-facing origin.** In the Aspire app host the Angular dev
server runs as a resource; the gateway adds a catch-all YARP route that proxies everything it
does not own to the dev server, while `/bff/*` and the health endpoints are handled by the
gateway directly (they are more specific, so routing prefers them). The SPA calls the BFF with
**relative** URLs (`/bff/...`), which therefore stay same-origin — no CORS, no cross-origin
cookie.

Mechanism notes:

- The dev server is hosted with core Aspire's **`AddExecutable`** (`pnpm nx serve web` on a
  fixed dev port). We deliberately avoid `Aspire.Hosting.NodeJs`, which still publishes on the
  9.x line while the rest of Aspire here is 13.4.2 — mixing the two is the same unsupported
  version skew recorded in ADR-0027 for Nx/Angular, and the Community Toolkit equivalent is
  only a 13.0.0 pre-release.
- The catch-all → dev-server route lives in `appsettings.Development.json` only. Production
  serving of the built SPA (gateway static files / fallback) is a separate later slice.

## Consequences

**Positive**
- One origin for the browser: BFF cookie, OIDC callback, and API calls all align (ADR-0013).
- The SPA needs no API base URL, no CORS, and no token handling.
- `dotnet run` on the app host brings up the SPA together with the gateway and backing services.
- Hot-module-reload works through the single origin: the Vite HMR client targets the page origin
  (the gateway) and YARP proxies the websocket upgrade — verified with a `101 Switching Protocols`
  handshake through the gateway — so no direct dev-server connection is needed.

**Negative / trade-offs**
- The gateway is in the request path for static assets in dev. Acceptable; it mirrors prod.
- Production SPA serving is not solved here; it is an explicit follow-up.

**Follow-ups**
- Production: serve the built SPA from the gateway (static files + SPA fallback), replacing the
  dev-only proxy route — tracked in #90, with the deployment slice (ADR-0017).
- Revisit `Aspire.Hosting.NodeJs` / Community Toolkit Node hosting once a 13.x-aligned release
  exists, if it simplifies the `AddExecutable` wiring.
