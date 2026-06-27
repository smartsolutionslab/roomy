# Feature Specification: Declare the real error body (`ErrorResponse`) in the OpenAPI contract

**Feature Branch:** `027-error-response-openapi-contract`
**Status:** Draft
**Created:** 2026-06-27
**Updated:** 2026-06-27
**Realizes:** corrects the deferred follow-up flagged in ADR-0046 (shared domain-`Error` → HTTP mapping);
honours ADR-0036 (build-time OpenAPI emit + drift-gated client codegen)

## Summary

A contract-correctness fix with **no change to the bytes the server sends**. Today every host emits the
body `ErrorResponse { code, message }` for its `400` responses (via `ErrorResults.ToBadRequest` or the
`ArgumentExceptionHandler`) and for its handler-driven `403` responses (via `Error.ToHttpResult` on an
`Error.Forbidden`) — the same body it correctly advertises for `404`/`409`/`422`. But those `400`/`403`
responses are declared with `.ProducesProblem(...)`, so the committed OpenAPI documents describe them as
`application/problem+json` → `ProblemDetails`. The contract therefore contradicts itself: the *identical*
`ErrorResponse` body is typed as `ErrorResponse` for a `404` and as `ProblemDetails` for a `400` on the same
endpoint. Because the Angular client is generated from these documents and drift-gated (ADR-0036), the
generated client types the `400`/`403` error body **wrongly**.

ADR-0046 deferred this, assuming aligning `400`/`401`/`403` would require *changing response bodies*. It does
not: the `400` and handler-driven `403` bodies are already `ErrorResponse`, so only the metadata is wrong.
This slice points the metadata at reality. Two statuses are genuinely **empty-bodied** — a `403` from the
`RequireAdministrator` policy (no handler runs) and the `401` from `Results.Unauthorized()` in
`AccountEndpoints` — and those are declared as empty, not as any JSON schema.

This **changes the emitted OpenAPI documents**, so the three committed specs are re-emitted and the typed
Angular clients regenerated (ADR-0036). The wire contract is unchanged — only the published spec is corrected
to match what the servers have always sent.

## User Scenarios & Testing

### Primary story

As a frontend consumer of the generated client, I want a `400` or `403` error body typed as `ErrorResponse`
(matching what the server actually returns) so that I can read `code`/`message` without the generated client
mis-typing it as `ProblemDetails`, and so the truly empty `401`/`403` responses are not advertised as carrying
a body they never do.

### Acceptance Scenarios

1. **A `400` is typed as `ErrorResponse`**
   - GIVEN any endpoint that returns `400` (e.g. `POST /offices` with an invalid name, `GET /reservations/mine`
     with a malformed cursor, `GET /occupancy` with both `officeId` and `roomId`)
   - WHEN its OpenAPI operation is inspected
   - THEN the `400` response declares `application/json` with the `ErrorResponse` schema, never
     `application/problem+json`/`ProblemDetails`.

2. **A handler-driven `403` is typed as `ErrorResponse`**
   - GIVEN `POST /reservations` and `DELETE /reservations/{reservationId}` (a non-admin on-behalf /
     cancel-another fails with `Error.Forbidden` → `ErrorResponse` body)
   - WHEN their OpenAPI operations are inspected
   - THEN each `403` declares `application/json` with the `ErrorResponse` schema.

3. **A policy-only `403` is typed as empty**
   - GIVEN `GET /reservations/employees` and `GET /reservations/by-employee/{employeeId}` (admin-only via
     `RequireAdministrator`; a denied caller is rejected by the authorization policy before any handler runs)
   - WHEN their OpenAPI operations are inspected
   - THEN each `403` is declared with **no response body** — neither `ProblemDetails` nor `ErrorResponse`.

4. **The `401` on `/account/me` is typed as empty**
   - GIVEN `GET /account/me` returning `Results.Unauthorized()` when the subject claim is absent
   - WHEN its OpenAPI operation is inspected
   - THEN the `401` is declared with **no response body**.

5. **The wire bytes are unchanged (regression)**
   - WHEN the affected endpoints are exercised over the real stack as in the existing integration tests
   - THEN every status code and response body is byte-for-byte what it is today: a `400`/handler `403` still
     returns `{ "code": …, "message": … }`; the policy `403` and the `/account/me` `401` still return an empty
     body.

6. **Generated client and drift gate**
   - WHEN the three host specs are re-emitted and `generate-client` is re-run
   - THEN the committed OpenAPI documents and the committed generated Angular trees reflect the new typing, and
     both `git diff --exit-code` drift gates (ADR-0036) are green.

### Edge cases

- A document from which the last `ProblemDetails` reference is removed MUST drop the now-unused
  `ProblemDetails` component schema (the drift gate catches a stale leftover).
- The `ErrorResponse` schema already exists in all three documents (declared for `404`/`409`/`422`), so this
  change adds no new model — it only widens which statuses reference it.

## Requirements

### Functional

- **FR-001:** `web-http` MUST expose a single uniform helper — `RouteHandlerBuilder ProducesError(this
  RouteHandlerBuilder, int statusCode)` — equivalent to `.Produces<ErrorResponse>(statusCode)` (an
  `application/json` `ErrorResponse` body). It MUST take no domain dependency.
- **FR-002:** Every endpoint response that **emits an `ErrorResponse` body** MUST declare it via FR-001 (or
  `.Produces<ErrorResponse>(status)`), and **no `ErrorResponse`-bodied status MAY remain declared as
  `.ProducesProblem(...)`**. This covers each `400`:
  `GET /reservations/mine`, `GET /reservations/employees`, `GET /reservations/by-employee/{employeeId}`,
  `GET /occupancy`, `POST /offices`, `PATCH /offices/{officeId}/name`, `PATCH /offices/{officeId}/location`,
  `POST /offices/{officeId}/rooms`, `PATCH /offices/{officeId}/rooms/{roomId}/name`, `POST /employees`,
  `GET /admin/users`.
- **FR-003:** The handler-driven `403` on `POST /reservations` and `DELETE /reservations/{reservationId}` MUST
  be declared as an `ErrorResponse` body (FR-001), not `ProblemDetails`.
- **FR-004:** The policy-only `403` on `GET /reservations/employees` and
  `GET /reservations/by-employee/{employeeId}` MUST be declared as an **empty-body** `403`
  (`.Produces(StatusCodes.Status403Forbidden)`), not `ProblemDetails` and not `ErrorResponse`.
- **FR-005:** The `401` on `GET /account/me` MUST be declared as an **empty-body** `401`
  (`.Produces(StatusCodes.Status401Unauthorized)`), not `ProblemDetails`.
- **FR-006:** After this slice, `.ProducesProblem(...)` MUST NOT appear in any endpoint of `attendance-api`,
  `organization-api`, or `identity-api` (every current use is reclassified by FR-002–FR-005).
- **FR-007:** No route, status code, or response **body** MAY change. The server's emitted bytes for every
  status are identical to today; only the OpenAPI metadata (and therefore the generated client types) changes.
- **FR-008:** The three committed OpenAPI documents (`backend/apps/{identity,organization,attendance}-api/*.json`)
  MUST be re-emitted and the typed Angular clients regenerated; both drift gates (ADR-0036) MUST be green, and
  any document no longer referencing `ProblemDetails` MUST no longer carry that component schema.

### Non-functional

- **NFR-001:** `web-http` MUST stay free of any `Domain`/`Application`/`Infrastructure` dependency; the helper
  references only `ErrorResponse` and ASP.NET types, so the architecture layer rules continue to skip it
  (ADR-0046).
- **NFR-002:** All existing quality gates stay green (`dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, the architecture tests, and `pnpm nx affected -t lint test build`),
  including both `git diff --exit-code` codegen drift gates.

## Test-first plan (Red → Green)

- Contract (document) assertions over the emitted OpenAPI per host: for every status in FR-002/FR-003 the
  operation's response content is `application/json` → `ErrorResponse`; for FR-004/FR-005 the response declares
  no content; and no operation in any of the three documents declares `application/problem+json`. These fail
  first against today's `ProblemDetails` declarations.
- Integration (regression, real stack, ADR-0052): the existing tests asserting the actual `400`/`403` bodies
  (`{ code, message }`) and the empty policy-`403` / `/account/me` `401` bodies stay green unchanged — they are
  the contract that the wire bytes did not move.
- Drift: re-emit specs + `generate-client`; both `git diff --exit-code` gates green (ADR-0036).

## Out of scope

- Statuses that endpoints *can* emit but do **not** currently declare are not added here — e.g. the
  `ErrorResponse` `401` that `POST /reservations`, `DELETE /reservations/{reservationId}`, and
  `GET /reservations/mine` return when the subject claim is missing. Declaring currently-undeclared statuses is
  a separate slice.
- Any change to the error **bodies** themselves, to `ErrorResults`/`ArgumentExceptionHandler`, or to the
  `Error` → status mapping (ADR-0046) — this slice only corrects the published metadata.
- Removing `ProblemDetails` from the framework's default `500`/fallback handling beyond dropping a document's
  now-unreferenced schema.

## Review & Acceptance Checklist

- [ ] Every functional requirement has a test written before its implementation
- [ ] Each `400` and each handler-driven `403` is typed as `ErrorResponse` in the spec
- [ ] The policy-only `403`s and the `/account/me` `401` are typed as empty-body
- [ ] No `.ProducesProblem(...)` remains in any host endpoint
- [ ] Server wire bytes unchanged; specs re-emitted and Angular clients regenerated; both drift gates green
- [ ] `web-http` takes no domain dependency; all gates green; no suppressions
