# Feature Specification: De-duplicate branded-identifier and required-text value-object mechanics

**Feature Branch:** `refactor/029-branded-type-and-text-vo-boilerplate`
**Status:** Draft
**Created:** 2026-06-27
**Updated:** 2026-06-27
**Depends on:** a new ADR (to be authored *before* any implementation — golden rule 4) recording the
decision to add a source generator for branded identifiers and a shared required-text helper, including
the trade-off against the project's "simplicity first / no over-abstraction" stance.

## Summary

A behaviour-preserving backend de-duplication of *mechanics*, not of *types*. Two patterns are copied
near-verbatim across the domain libraries:

- **~13 GUID branded identifiers** (`OfficeIdentifier`, `CompanyIdentifier`, `UserIdentifier`,
  `EmployeeIdentifier`, `RoomIdentifier`, `ReservationIdentifier`, `KeycloakSubjectIdentifier`, … across
  the `identity`, `organization`, and `attendance` contexts) are byte-identical `readonly record struct`s
  that differ only by **type name** and the **error-message text**. The recent `…Id` → `…Identifier`
  rename had to touch every one of them in lockstep — the churn cost of the duplication made visible.
- **~6 required-trimmed-text value objects** (`DisplayName`, `EmployeeName`, `OfficeName`, `RoomName`,
  `CompanyName`, `Location`) share an identical `string.IsNullOrWhiteSpace(value) ? null : new(value.Trim())`
  normalization body, differing only by **type name** and **error-message text**.

The types **must stay distinct and per-context** — context isolation and ADR-0031 forbid collapsing a
`CompanyIdentifier` in `attendance` and one in `organization` into a single shared type; the same applies
to the text VOs and their distinct names/messages. This slice de-duplicates only the **repeated
mechanics**:

1. a small **source generator** in `shared-kernel` (e.g. a `[GuidIdentifier]` attribute on a `partial
   readonly record struct`) that emits the `New`/`From`/`TryParse`/two implicit `Guid` operators/`ToString`
   and the empty-GUID guard, so each identifier declaration shrinks to its name (and, where it differs,
   its message), and
2. a shared **`RequiredText.TryNormalize(string?) → string?`** helper in `shared-kernel` that each text VO's
   `TryParse` calls, while each type keeps its own distinct `From` error message.

Each generated/refactored type keeps an identical public surface and runtime behaviour — same equality,
same implicit conversions, same validation, same exception messages. `domain` already depends on
`shared-kernel`, so the new attribute/helper introduce no disallowed dependency. No wire contract, route,
persistence mapping, or OpenAPI schema changes; no client regeneration.

## User Scenarios & Testing

### Primary story

As a maintainer, I want the identical identifier and text-VO mechanics generated/shared from one place,
so that a future change to the pattern (like the `…Id` → `…Identifier` rename) is made once instead of in
~19 hand-copied files, while every type remains its own distinct, per-context value object.

### Acceptance Scenarios

1. **Identifier public surface is preserved**
   - GIVEN any refactored identifier (e.g. `OfficeIdentifier`, `UserIdentifier`, `CompanyIdentifier`)
   - WHEN its members are exercised
   - THEN `New()` yields a non-empty GUIDv7; `From(guid)` returns the value; `TryParse(guid)` returns the
     value or `null` for `Guid.Empty`; both implicit operators (`to Guid`, `from Guid`) behave as before;
     `ToString()` equals the GUID's string; and record-struct equality is unchanged.

2. **Identifier empty-guard message is preserved per type**
   - GIVEN `From(Guid.Empty)` on each identifier type
   - WHEN it throws
   - THEN it throws `ArgumentException` whose message is exactly `"{TypeName} must not be empty."` for that
     type (e.g. `"OfficeIdentifier must not be empty."`), with parameter name `value`.

3. **Text VO normalization is preserved**
   - GIVEN any refactored text VO (`DisplayName`, `EmployeeName`, `OfficeName`, `RoomName`, `CompanyName`,
     `Location`)
   - WHEN `TryParse` is called with `null`, empty, or whitespace-only input
   - THEN it returns `null`; with surrounding whitespace it returns a value whose `Value` is the trimmed
     input; `ToString()` returns `Value`.

4. **Text VO error messages stay distinct**
   - GIVEN `From` with blank input on each text VO
   - WHEN it throws
   - THEN it throws `ArgumentException` with that type's own message (e.g. `"DisplayName must not be
     blank."`, `"Location must not be blank."`), with parameter name `value` — i.e. the shared helper does
     not flatten the messages.

5. **Types stay distinct and per-context (no merge)**
   - GIVEN the `identity`, `organization`, and `attendance` domain libraries
   - THEN each identifier and text VO remains a distinct type in its own context's namespace; no identifier
     or text VO is replaced by a single cross-context shared type; the `CrossContextIsolationConventionTests`
     and `@nx/enforce-module-boundaries` constraints remain satisfied.

6. **One definition of the mechanics**
   - GIVEN the codebase after the refactor
   - THEN the `New`/`From`/`TryParse`/implicit-operators/`ToString` body for identifiers exists only in the
     generator (each identifier declaration carries only `[GuidIdentifier]` + its partial struct), and the
     `IsNullOrWhiteSpace → null else Trim` body exists only in `RequiredText.TryNormalize`.

### Edge cases

- An identifier whose original `From` text was line-wrapped (e.g. `KeycloakSubjectIdentifier`) produces the
  **same single-line message string** after generation — formatting of the source must not change the
  message value.
- A text VO given input that is non-empty but all whitespace (`"   "`) returns `null` from `TryParse`
  (matches today).
- A text VO given input with internal whitespace (`"Acme  Corp"`) is preserved verbatim except for the
  outer trim (no internal collapsing).

## Requirements

### Functional

- **FR-001:** `shared-kernel` MUST provide a source generator that, applied to a `partial readonly record
  struct` marked with a `[GuidIdentifier]` attribute, emits: `Value` (Guid), `New()`, `From(Guid)`,
  `TryParse(Guid)`, `implicit operator Guid`, `implicit operator <T>(Guid)`, and `ToString()`, with the
  empty-GUID guard message `"{TypeName} must not be empty."` and parameter name `value`. The generated type
  MUST implement `IValueObject`.
- **FR-002:** Every existing GUID identifier across `identity`, `organization`, and `attendance` (the ~13
  structs, including the per-context duplicates of `UserIdentifier`, `OfficeIdentifier`, `EmployeeIdentifier`,
  `CompanyIdentifier`) MUST be converted to use FR-001 and MUST retain its current namespace, type name,
  public surface, behaviour, and exception message (Scenarios 1–2).
- **FR-003:** `shared-kernel` MUST provide `RequiredText.TryNormalize(string?)` returning the trimmed value
  or `null` for null/empty/whitespace-only input, with no other transformation.
- **FR-004:** Every existing required-trimmed-text VO (`DisplayName`, `EmployeeName`, `OfficeName`,
  `RoomName`, `CompanyName`, `Location`) MUST call FR-003 from its `TryParse`, keep its own distinct `From`
  blank-input message and parameter name `value`, and retain its current public surface and behaviour
  (Scenarios 3–4).
- **FR-005:** No identifier or text VO MAY be merged into a single shared cross-context type; each stays a
  distinct per-context value object (Scenario 5). The attribute/helper live in `shared-kernel`; `domain`'s
  dependency on `shared-kernel` is the only new edge.
- **FR-006:** No persistence mapping (EF value conversions / GUIDv7 keys), route, status code, response body,
  or OpenAPI schema MAY change as a result of this refactor; no Angular client regeneration is required.

### Non-functional

- **NFR-001:** The source generator MUST be referenced as a build-time analyzer only and MUST NOT become a
  runtime dependency of `shared-kernel` or any `domain` assembly — `SharedKernelPurityTests` (no MediatR /
  Wolverine / EF Core / ASP.NET / YARP, no framework) MUST stay green, i.e. the Roslyn/`Microsoft.CodeAnalysis`
  dependency stays inside the generator project and out of the inspected runtime assemblies.
- **NFR-002:** All existing domain unit tests (identifier and value-object behaviour) MUST stay green
  unchanged — they are the contract that behaviour did not move.
- **NFR-003:** All quality gates stay green: `dotnet build -warnaserror` (nullable + analyzers, no new
  warnings from generated code), `dotnet test` (unit + integration + architecture), `dotnet format
  --verify-no-changes`, and `pnpm nx affected -t lint` (module boundaries).

## Test-first plan (Red → Green)

- Author the ADR first (golden rule 4); this slice does not start until the generator/helper decision is
  recorded.
- **Unit (generator):** a fixture struct marked `[GuidIdentifier]` exercises `New`/`From`/`TryParse`/both
  implicit operators/`ToString`/equality and the empty-GUID message — written and failing before the
  generator exists.
- **Unit (`RequiredText`):** null / empty / whitespace-only → `null`; padded → trimmed; internal whitespace
  preserved — failing before the helper exists.
- **Unit (each refactored type):** the existing per-identifier and per-text-VO tests stay green; where a
  type lacks a message-text assertion today, add one pinning its exact `ArgumentException` message before
  refactoring it (Scenarios 2 and 4).
- **Architecture (regression):** `SharedKernelPurityTests`, `CrossContextIsolationConventionTests`, and
  `LayerDependencyConventionTests` stay green — proving no type merged across contexts and no framework
  leaked into the core.

## Out of scope

- Merging any identifier or text VO into a shared cross-context type (forbidden by ADR-0031 / context
  isolation).
- Changing GUIDv7 generation, EF Core value conversions, or any persistence mapping.
- Generalizing the generator beyond GUID identifiers (e.g. string- or int-backed identifiers, length- or
  format-constrained text) — add only what the existing types need; anything further is a separate spec.
- Changing any error message text, equality semantics, or public member of an existing type.
- Any frontend branded-type or TypeScript-side de-duplication.

## Review & Acceptance Checklist

- [ ] ADR authored and accepted before implementation (generator + `RequiredText` decision, with the
      simplicity-vs-duplication trade-off recorded)
- [ ] Every functional requirement has a test written before its implementation
- [ ] Each identifier keeps an identical public surface, behaviour, and empty-GUID message
- [ ] Each text VO keeps identical normalization and its own distinct blank-input message
- [ ] No identifier or text VO is merged across contexts; all stay per-context distinct types
- [ ] Generator is analyzer-only; `shared-kernel` and `domain` gain no runtime framework dependency
- [ ] All existing domain unit tests and architecture/boundary tests stay green
- [ ] No persistence, wire-contract, or OpenAPI change; no client regen
- [ ] All gates green; no suppressions
