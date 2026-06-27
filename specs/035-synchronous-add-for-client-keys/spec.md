# Feature Specification: Use synchronous `Add` for client-keyed aggregates

**Feature Branch:** `refactor/035-synchronous-add-for-client-keys`
**Status:** Draft
**Created:** 2026-06-27
**Updated:** 2026-06-27

## Summary

A behaviour-preserving infrastructure tidy-up. Four repositories persist new aggregates with
EF Core's `context.<Set>.AddAsync(entity, cancellationToken)`: `OfficeRepository`,
`EmployeeRepository`, `CompanyRepository` (organization infrastructure) and `UserRepository`
(identity infrastructure). `AddAsync` exists only to await value generators that must hit the
database (e.g. HiLo); every one of these aggregates carries a **client-generated GUIDv7 key**
(`OfficeIdentifier.New()` → `Guid.CreateVersion7()`, and the matching `…Identifier.New()` on each
aggregate), so the synchronous `Add` is EF Core's documented recommendation and avoids a pointless
`Task`/`await`. This slice replaces the `AddAsync` calls with `Add`. No route, status code, response
body, persisted column, or OpenAPI schema changes — the wire contract and the generated client are
untouched.

## User Scenarios & Testing

### Primary story

As a maintainer, I want new aggregates with client-generated keys added through the synchronous
`Add`, so that the persistence code follows EF Core's recommendation and carries no needless async
machinery, with no observable change to behaviour.

### Acceptance Scenarios

1. **No `AddAsync` for client-keyed aggregates**
   - GIVEN `OfficeRepository`, `EmployeeRepository`, `CompanyRepository`, and `UserRepository`
   - THEN none calls `DbSet<T>.AddAsync` — each uses the synchronous `Add`.

2. **Entities persist identically**
   - GIVEN the existing create flows (create office, hire employee, seed company, register user)
   - WHEN the handler adds the aggregate and the unit of work saves
   - THEN the row is persisted with exactly the same key and columns as before the change.

3. **Ports remain satisfied**
   - GIVEN the `IOfficeRepository` / `IEmployeeRepository` / `ICompanyRepository` / `IUserRepository`
     contracts and their handler call sites
   - THEN every repository still satisfies its port and every call site compiles unchanged.

### Edge cases

- A repository whose only `await` was the `AddAsync` MUST still honour its `Task`-returning port:
  it returns a completed task (`Task.CompletedTask`) rather than gaining a needless `async` state
  machine. The cancellation token is no longer consulted (synchronous `Add` does no I/O), matching
  EF Core's behaviour where `AddAsync` only observes the token while running database value
  generators — of which there are none here.

## Requirements

### Functional

- **FR-001:** `OfficeRepository.AddAsync`, `EmployeeRepository.AddAsync`,
  `CompanyRepository.AddAsync`, and `UserRepository.AddAsync` MUST call the synchronous
  `context.<Set>.Add(entity)`; no `DbSet<T>.AddAsync` call may remain in either context's
  infrastructure for these client-keyed aggregates.
- **FR-002:** Each repository method MUST keep a signature compatible with its port. Where
  `AddAsync` was the only `await`, the method MUST drop the `async`/`await` and return
  `Task.CompletedTask` (the `Task AddAsync(…, CancellationToken)` port signature is unchanged).
- **FR-003:** Persistence MUST be unchanged: the same aggregate key (client-generated GUIDv7) and
  the same columns are written, and the change is tracked as `Added` exactly as before.
- **FR-004:** No route, status code, response body, persisted schema, or OpenAPI schema MAY change;
  no Angular client regeneration is required.

### Non-functional

- **NFR-001:** No new dependency, abstraction, or framework reference is introduced; the change is
  confined to the four named repository files.
- **NFR-002:** All existing quality gates stay green (`dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, the architecture tests, and `nx affected` lint). The build's
  nullable/analyzer pass MUST raise no async-without-await warning for the touched methods.

## Test-first plan (Red → Green)

- The existing persistence/integration tests for create-office, hire-employee, company seeding, and
  register-user are the contract that behaviour did not move — they MUST stay green unchanged. Run
  them red against the intended refactor only if a behavioural difference is suspected; otherwise
  they stand as the regression guard.
- If any repository lacks a direct add-then-readback persistence test, add one **before** the edit
  asserting the aggregate is retrievable by its client-generated identifier after save (real stack,
  no mocks per ADR-0052), so the synchronous `Add` is proven equivalent.

## Out of scope

- Any repository whose aggregate uses a database-generated key (none today; if one exists it keeps
  `AddAsync`).
- Changing port signatures from `Task` to `void`/`ValueTask`, or altering handler call sites beyond
  what compilation requires.
- Any change to save semantics, the unit of work, event publication, or transaction handling.

## Review & Acceptance Checklist

- [ ] No `DbSet<T>.AddAsync` remains in the four named repositories
- [ ] Each method still satisfies its port; call sites compile unchanged
- [ ] Aggregates persist with identical keys and columns (regression tests green)
- [ ] Methods reduced to `Task.CompletedTask` raise no async-without-await warning
- [ ] Wire contract unchanged; no OpenAPI re-emit, no client regen
- [ ] All gates green; no suppressions
