# 0046. One shared domain-Error → HTTP mapping at the API edge

- **Status:** Accepted
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

Every context API turns a domain `Error`/`Result` failure into an HTTP response, but the three hosts did it
differently:

- **Attendance** had a complete, correct mapper (`ErrorResults.ToHttpResult`): `Validation→422`, `NotFound→404`,
  `Conflict→409`, `Forbidden→403`, `Unauthorized→401`, else `500`, with body `{ code, message }`.
- **Organization** (`OfficeEndpoints.MapError`) mapped only `NotFound→404` (empty body) and `Conflict→409`
  (plain-text message), and **everything else — including `Validation` — fell through to `Results.Problem` →
  500**, with no `{code,message}` body.
- **Identity** mapped inline: `grant-administrator` on a not-yet-activated account returns
  `Error.Validation("user.not_active", …)`, which the endpoint mapped to `Results.Problem` → **500**, when the
  client error is **422**. Its 404s returned an empty body and its 400 used ProblemDetails.

So the same domain error kind produced different status codes and different bodies depending on the service —
and one was an outright bug (a client validation error surfaced as a 500).

## Decision

Introduce **`libs/web-http`** (`SmartSolutionsLab.Roomy.Web.Http`) holding the single mapping, extracted from
attendance's (the correct one):

- `Error.ToHttpResult()` — the kind→status table above, body always `ErrorResponse(code, message)`.
- `Error.ToBadRequest()` — a 400 with the same `{code,message}` body, for request-shape validation (a malformed
  cursor/limit) that is not a domain rule.
- public `ErrorResponse(string Code, string Message)`.

All three hosts reference it and call `ToHttpResult()`/`ToBadRequest()` for their domain-error responses; the
per-host mappers (`MapError`, the inline `Results.Problem`/`Results.NotFound()`/`Results.Conflict`) are deleted.
The lib depends only on shared-kernel + ASP.NET and carries no `Domain/Application/Infrastructure` namespace
segment, so the architecture layer rules never select it. Request-shape `400`s that are not built from an
`Error` (organization's `Results.BadRequest("An office requires …")`) stay as-is.

## Consequences

**Positive**
- One definition of the error contract; the same domain error kind now yields the same status + `{code,message}`
  body across all three services. The **identity `user.not_active` 500→422 bug is fixed** (a regression test was
  added first). Organization's 404/409 now carry the `{code,message}` body.
- New error kinds map correctly everywhere for free.

**Negative / trade-offs**
- The endpoints' OpenAPI metadata still declares `ProducesProblem` (ProblemDetails) for several error
  responses, so the **committed specs are unchanged by this PR** (no client regen) but remain inaccurate about
  the error body — exactly as attendance already was. Aligning `.ProducesProblem` → `.Produces<ErrorResponse>`,
  re-emitting the three specs, and regenerating the typed Angular clients is a **follow-up** (kept separate so
  this behaviour fix stays small and the spec/client churn is reviewed on its own).

## Notes
Third slice of the backend de-duplication pass (after the test-support lib and the shared Keycloak auth lib /
ADR-0045). Behaviour change is intentional and limited to the error status/body; no use-case logic changed.
