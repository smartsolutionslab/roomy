# Feature Specification: One read-dispatch pattern across every host

**Feature Branch:** `refactor/031-consistent-read-dispatch`
**Status:** Draft
**Created:** 2026-06-27
**Updated:** 2026-06-27
**Realizes:** (pending) an ADR amending **ADR-0051** — read dispatch at the API edge goes through
`IQueryHandler<…>` for every context, not just attendance.

## Summary

A behaviour-preserving backend de-duplication of *how a read reaches its data*. Today the three hosts
disagree. Attendance routes every read through an `IQueryHandler<…>` (the `Queries/` + `Queries/Handlers/`
split of ADR-0051): `ReservationEndpoints`, `OccupancyEndpoints`, and `RoomCatalogueEndpoints` all inject
`IQueryHandler<…>` and never see a repository. Identity and organization skip the query side entirely —
their read endpoints inject the **domain repository** straight into the endpoint and call it: `AdminUserEndpoints`
(`users.GetPageAsync`, `users.GetByIdentifierAsync`), `AccountEndpoints` (`users.GetByKeycloakSubjectAsync`),
and `OfficeEndpoints` (`offices.GetAllAsync`, `offices.GetByIdentifierAsync`). Two of three hosts bypass the
read use-case layer.

This slice picks **one** pattern and applies it uniformly. The chosen direction (see the ADR decision below)
is to route identity and organization reads through `IQueryHandler<…>` for symmetry with attendance and
ADR-0051 — every migrated read gains an `IQuery` message, an `IQueryHandler`, and a view/result record under
`Queries/` and `Queries/Handlers/`, and the endpoint stops taking a repository for reads. No route, status
code, response body, or OpenAPI schema changes — the wire contract is untouched, so there is no client
regeneration.

## Decision required first (ADR)

This changes a **cross-cutting pattern** — where reads are dispatched at the edge of every context — so it
requires an ADR **before** any code, recorded as an amendment to ADR-0051 (which today states identity and
organization "have no `Queries/` folder"). The ADR must choose between:

- **Option A — Route all reads through `IQueryHandler<…>` (recommended).** Identity and organization grow a
  `Queries/` + `Queries/Handlers/` structure; their read endpoints inject `IQueryHandler<…>` exactly as
  attendance does. Consistency across all three hosts, a single testable read seam, and the read/write split
  visible in every context's tree. Cost: one query + handler + view record per migrated read, plus DI
  registration. This is the direction the rest of this spec assumes.
- **Option B — Formally document direct-repository reads as an accepted exception.** Keep identity and
  organization calling the repository from the endpoint, and amend ADR-0051 to sanction direct-repository
  reads for trivial lookups, defining the boundary (e.g. single-aggregate fetch with no projection) and
  noting attendance's query handlers are kept where a read crosses a read model. Lower churn; the cost is a
  permanent two-pattern codebase and a read seam that cannot be unit-tested in isolation in two hosts.

**Recommendation: Option A.** It removes the inconsistency rather than codifying it, matches the existing
ADR-0051 direction, and makes every host's reads unit-testable at the handler. The functional requirements
below specify Option A; if the ADR lands on Option B, this spec is superseded by that ADR and re-scoped to a
documentation-only change.

## User Scenarios & Testing

### Primary story

As a maintainer, I want every host to dispatch reads the same way — through a query handler — so that the
read seam is uniform, unit-testable, and cannot drift per context, and so a reader sees the same shape in
identity, organization, and attendance.

### Acceptance Scenarios

1. **Every read endpoint dispatches through a query handler**
   - GIVEN the identity, organization, and attendance hosts
   - WHEN their read (`MapGet`) endpoints are inspected
   - THEN each one injects an `IQueryHandler<…>` and dispatches a query; **no read endpoint injects a domain
     repository** (`IUserRepository`, `IOfficeRepository`) to fetch its data.

2. **Identity reads migrated**
   - GIVEN the identity host
   - THEN `ListUsers`, `GetUser` (`AdminUserEndpoints`), and `GetCurrentAccount` (`AccountEndpoints`) each
     dispatch a query — backed by `users.GetPageAsync`, `users.GetByIdentifierAsync`, and
     `users.GetByKeycloakSubjectAsync` respectively inside the handler — and return the same response shapes
     as today.

3. **Organization reads migrated**
   - GIVEN the organization host
   - THEN `ListOffices` and `GetOffice` (`OfficeEndpoints`) each dispatch a query — backed by
     `offices.GetAllAsync` and `offices.GetByIdentifierAsync` inside the handler — and return the same
     response shapes as today.

4. **Each migrated read has a query + handler + unit tests**
   - GIVEN each newly introduced query
   - THEN it has an `IQuery` message and a result/view record under `Queries/`, an `IQueryHandler` under
     `Queries/Handlers/`, and unit tests (NSubstitute doubles, Shouldly, ADR-0052) covering its success path
     and its not-found/empty path where the underlying repository call can miss.

5. **CQRS folder structure present in identity and organization**
   - GIVEN the identity and organization `application` libraries
   - THEN each has a `Queries/` folder and a `Queries/Handlers/` subfolder with the namespaces
     `…Application.Queries` and `…Application.Queries.Handlers` (ADR-0051), registered in that context's
     `*InfrastructureServiceCollectionExtensions`.

6. **HTTP behaviour is unchanged (regression)**
   - WHEN the user-list, user-by-id, account-me, office-list, and office-by-id endpoints are exercised over
     the real stack as in the existing integration tests
   - THEN every status code and body matches today's behaviour — including `404` on a missing user/office,
     `401` on `GetCurrentAccount` with no subject, and the keyset page shape of `ListUsers`.

### Edge cases

- A missing user / office → the query handler surfaces `Error.NotFound` and the endpoint returns `404`,
  exactly as the direct repository call does today (repositories return `Result<T>`, never `T?`).
- `GetCurrentAccount` with no resolvable subject → still `401` (the subject read stays at the edge; only the
  user fetch moves into the handler).
- An empty `ListOffices` / `ListUsers` result → the query returns the empty collection / empty page; status
  and body are unchanged.

## Requirements

### Functional

- **FR-001:** An ADR amending ADR-0051 MUST be merged **before** implementation, recording the chosen read-
  dispatch pattern (Option A or B) and the rationale. FR-002…FR-008 assume Option A.
- **FR-002:** Identity `application` MUST gain queries + handlers for the three identity reads: a paged
  user-list query (over `IUserRepository.GetPageAsync`), a user-by-identifier query (over
  `GetByIdentifierAsync`, returning `Error.NotFound` on miss), and a current-account query (over
  `GetByKeycloakSubjectAsync`, returning `Error.NotFound` on miss). Each returns the same view/result shape
  the endpoint maps to its response today.
- **FR-003:** Organization `application` MUST gain queries + handlers for the two office reads: an office-list
  query (over `IOfficeRepository.GetAllAsync`) and an office-by-identifier query (over `GetByIdentifierAsync`,
  returning `Error.NotFound` on miss).
- **FR-004:** `AdminUserEndpoints`, `AccountEndpoints`, and `OfficeEndpoints` MUST inject `IQueryHandler<…>`
  for their read endpoints and MUST stop injecting `IUserRepository` / `IOfficeRepository` for reads. A read
  endpoint MUST NOT take a domain repository parameter.
- **FR-005:** The new queries, handlers, and view/result records MUST live under `Queries/` and
  `Queries/Handlers/` with the ADR-0051 namespaces, and MUST be registered in each context's
  `*InfrastructureServiceCollectionExtensions`.
- **FR-006:** Each migrated read MUST have a handler unit test written **before** its handler, asserting the
  mapped result on success and `Error.NotFound` / empty on the miss path (ADR-0052: NSubstitute, Shouldly).
- **FR-007:** Attendance MUST be unchanged — it is the reference pattern; this slice only brings identity and
  organization up to it.
- **FR-008:** No route, status code, response body, or OpenAPI schema MAY change; no Angular client
  regeneration is required.

### Non-functional

- **NFR-001:** Handlers MUST NOT reference ASP.NET / `ClaimsPrincipal` (ADR-0005); the subject read for
  `GetCurrentAccount` stays at the edge and the resolved subject identifier is passed into the query.
- **NFR-002:** The dependency rule is preserved — `application` queries depend only on `domain`/`application`;
  the architecture tests stay green.
- **NFR-003:** All existing quality gates stay green (`dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, the architecture tests, and `pnpm nx affected -t lint`).

## Test-first plan (Red → Green)

- Unit (`identity/application`): paged user-list handler maps the page; user-by-id handler returns the view
  on hit and `Error.NotFound` on miss; current-account handler returns the account view on hit and
  `Error.NotFound` on miss. (NSubstitute repository doubles, ADR-0052.)
- Unit (`organization/application`): office-list handler maps all offices (incl. empty); office-by-id handler
  returns the view on hit and `Error.NotFound` on miss.
- Integration (regression, real stack): the existing user-list / user-by-id / account-me / office-list /
  office-by-id tests stay green unchanged — they are the contract that behaviour did not move.

## Out of scope

- The **read-after-write** repository fetches inside command flows (`CreateOffice`, `AddRoom`, `RenameOffice`,
  …, which re-load the office to build the 200/201 body). Those are part of the command path, not a read
  endpoint; they keep their repository injection and are not migrated here.
- Any change to repository interfaces, query parameters, paging, filtering, or sorting behaviour.
- Attendance endpoints and queries (already the reference pattern).
- Frontend, gateway/BFF, Keycloak, or OpenAPI emit changes.

## Review & Acceptance Checklist

- [ ] ADR amending ADR-0051 is merged before any implementation, with the chosen option and rationale
- [ ] Every functional requirement has a test written before its implementation
- [ ] No read endpoint in any host injects a domain repository to fetch its data
- [ ] Identity and organization have `Queries/` + `Queries/Handlers/` per ADR-0051, registered in DI
- [ ] Each migrated read has a query, handler, and handler unit tests (success + miss/empty)
- [ ] Attendance is untouched; wire contract unchanged; no OpenAPI re-emit, no client regen
- [ ] All gates green; no suppressions
