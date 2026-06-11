# 0053. Read the current user once at the API edge; one administrator-role constant

- **Status:** Accepted
- **Date:** 2026-06-11
- **Deciders:** Heiko Weiß

## Context and problem statement

Every context API needs two facts about the caller: *who* they are (the Keycloak subject) and
*whether* they are an administrator. Today each endpoint re-derives both by hand, three ways:

- **Subject parse duplicated.** `Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier)
  ?? principal.FindFirstValue("sub"), …)` appears verbatim in `ReservationEndpoints.TryGetSubject`
  (`attendance-api`) and inline in `AccountEndpoints.GetCurrentAccountAsync` (`identity-api`).
- **Administrator role string copied four times.** `private const string AdministratorRole =
  "administrator"` is declared independently in `ReservationEndpoints`, `OfficeEndpoints`,
  `EmployeeEndpoints`, and `AdminUserEndpoints`. A typo in one host silently disables its admin gate.
- **The on-behalf authorization rule lives in the endpoint.** `ReservationEndpoints.MayReserveFor`
  (`employee == actor || principal.IsInRole(AdministratorRole)`) is a business rule — only an
  administrator may act for another employee — but it sits in the HTTP layer where it cannot be
  unit-tested against the `application`, while the sibling `CancelReservation` command already
  carries `ActorIsAdmin` and could enforce the same rule itself.

So the same two reads are open-coded across hosts, the admin role is a stringly-typed literal in
three places, and one authorization rule is stranded in the endpoint instead of the use case that
owns it.

## Decision drivers

- **One definition of each cross-cutting read.** Subject extraction and the admin check are
  identical for every host; they should have one home, like the shared `Error → HTTP` map (ADR-0046).
- **No framework in `application`.** Handlers must not learn about `ClaimsPrincipal`; the edge stays
  responsible for turning claims into the primitives a command already accepts (`Actor`,
  `ActorIsAdmin`). The current command shape is right and stays.
- **Authorization rules belong to the use case.** "Only an administrator may act on behalf of
  another" is a domain authorization rule; it should be enforced and tested where `ReservePlace`
  is handled, consistent with how `CancelReservation` already treats `ActorIsAdmin`.
- **Surgical, behaviour-preserving.** The wire contract and every status code stay identical; this
  removes duplication and relocates one rule, nothing more.

## Considered options

- **A — Leave it.** Lowest churn; the parse stays duplicated, the role stays a literal in three
  hosts, and the on-behalf rule stays untestable.
- **B — Inject an `ICurrentUser` service backed by `IHttpContextAccessor`.** Most "service-like",
  but adds DI plumbing and an accessor registration to every host for a read that minimal APIs
  already bind for free as a `ClaimsPrincipal` parameter — abstraction the codebase does not need yet.
- **C — Shared extension methods on `ClaimsPrincipal` in `web-http`, plus one role constant; move
  the on-behalf rule into the handler (chosen).** Endpoints keep binding `ClaimsPrincipal`
  natively but stop open-coding the reads; the admin literal exists once; the authorization rule
  moves to the use case that owns it.

## Decision

**Option C**, in the existing `backend/libs/web-http` (`SmartSolutionsLab.Roomy.Web.Http`) — already
referenced by all three hosts and deliberately outside the layer taxonomy (ADR-0046).

1. **`CurrentUser` extension methods on `ClaimsPrincipal`:**
   - `Result<Guid> Subject(this ClaimsPrincipal)` — the single `NameIdentifier ?? "sub"` parse,
     returning `Error.Unauthorized` when absent/unparsable (callers map via the existing
     `ToHttpResult()`; the previous `Results.Unauthorized()` 401 is preserved).
   - `bool IsAdministrator(this ClaimsPrincipal)` — `IsInRole(RoomyRoles.Administrator)`.

   Each context still wraps the returned `Guid` into its own typed identifier
   (`KeycloakSubjectIdentifier` in identity, `UserIdentifier` in attendance), so `web-http`
   gains **no** domain dependency.

2. **One role constant:** `public static class RoomyRoles { public const string Administrator =
   "administrator"; }` in `web-http`. The four local `AdministratorRole` constants are deleted;
   endpoint `IsInRole(…)` and policy `RequireRole(…)` registrations reference `RoomyRoles.Administrator`.

3. **The on-behalf rule moves into `ReservePlaceHandler`.** `ReservePlace` gains an `ActorIsAdmin`
   flag (mirroring `CancelReservation`); the handler rejects an on-behalf reservation
   (`Employee != Actor` with a non-admin actor) with `Error.Forbidden("not_authorized", …)`. The
   endpoint stops computing `MayReserveFor` and simply forwards `principal.IsAdministrator()`.
   The `403` response and its message are unchanged.

`web-http`'s dependencies are unchanged (shared-kernel + ASP.NET); it stays unselected by the
architecture layer rules.

## Consequences

**Positive**
- The subject parse and the admin check each have one definition; a role typo can no longer
  silently disable one host's gate.
- The on-behalf authorization rule is unit-tested in `application` alongside `ReservePlace`, no
  longer reachable only through the HTTP layer.
- Endpoints shrink to intent: `principal.Subject()`, `principal.IsAdministrator()`.

**Negative / trade-offs**
- Mechanical churn across the three hosts' endpoints; warnings-as-errors enforces the cutover.
- `ReservePlace` gains a field, so its DI registration and tests update — but it now matches
  `CancelReservation`, removing an asymmetry rather than adding one.
- No wire-contract change: same routes, same status codes, same bodies; no OpenAPI re-emit, no
  Angular client regen.

**Follow-ups**
- The two realm-role flatteners (`KeycloakRealmRoles.AddRoleClaims` for the context APIs vs the
  gateway's `RealmRoleReader` + `FlattenRealmRoles`) parse the same `realm_access.roles` JSON and
  are a separate, smaller auth-claims dedup — out of scope here, noted for a later slice.
- csharp.md and CLAUDE.md record the "read the caller at the edge via `CurrentUser`; one
  `RoomyRoles` constant" convention.
