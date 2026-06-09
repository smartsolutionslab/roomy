# Data Model: Office & Room Management

Organization context. Aggregates organised by folder + namespace (one folder per aggregate, holding
the root, its value objects, its entities, and its repository interface) per `CLAUDE.md`.

## Aggregates & entities

### `Company` (aggregate root — minimal, seeded)

The single seeded company that all offices belong to. No management behaviour in the MVP; it exists
so office-name uniqueness has a real scope and to honour the documented model (CLAUDE.md: "Company
(seeded root)").

| Member | Type | Notes |
|---|---|---|
| `Identifier` | `CompanyIdentifier` | branded GUIDv7 |
| `Name` | `CompanyName` | non-empty, trimmed |

- Factory: `Company.Create(CompanyName)` → `Company` (id assigned). Seeded once at startup from
  configuration (idempotent via `ExistsAsync`), mirroring identity's `DefaultAdminSeeder`.
- `ICompanyRepository`: `Task<bool> ExistsAsync(CancellationToken)`, `Task AddAsync(Company, …)`,
  `Task<Result<Company>> GetSeededAsync(CancellationToken)` (the single company; `Error.NotFound`
  if unseeded).

### `Office` (aggregate root)

The consistency boundary for its rooms: it owns the room collection and enforces room-name
uniqueness within itself and the derived capacity sum.

| Member | Type | Notes |
|---|---|---|
| `Identifier` | `OfficeIdentifier` | branded GUIDv7 |
| `CompanyIdentifier` | `CompanyIdentifier` | the owning company (by ID only) |
| `Name` | `OfficeName` | non-empty, trimmed; unique within the company (set-level, see below) |
| `Location` | `Location` | non-empty, trimmed |
| `Rooms` | `IReadOnlyList<Room>` | the contained rooms; never exposed mutably |
| `Capacity` | `Capacity` (derived) | `=> sum of Rooms' capacities`; **no setter** (FR-008) |

Behaviour:

- `Office.Create(CompanyIdentifier, OfficeName, Location)` → `Office` (starts with no rooms,
  scenario 1).
- `Rename(OfficeName)` — sets the name (FR-002). Uniqueness re-check is a handler concern.
- `RelocateTo(Location)` — sets the location (FR-003).
- `AddRoom(RoomName, Capacity)` → `Result<Room>` — rejects a duplicate room name within this office
  (`Error.Conflict("office.room_name_taken", …)`, edge case), otherwise appends a new `Room` and
  returns it (scenario 2). Capacity ≥ 1 is enforced by the `Capacity` value object before this point
  (scenario 6 / FR-007).
- `RenameRoom(RoomIdentifier, RoomName)` → `Result` — `Error.NotFound` if the room is not in this
  office; `Error.Conflict` on a duplicate name; otherwise renames (scenario 5).

> Office-name uniqueness **within the company** is a *set-level* invariant across offices — the
> aggregate cannot see its siblings. It is enforced by a unique index `(company_id, name)` plus an
> `ExistsByNameAsync(company, name)` pre-check in the create/rename handlers (mirrors identity's
> `Email` uniqueness). Room-name uniqueness is *within* one office, so the `Office` aggregate
> enforces it directly **and** a unique index `(office_id, name)` backs it at the database.

### `Room` (entity, inside the `Office` aggregate)

| Member | Type | Notes |
|---|---|---|
| `Identifier` | `RoomIdentifier` | branded GUIDv7 |
| `Name` | `RoomName` | non-empty, trimmed; unique within the office |
| `Capacity` | `Capacity` | ≥ 1, fixed at creation in the MVP (FR-006) |

- `Rename(RoomName)` — internal; reached only through `Office.RenameRoom`.
- No public construction outside the aggregate; created via `Office.AddRoom`.

## Value objects (`IValueObject`, guard with `Ensure.That`)

| Value object | Shape | Invariants |
|---|---|---|
| `CompanyIdentifier` | `readonly record struct` | GUIDv7, non-empty; implicit `Guid` conversions |
| `OfficeIdentifier` | `readonly record struct` | as above |
| `RoomIdentifier` | `readonly record struct` | as above |
| `CompanyName` | `sealed record` | non-null, non-whitespace, trimmed |
| `OfficeName` | `sealed record` | non-null, non-whitespace, trimmed |
| `Location` | `sealed record` | non-null, non-whitespace, trimmed |
| `RoomName` | `sealed record` | non-null, non-whitespace, trimmed |
| `Capacity` | `readonly record struct` | whole number ≥ 1 (FR-007); `From(int)` throws below 1, `TryParse` returns null |

Each identifier follows identity's pattern: `New()` (GUIDv7), `From(Guid)` (throws on empty),
`TryParse(Guid)` (null on empty), implicit `Guid`↔ conversions for trivial EF value converters.

## Persistence (`OrganizationDbContext : RoomyDbContext`)

snake_case naming is applied by the shared baseline. Three tables:

### `companies`
- PK `identifier` (uuid, value-generated never)
- `name` (text, required)

### `offices`
- PK `identifier` (uuid, value-generated never)
- `company_identifier` (uuid, required) — FK → `companies.identifier`
- `name` (text, required)
- `location` (text, required)
- Unique index `ux_offices_company_identifier_name` on `(company_identifier, name)` (FR-010)
- Capacity is **not** stored (derived from rooms)

### `rooms`
- PK `identifier` (uuid, value-generated never)
- `office_identifier` (uuid, required) — FK → `offices.identifier`, cascade delete with the office
- `name` (text, required)
- `capacity` (int, required)
- Unique index `ux_rooms_office_identifier_name` on `(office_identifier, name)` (FR-010)

EF mapping notes:
- `Office` → `Room` is a one-to-many owned-by-aggregate relationship; the `Rooms` collection is
  exposed read-only and mapped to a backing field (`builder.Navigation(o => o.Rooms).HasField(...)
  .UsePropertyAccessMode(PropertyAccessMode.Field)`).
- `Capacity` (derived) is ignored on `Office` (`builder.Ignore(o => o.Capacity)`); `Room.Capacity`
  is a stored value-object column via converter.
- Value objects round-trip via `HasConversion` (identifier → `Guid`, names/location → `string`,
  `Capacity` → `int`), exactly as identity maps `Email`/`Role`.

Initial migration `InitialCreate` is generated through a host design-time factory (EF migrations are
marked generated code in `.editorconfig`). Integration tests apply it with `MigrateAsync`. The
shared `db-migrator` (ADR-0033) registers `OrganizationDbContext` so the `organization` database is
migrated before the API starts.
