# Feature Specification: Register value-object conversions centrally as an EF Core convention

**Feature Branch:** `refactor/028-ef-value-object-conventions`
**Status:** Draft
**Created:** 2026-06-27
**Updated:** 2026-06-27
**Relates to:** ADR-0012 (EF Core persistence; the base `RoomyDbContext` all contexts derive from)

## Summary

A behaviour-preserving persistence de-duplication. Today every `IEntityTypeConfiguration`
hand-writes the same three things for each mapped type: a
`.HasConversion(x => x.Value, v => XIdentifier.From(v)).ValueGeneratedNever()` for every
branded identifier, a `.HasConversion(x => x.Value, v => XVo.From(v))` for every other value
object, and a `.Ignore(x => x.DomainEvents)` for every aggregate. EF Core does not pick up the
value objects' implicit `Guid`/`string`/`int` operators, so each converter is repeated by hand —
dozens of near-identical lines spread across `organization`, `identity`, and `attendance`
(aggregate configs *and* read-model configs). The repetition is drift-prone: a single config can
silently omit `ValueGeneratedNever()` on a key or forget to ignore `DomainEvents`, and nothing
fails until a migration or a save misbehaves.

This slice registers the value-object conversions **once**, centrally, on the base
`RoomyDbContext` that all three contexts' `DbContext`s derive from (directly, or via
`EventStoreDbContext`): a convention supplies the `Value`/`From` converter for every
`IValueObject`-backed property, keeps `ValueGeneratedNever()` on branded identifiers, and a small
`builder.IgnoreDomainEvents()` (or equivalent model-wide rule) drops `DomainEvents` on every
aggregate. The per-property `HasConversion`/`ValueGeneratedNever`/`Ignore(DomainEvents)` lines are
then removed from the configs. **Nothing about the stored model changes** — identical column types,
identical keys, identical indexes, identical migrations (no schema diff), identical query
behaviour.

## User Scenarios & Testing

### Primary story

As a maintainer, I want value-object persistence behaviour expressed once on the base context so
that adding a new identifier or value object needs no per-property converter, and no config can
silently drift on `ValueGeneratedNever()` or `DomainEvents`.

### Acceptance Scenarios

1. **Branded identifier round-trips without a per-property converter**
   - GIVEN an aggregate whose key is a branded `…Identifier` (Guid-backed `IValueObject`) and a
     config that declares **no** `.HasConversion(...)` for it
   - WHEN the aggregate is saved and re-read through its context
   - THEN the identifier persists as its backing `Guid` and materialises back to the identifier
     type unchanged.

2. **Identifier keys are still never store-generated**
   - GIVEN any aggregate or owned entity keyed by a branded identifier
   - WHEN the EF model is built
   - THEN the key property's `ValueGenerated` is `Never` (the application supplies GUIDv7 values),
     exactly as the hand-written `.ValueGeneratedNever()` did.

3. **Non-identifier value objects round-trip without a per-property converter**
   - GIVEN a property typed as a string-backed value object (e.g. `OfficeName`, `Location`,
     `Email`, `DisplayName`) or an int-backed one (e.g. `Capacity`), with no `.HasConversion(...)`
     in its config
   - WHEN the entity is saved and re-read
   - THEN the value persists as its backing primitive and materialises back to the value-object
     type unchanged, with the same column type as today.

4. **DomainEvents ignored on every aggregate**
   - GIVEN every mapped aggregate
   - WHEN the EF model is built
   - THEN `DomainEvents` is not a mapped property on any of them, without any config declaring
     `.Ignore(x => x.DomainEvents)`.

5. **No model/schema change (regression — the contract that nothing moved)**
   - GIVEN the migration model snapshot before this change
   - WHEN `dotnet ef migrations` / a model-diff check is run for each context after the change
   - THEN there is **no** pending model difference for any of the three contexts: no new
     migration is produced, column types, keys, indexes, and constraints are byte-for-byte the
     same.

6. **The convention applies across all three contexts**
   - GIVEN `OrganizationDbContext`, `IdentityDbContext` (both via `RoomyDbContext`) and
     `AttendanceDbContext` (via `EventStoreDbContext`)
   - THEN identifiers and value objects on aggregates **and** read models in every context get the
     central conversion, with no per-property converters remaining in their configs.

### Edge cases

- **Custom, non-passthrough mappings are untouched.** `UserConfiguration` maps `User.Role` to an
  `IsAdministrator` boolean column and enum properties (`Role`, `State`, `Status`, `FailureReason`)
  via `HasConversion<string>()`. These are not `Value`/`From` value-object passthroughs and MUST
  remain hand-written and unchanged.
- **A value object backed by a primitive the convention does not handle** (none today beyond
  Guid/string/int) is left for its config to map explicitly; the convention only claims the backing
  types it supports.
- **Owned-entity identifiers** (e.g. `Room.Identifier` under `Office.OwnsMany`) get the same
  identifier treatment (converter + `ValueGeneratedNever`) as root keys.

## Requirements

### Functional

- **FR-001:** The base `RoomyDbContext` MUST register, once, a value conversion (`value-object → backing
  primitive` and back via its `From` factory) for every property whose CLR type is an `IValueObject`
  backed by `Guid`, `string`, or `int`, such that derived contexts inherit it without any
  per-property `.HasConversion(...)`.
- **FR-002:** Properties whose CLR type is a branded identifier (a Guid-backed `IValueObject` used
  as / within a key) MUST be configured `ValueGeneratedNever()` centrally, preserving today's
  application-supplied GUIDv7 keys.
- **FR-003:** `DomainEvents` MUST be excluded from the model for every mapped aggregate centrally
  (a model-wide rule / `builder.IgnoreDomainEvents()` helper), with no per-aggregate
  `.Ignore(x => x.DomainEvents)` remaining.
- **FR-004:** The per-property `.HasConversion(...)`, `.ValueGeneratedNever()` (for identifiers),
  and `.Ignore(x => x.DomainEvents)` lines MUST be removed from every affected
  `IEntityTypeConfiguration` across `organization`, `identity`, and `attendance` (aggregate configs
  and read-model configs), once the central rules cover them. Configs keep only what is genuinely
  per-type: table/column names, `IsRequired`, indexes, ownership, and custom non-passthrough
  conversions (see edge cases).
- **FR-005:** The convention MUST apply to all `DbContext`s deriving from `RoomyDbContext`,
  including those deriving via `EventStoreDbContext`, covering every context's aggregates and read
  models.
- **FR-006:** The change MUST produce **no** EF model difference for any context: column types,
  keys, indexes, constraints, and the migration model snapshot are unchanged, and no new migration
  is generated.

### Non-functional

- **NFR-001:** No `domain`/`application` dependency is introduced; the convention lives in
  `infrastructure-persistence` and reflects only over `IValueObject` (a `shared-kernel` marker) and
  the loaded domain types EF already maps (ADR-0005, dependency rule).
- **NFR-002:** All existing quality gates stay green (`dotnet build -warnaserror`, `dotnet test`
  including the persistence integration tests and architecture tests,
  `dotnet format --verify-no-changes`).

## Test-first plan (Red → Green)

- **Model-shape unit tests** (per context, against the built `IModel`): for a representative
  identifier property, assert it has a value converter and `ValueGenerated == Never`; for a
  string- and an int-backed value object, assert the converter and the unchanged column type;
  assert `DomainEvents` is not a mapped property on each aggregate. These fail first because the
  per-property config is removed before the convention exists.
- **No-diff / snapshot regression:** a check that no pending model change exists for
  `organization`, `identity`, and `attendance` after the refactor (model-snapshot diff is empty,
  no new migration). This is the contract that schema and migrations did not move.
- **Persistence integration (regression, real stack):** the existing save/round-trip integration
  tests for each context stay green unchanged — proving the central conversion behaves exactly as
  the removed per-property converters did.

## Out of scope

- Any schema change, new migration, or column rename.
- Changing the enum-as-string mappings or `User.Role → IsAdministrator` custom conversion.
- The event store's own `StoredEvent` mapping and the snake-case naming convention (unrelated).
- Introducing source generators or compile-time converter generation — a runtime convention on the
  base context is sufficient; revisit only if reflection cost is shown to matter.

## Review & Acceptance Checklist

- [ ] Every functional requirement has a test written before its implementation
- [ ] No per-property `.HasConversion(...)` for a plain value object / identifier remains in any config
- [ ] No `.ValueGeneratedNever()` for an identifier and no `.Ignore(x => x.DomainEvents)` remains in any config
- [ ] `ValueGeneratedNever()` and `DomainEvents`-ignored still hold for every aggregate, proven by model-shape tests
- [ ] No EF model diff / new migration for any of the three contexts
- [ ] Custom non-passthrough mappings (`User.Role`, enum-as-string) untouched
- [ ] All gates green; no suppressions
