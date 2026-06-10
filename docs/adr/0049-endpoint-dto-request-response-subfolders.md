# 0049. API-host endpoint DTOs live in Request/ and Response/ subfolders

- **Status:** Accepted
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

Each API host exposes its endpoints from an `Endpoints/` folder (e.g.
`apps/attendance-api/Endpoints/`). The one-type-per-file rule (csharp.md) recently split every
request/response DTO into its own file, which left the `Endpoints/` folder flat and crowded —
`apps/attendance-api/Endpoints/` alone held three endpoint classes mixed with eleven response
records and a request record, with no visual separation between the HTTP *contract* types (the
JSON shapes crossing the wire) and the *routing* code (the endpoint classes that map and handle).

A reader scanning the folder cannot tell the request bodies, the response bodies, and the
endpoint definitions apart, and "where does the response shape for this endpoint live?" has no
predictable answer beyond a name guess.

How should an API host organise its endpoint request/response DTOs so the contract surface is
discoverable and separated from the routing code?

## Decision drivers

- **Discoverability.** The set of response shapes an API emits — and the set of request bodies it
  accepts — should each be a folder you can open, not a name-pattern you grep.
- **Folder = namespace.** The repo already mirrors folders into namespace segments everywhere
  (e.g. `…/Infrastructure/ReadModels/Employees/` → `…ReadModels.Employees`); endpoint DTOs should
  not be the one exception.
- **Contract stability.** The reorganisation must not change the emitted OpenAPI schema (and thus
  the generated Angular client, ADR-0036) — the drift gate must stay green.
- **One type per file stays.** This is an organisation rule layered on top of csharp.md, not a
  relaxation of it.

## Considered options

- **A — Keep DTOs flat in `Endpoints/`.** Lowest churn, but the folder stays a mix of routing and
  contract types with no separation.
- **B — Subfolders, flat namespace.** Move the files into `Endpoints/Response/` and
  `Endpoints/Request/` but keep the `…Endpoints` namespace. No `using` churn, but it breaks the
  folder=namespace convention used everywhere else.
- **C — Subfolders with matching sub-namespaces (chosen).** Move the DTOs into `Response/` and
  `Request/` subfolders with namespaces `…Endpoints.Response` and `…Endpoints.Request`; the
  endpoint classes gain a `using` for the sub-namespace(s) they reference.

## Decision

**Option C.** In every API host:

1. Response-body DTOs — the `*Response` records **and** their `*Page` keyset-pagination wrappers —
   live in `Endpoints/Response/`, namespace `…Api.Endpoints.Response`.
2. Request-body DTOs — the `*Request` records — live in `Endpoints/Request/`, namespace
   `…Api.Endpoints.Request`. A host with no request bodies (identity-api) has no `Request/` folder.
3. The endpoint classes stay in `Endpoints/` and add `using …Endpoints.Response;` /
   `using …Endpoints.Request;` as needed.
4. The same split applies to the gateway BFF endpoint area: its `whoami` response (`CurrentUser`)
   moves to `apps/gateway/Bff/Response/`, namespace `…Gateway.Bff.Response`.

The OpenAPI schema name is the simple type name, independent of namespace, so the emitted spec and
the generated client are unchanged — verified by the existing drift gate and the
`#/components/schemas/AccountResponse` assertion in `OpenApiDocumentTests`.

## Consequences

**Positive**
- The contract surface is two folders you can open: every response shape under `Response/`, every
  request body under `Request/`, routing code alone in `Endpoints/`.
- Folder=namespace stays consistent across the whole codebase.
- One-type-per-file is unchanged; this only relocates the files.

**Negative / trade-offs**
- Endpoint classes (and integration tests that deserialize public DTOs) carry an extra `using` for
  the sub-namespace — minor, and the build enforces it.
- Nested, `private` DTOs (e.g. `BffTokenRefresher.TokenResponse`) are implementation details, not
  endpoint contract types, and stay where they are — the rule is about top-level contract DTOs.

**Follow-ups**
- csharp.md and CLAUDE.md record the rule; new API endpoints place their DTOs accordingly.
