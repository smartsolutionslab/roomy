# Feature Specification: Read the current user once at the API edge

**Feature Branch:** `refactor/014-current-user-edge`
**Status:** Draft
**Created:** 2026-06-11
**Updated:** 2026-06-11
**Realizes:** ADR-0053 (current-user edge helpers; one administrator-role constant; on-behalf rule
moves into the handler)

## Summary

A behaviour-preserving backend de-duplication. Today each context API re-derives *who the caller
is* (Keycloak subject) and *whether they are an administrator* by hand: the `NameIdentifier ?? "sub"`
parse is open-coded in two hosts, the `"administrator"` role string is a literal in four endpoint
classes, and the
"only an administrator may reserve on behalf of another" rule sits in the attendance endpoint where
it cannot be unit-tested. This slice introduces shared `CurrentUser` extensions and one `RoomyRoles`
constant in `libs/web-http`, and moves the on-behalf authorization rule into `ReservePlaceHandler`.
No route, status code, or response body changes — the wire contract and the OpenAPI specs are
untouched, so no client regeneration.

## User Scenarios & Testing

### Primary story
As a maintainer, I want the caller's identity and admin status read in one shared place and the
on-behalf rule enforced in the use case, so that the gates cannot drift between hosts and the rule
is tested where it lives.

### Acceptance Scenarios

1. **Subject parse, one definition**
   - GIVEN a `ClaimsPrincipal` carrying a `sub` (or `NameIdentifier`) GUID claim
   - WHEN `principal.Subject()` is called
   - THEN it returns `Result<Guid>` success with that GUID; with neither claim present (or
     unparsable) it returns `Error.Unauthorized`.

2. **Admin check, one definition**
   - GIVEN a principal with / without the `administrator` role claim
   - WHEN `principal.IsAdministrator()` is called
   - THEN it returns `true` / `false`, resolving the role name from `RoomyRoles.Administrator`.

3. **Hosts use the shared reads**
   - GIVEN the identity, organization, and attendance hosts
   - THEN no endpoint declares a local `AdministratorRole` constant, and the
     `NameIdentifier ?? "sub"` parse appears nowhere outside `CurrentUser`.

4. **On-behalf rule enforced in the handler**
   - GIVEN a non-administrator actor and a `ReservePlace` whose `Employee` differs from its `Actor`
   - WHEN the command is handled
   - THEN it fails with `Error.Forbidden("not_authorized", …)` and **no** `AttendanceDay` is loaded
     or saved.

5. **Self-service and admin on-behalf still succeed**
   - GIVEN an actor reserving for themselves (`Employee == Actor`), OR an administrator actor
     reserving for another employee
   - WHEN the command is handled
   - THEN the reservation proceeds exactly as before.

6. **HTTP behaviour is unchanged (regression)**
   - WHEN the reserve / cancel / account / employee-list endpoints are exercised over the real
     stack as in the existing integration tests
   - THEN every status code and body matches today's behaviour: missing subject → `401`,
     non-admin on-behalf / non-admin employee-list / non-admin by-employee → `403` with body
     `{ "code": "not_authorized", … }`.

### Edge cases
- A principal with a present-but-empty / non-GUID subject → `Subject()` is `Error.Unauthorized`
  (endpoint returns `401`), matching today's `Guid.TryParse` failure path.
- A duplicate `administrator` role claim → `IsAdministrator()` is still `true` (no throw).

## Requirements

### Functional
- **FR-001:** `web-http` MUST expose `Result<Guid> Subject(this ClaimsPrincipal)` performing the
  single `NameIdentifier ?? "sub"` GUID parse, returning `Error.Unauthorized` on absence/parse
  failure.
- **FR-002:** `web-http` MUST expose `bool IsAdministrator(this ClaimsPrincipal)` and a single
  `RoomyRoles.Administrator` constant (`"administrator"`).
- **FR-003:** `identity-api`, `organization-api`, and `attendance-api` MUST consume FR-001/FR-002;
  the four local `AdministratorRole` constants (`ReservationEndpoints`, `OfficeEndpoints`,
  `EmployeeEndpoints`, `AdminUserEndpoints`) and both inline subject parses MUST be removed. Each host
  MUST wrap the returned `Guid` into its own identifier type — `web-http` MUST NOT take a domain
  dependency.
- **FR-004:** `ReservePlace` MUST carry `ActorIsAdmin`; `ReservePlaceHandler` MUST reject an
  on-behalf reservation by a non-administrator (`Employee != Actor && !ActorIsAdmin`) with
  `Error.Forbidden("not_authorized", …)` before touching the repository. `ReservationEndpoints`
  MUST stop computing `MayReserveFor` and forward `principal.IsAdministrator()`.
- **FR-005:** No route, status code, response body, or OpenAPI schema MAY change; no Angular client
  regeneration is required.

### Non-functional
- **NFR-001:** Handlers MUST NOT reference `ClaimsPrincipal` or any ASP.NET type (ADR-0005); the
  edge converts claims to the primitives the command already accepts.
- **NFR-002:** All existing quality gates stay green (`dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, the architecture tests, and `nx affected` lint).

## Test-first plan (Red → Green)
- Unit: `Subject()` success / both-claims-absent / unparsable; `IsAdministrator()` true / false /
  duplicate-claim. (`web-http` test project.)
- Unit (`attendance/application`): `ReservePlaceHandler` rejects non-admin on-behalf without
  loading/saving (NSubstitute doubles, ADR-0052); admin on-behalf and self-service still succeed.
- Integration (regression, real stack): the existing reserve/cancel/account/employee-list tests
  stay green unchanged — they are the contract that behaviour did not move.

## Out of scope
- An injected `ICurrentUser` service / `IHttpContextAccessor` (rejected in ADR-0053).
- De-duplicating the two realm-role flatteners (`KeycloakRealmRoles` vs gateway `RealmRoleReader`)
  — a separate slice.
- Any change to roles themselves, Keycloak config, or the BFF session.

## Review & Acceptance Checklist
- [ ] Every functional requirement has a test written before its implementation
- [ ] No local `AdministratorRole` constant or inline subject parse remains in any host
- [ ] On-behalf rule is enforced and tested in `application`, not the endpoint
- [ ] `web-http` takes no domain dependency
- [ ] Wire contract unchanged; no OpenAPI re-emit, no client regen
- [ ] All gates green; no suppressions
