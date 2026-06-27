# Feature Specification: One canonical error-code taxonomy across all contexts

**Feature Branch:** `034-error-code-taxonomy`
**Status:** Draft
**Created:** 2026-06-27
**Realizes:** a new ADR (next free number, e.g. ADR-0059) — *or* an amendment to ADR-0046 — that
records the canonical error-code scheme. The ADR MUST be accepted **before** any code in this
slice is written, because this changes the codes on the wire (a contract change).

## Summary

A behaviour-adjacent contract de-duplication. The string `code` on every `ErrorResponse` body — the
stable contract the gateway and the typed Angular client key off to choose a user-facing message —
follows **two incompatible conventions** today:

- **Attendance** emits **bare snake_case** with no prefix: `not_bookable`, `already_reserved_today`,
  `room_full`, `reservation_not_found`, `past_immutable`, `not_owner`, `not_authorized`,
  `unknown_office`, `unknown_room`, `unknown_employee`, `concurrency_conflict`,
  `concurrency_retry_exhausted`.
- **Organization** emits **dotted, prefixed** codes — but prefixed by *aggregate*, not context:
  `office.name_taken`, `office.room_name_taken`, `office.room_not_found`, `office.not_found`,
  `employee.terminal`, `employee.not_found`, `company.not_seeded`.
- **Identity** is **internally inconsistent**: dotted `user.not_found` / `user.not_active` alongside
  bare `email_taken` / `password_rejected`.
- The shared edge (`web-http` `CurrentUser`) emits bare `no_subject` / `no_user_id`.

So a client cannot rely on a prefix to route a code to a context, and the codes are scattered as inline
`Error.*` string literals across aggregates, handlers and infrastructure (only organization's two
`office.*` name-clash codes are centralised, in `OfficeErrors`). This slice picks **one** scheme,
aligns every emitted code to it, centralises each context's codes into one catalog type, and moves the
OpenAPI examples / Angular mapping / tests in lockstep so no old code survives anywhere.

Because the `code` value is part of the published HTTP contract, this is **not** behaviour-preserving:
every changed code must move on the wire, in the OpenAPI error examples, in the generated/typed Angular
client mapping, and in every test that asserts a code — together, in this one slice.

## The canonical scheme (the ADR decision)

The ADR MUST record exactly this format; the recommendation below is what this spec is written against:

- **Format:** `<context>.<reason>` — a single dot separating a context prefix from a reason.
- **Prefix:** the bounded context that owns the failure, one of exactly `attendance.`,
  `organization.`, `identity.`. Cross-cutting failures emitted by the shared edge (`web-http`) are not
  owned by a context and take the one reserved non-context prefix `auth.` (`auth.no_subject`,
  `auth.no_user_id`).
- **Reason:** `snake_case`, lower-case ASCII, no further dots; where an aggregate name is needed to
  disambiguate it folds into the reason (`organization.office_name_taken` vs
  `organization.room_name_taken`), so a code is always exactly two dot-separated segments.
- **Casing/charset:** `^[a-z]+\.[a-z0-9_]+$` — the whole code matches this regex.

> The aggregate-prefixed alternative (`office.*`, `employee.*`, keeping organization as-is and moving
> attendance to `reservation.*`) was considered and is **rejected** in the ADR: the client routes by
> context, an aggregate is an internal detail, and several reasons (concurrency, unknown_*) have no
> single aggregate. The ADR records this rejection.

### Code inventory (old → new) — the contract delta

| Context | Old code | New code |
|---|---|---|
| attendance | `not_bookable` | `attendance.not_bookable` |
| attendance | `already_reserved_today` | `attendance.already_reserved_today` |
| attendance | `room_full` | `attendance.room_full` |
| attendance | `reservation_not_found` | `attendance.reservation_not_found` |
| attendance | `past_immutable` | `attendance.past_immutable` |
| attendance | `not_owner` | `attendance.not_owner` |
| attendance | `not_authorized` | `attendance.not_authorized` |
| attendance | `unknown_office` | `attendance.unknown_office` |
| attendance | `unknown_room` | `attendance.unknown_room` |
| attendance | `unknown_employee` | `attendance.unknown_employee` |
| attendance | `concurrency_conflict` | `attendance.concurrency_conflict` |
| attendance | `concurrency_retry_exhausted` | `attendance.concurrency_retry_exhausted` |
| organization | `office.name_taken` | `organization.office_name_taken` |
| organization | `office.room_name_taken` | `organization.room_name_taken` |
| organization | `office.room_not_found` | `organization.room_not_found` |
| organization | `office.not_found` | `organization.office_not_found` |
| organization | `employee.terminal` | `organization.employee_terminal` |
| organization | `employee.not_found` | `organization.employee_not_found` |
| organization | `company.not_seeded` | `organization.company_not_seeded` |
| identity | `user.not_found` | `identity.user_not_found` |
| identity | `user.not_active` | `identity.user_not_active` |
| identity | `email_taken` | `identity.email_taken` |
| identity | `password_rejected` | `identity.password_rejected` |
| shared edge | `no_subject` | `auth.no_subject` |
| shared edge | `no_user_id` | `auth.no_user_id` |

> This table is the authoritative delta. If a code is emitted in production but missing here, the
> taxonomy is incomplete — surface it before implementing rather than guessing a prefix.

## User Scenarios & Testing

### Primary story

As a maintainer, I want every error `code` on the wire to follow one documented `context.reason` shape,
defined once per context, so that the gateway and the Angular client can route a failure by its prefix
and the contract cannot drift between services.

### Acceptance Scenarios

1. **Every emitted code conforms to the scheme**
   - GIVEN any production `Error` raised in a `domain`, `application`, or `infrastructure` library, or at
     the `web-http` edge
   - WHEN its `Code` is inspected
   - THEN it matches `^[a-z]+\.[a-z0-9_]+$` and its prefix is one of `attendance`, `organization`,
     `identity`, or `auth`.

2. **The mapping moves on the wire (attendance)**
   - GIVEN the reserve / cancel endpoints exercised over the real stack as in the existing integration
     tests
   - WHEN a rule fails (room full, already reserved, not bookable, past immutable, not owner, not
     authorized, unknown room, retries exhausted)
   - THEN the `ErrorResponse.code` is the new `attendance.*` value from the table, and the HTTP status
     is unchanged (the status table in ADR-0046 still owns status; only `code` changes).

3. **The mapping moves on the wire (organization & identity)**
   - GIVEN the office/room and account/admin endpoints over the real stack
   - WHEN a name clash, missing office/room, terminal employee, not-active grant, taken email, or
     rejected password occurs
   - THEN the `code` is the new `organization.*` / `identity.*` value and the status is unchanged.

4. **Codes are centralised per context — no scattered literals**
   - GIVEN the attendance, organization, and identity libraries
   - THEN no inline `Error.Validation/Conflict/NotFound/Forbidden/Unauthorized("…", …)` literal code
     string remains in an aggregate, handler, repository, read model, or provider; each code is produced
     from a single per-context catalog (e.g. `AttendanceErrors`, the existing `OfficeErrors` extended or
     folded into an organization catalog, `IdentityErrors`), mirroring how `OfficeErrors` already
     centralises the two name-clash codes.

5. **The Angular client mapping moves in lockstep**
   - GIVEN the typed client's code→message switches (`reserve-page.ts`, `cancel-error-key.ts`,
     `occupancy-page.ts`, and any other `errorCode(...)` consumer)
   - WHEN a backend code arrives
   - THEN it is matched against the new `attendance.*` values and resolves to the same Transloco
     message keys as today; no `case` keeps an old bare code.

6. **No orphaned old code anywhere**
   - GIVEN the whole repo (backend, OpenAPI specs/examples, frontend, tests)
   - WHEN searched for any old code string from the inventory table
   - THEN there are zero matches outside this spec and the ADR.

### Edge cases

- A code reached only by the optimistic-write retry loop (`concurrency_conflict`, internal) still moves
  to `attendance.concurrency_conflict` even though it rarely surfaces, so the catalog stays complete.
- The shared edge codes (`auth.no_subject`, `auth.no_user_id`) live in `web-http`, which by ADR-0046
  carries no domain dependency — they get the reserved `auth.` prefix, not a context prefix, and stay in
  `web-http`.
- Test-fixture codes that are illustrative and never cross the wire (e.g. arbitrary codes in
  `shared-kernel` `Result` unit tests) are not part of the contract and are out of scope; only tests
  asserting a *production* code from the inventory move.

## Requirements

### Functional

- **FR-001:** An accepted ADR MUST define the canonical scheme exactly as in *The canonical scheme*
  above (format `<context>.<reason>`, prefixes `attendance|organization|identity|auth`, reason
  `snake_case`, regex `^[a-z]+\.[a-z0-9_]+$`) and record the rejection of the aggregate-prefixed
  alternative. No production code in this slice may merge before that ADR is Accepted.
- **FR-002:** Every production error code MUST be migrated to its new value per the inventory table.
  Attendance's twelve bare codes, organization's seven aggregate-prefixed codes, identity's four mixed
  codes, and the two shared-edge codes all conform afterward.
- **FR-003:** Each context MUST expose **one** error catalog as the single source of its codes
  (`AttendanceErrors` for attendance; `OfficeErrors` extended — or a single organization catalog — to
  also cover `office_not_found`, `room_not_found`, `employee_terminal`, `employee_not_found`,
  `company_not_seeded`; `IdentityErrors` for identity). After this slice, **no** inline `Error.*` call
  with a literal code string remains in any aggregate, handler, repository, read model, or external
  provider in those contexts.
- **FR-004:** The shared-edge codes MUST become `auth.no_subject` / `auth.no_user_id` in
  `web-http`/`CurrentUser`, keeping `web-http` free of any domain dependency (ADR-0046).
- **FR-005:** HTTP status codes and response bodies' *shape* MUST NOT change — only the `code` string
  value changes. The `Error`→HTTP mapping (ADR-0046) is untouched.
- **FR-006:** The committed OpenAPI specs and any error `code` examples MUST be re-emitted to the new
  values, and the typed Angular client regenerated if the spec output changes, so the spec, the client,
  and the runtime contract agree.
- **FR-007:** The Angular code→message mapping (`errorCode(...)` consumers) MUST be updated to the new
  `attendance.*` codes, preserving every existing Transloco message key; no consumer keeps an old code.
- **FR-008:** No old code from the inventory table may remain anywhere in the repo (backend,
  OpenAPI/examples, frontend, tests) except in this spec and the ADR.

### Non-functional

- **NFR-001:** `domain`/`application` MUST stay framework-free; catalogs return `Error` from
  `shared-kernel` and take no infrastructure dependency (ADR-0005).
- **NFR-002:** Cross-context isolation is preserved — each catalog lives in its own context; `web-http`
  owns only the `auth.*` edge codes (ADR-0031/0046).
- **NFR-003:** All quality gates stay green: `dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, the architecture tests, and `pnpm nx affected -t lint test build`
  — with no suppressions, skips, or deleted tests.

## Test-first plan (Red → Green)

- **Conformance test (new, backend):** a test that enumerates the production codes (via the catalogs)
  and asserts each matches `^[a-z]+\.[a-z0-9_]+$` with an allowed prefix — written first, red until the
  codes move. This becomes the standing guard against future drift.
- **Unit (catalogs):** each catalog factory returns the exact new code + the existing message and kind
  (NSubstitute not needed; pure `Error` assertions with Shouldly, ADR-0052).
- **Unit (domain/application):** existing `AttendanceDay`, `ReservePlaceHandler`, `Employee`,
  `Office`, and `GrantAdministrator` tests update their expected codes to the new values (red first).
- **Integration (real stack):** existing reserve/cancel/office/room/account/admin endpoint tests assert
  the new `code` on each failure path; status assertions stay unchanged.
- **Frontend:** `gateway-error.spec.ts`, `reserve-page.spec.ts`, `cancel-error-key` and occupancy tests
  assert the new `attendance.*` codes and the same rendered messages.
- **Repo guard:** a search-based check (or the conformance test plus a frontend lint) confirms no old
  inventory code string survives (FR-008).

## Out of scope

- Changing any HTTP **status** code or the `ErrorResponse` body shape (owned by ADR-0046) — only the
  `code` value changes.
- Adding new error conditions, messages, or Transloco keys; messages and kinds are preserved verbatim.
- Localising or restructuring user-facing messages.
- Reworking the optimistic-concurrency retry mechanism (ADR-0055); only its codes are renamed.
- Test-only / illustrative codes that never cross the wire (e.g. `shared-kernel` `Result` fixtures).

## Review & Acceptance Checklist

- [ ] ADR recording the canonical scheme is Accepted before any implementation merges
- [ ] Every functional requirement has a test written before its implementation
- [ ] The conformance test enumerates production codes and enforces the regex + prefix set
- [ ] Every emitted code matches the inventory table; no code is left bare or aggregate-prefixed
- [ ] Each context exposes one catalog; no inline literal-code `Error.*` calls remain
- [ ] `web-http` owns only the `auth.*` edge codes and keeps no domain dependency
- [ ] OpenAPI specs/examples re-emitted and the typed client regenerated if output changed
- [ ] Angular code→message mapping and all asserting tests moved in lockstep
- [ ] No old code string survives anywhere outside this spec and the ADR
- [ ] All gates green; no suppressions or skipped tests
