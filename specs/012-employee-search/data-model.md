# Data model & read shapes: employee search (012)

No new aggregate, no new persisted entity. This feature adds **read shapes**, **cursor shapes**, and
**index/migration** changes only. Source of truth for behaviour: the spec; for decisions: ADR-0047.

## Read shapes (returned by the read ports)

Both surfaces expose the same minimal `{ id, name }` projection (search is name-only).

| Surface | Read port (application) | Item shape |
|---|---|---|
| Attendance picker | `IEmployeeCatalog.GetAsync(query, request, ct)` (extended) | `EmployeeView(EmployeeIdentifier Employee, string Name)` (exists) |
| Organization directory | `IEmployeeDirectory.SearchAsync(query, request, ct)` (**new**) | `EmployeeListItem(EmployeeIdentifier Employee, string Name)` (**new**) |

Both return `Result<Page<…>>` (`Page<T>` from `…SharedKernel.Pagination`). The HTTP edge maps each to
a concrete `EmployeePage(IReadOnlyList<EmployeeResponse> Items, string? NextCursor)` + `EmployeeResponse`
record per host (ADR-0044/0036 — stable OpenAPI schema names; organization gets its own copy).

## Query input

A bounded, optional search term carried beside the existing `PageRequest`:

- `q`: optional string, trimmed. Blank/whitespace ⇒ "no filter". Length > 100 ⇒ **400**.
- The term is a shared **`SearchTerm`** value object (`…SharedKernel.Search`, foundational task
  T003): `SearchTerm.From(string?) → Result<SearchTerm>` applies trim / blank-is-empty / max-100,
  and exposes `IsEmpty` + the normalized term. Both read ports take it directly, e.g.
  `GetAsync(SearchTerm term, PageRequest request, CancellationToken ct)` — a typed value, not a bare
  primitive threaded through layers, and not a per-context `EmployeeQuery` wrapper (one shared type,
  validated identically in both contexts).

## Cursor shapes (opaque to clients, owned by each read model)

Both surfaces use a **`(name, id)`** tiebreaker because neither name column is unique (names collide;
the id breaks ties → a stable, total order, ADR-0044). The id is the attendance read model's
`employee_id` and the organization table's `identifier`.

| Surface | `q` mode | Sort order | Cursor record (lives with the read model) |
|---|---|---|---|
| Attendance | blank | `(display_name, employee_id)` ASC | `EmployeeCursor(string Name, Guid EmployeeId)` (exists) |
| Attendance | non-blank | `(word_similarity DESC, display_name ASC, employee_id ASC)` | `EmployeeSearchCursor(double Similarity, string Name, Guid EmployeeId)` (**new**) |
| Organization | blank | `(name, identifier)` ASC | `EmployeeCursor(string Name, Guid EmployeeId)` (**new**, org copy) |
| Organization | non-blank | `(word_similarity DESC, name ASC, identifier ASC)` | `EmployeeSearchCursor(double Similarity, string Name, Guid EmployeeId)` (**new**, org copy) |

Encoded/decoded with `CursorCodec` (base64url JSON). Each context owns its own cursor records (no
cross-context type sharing, ADR-0014). A cursor whose decoded shape does not match the current `q`
mode (or any malformed cursor) ⇒ **400** (ADR-0044 path). `q` is **not** encoded in the cursor —
the client resends it; changing `q` resets paging.

## Persistence changes (migrations — one per database, ADR-0014)

Applied by the dedicated migration runner (ADR-0033). Each context owns its own:

**Attendance read-model DB** — column `employees.display_name`:
1. `CREATE EXTENSION IF NOT EXISTS pg_trgm;` · `CREATE EXTENSION IF NOT EXISTS unaccent;`
2. `immutable_unaccent(text)` wrapper (IMMUTABLE) — see `research.md` R3.
3. `CREATE INDEX ... USING gin (immutable_unaccent(display_name) gin_trgm_ops);`

**Organization DB** — column `employees.name`: identical three steps on the `name` column.

The existing keyset index/ordering for the blank-`q` path is unchanged.

## Query predicate (per read model, raw SQL — ADR-0047 §2)

```sql
-- non-blank q (parameters via FromSql interpolation)
WHERE @q <% immutable_unaccent(<name_col>)
  AND ( word_similarity(@q, immutable_unaccent(<name_col>)) < @cursorSim
     OR ( word_similarity(@q, immutable_unaccent(<name_col>)) = @cursorSim
          AND (<name_col>, <id_col>) > (@cursorName, @cursorId) ) )
ORDER BY word_similarity(@q, immutable_unaccent(<name_col>)) DESC, <name_col>, <id_col>
LIMIT @limit + 1;
```

`@q` is itself `immutable_unaccent`-folded before binding (or folded inline). Blank `q` keeps the
existing ADR-0044 `(name,id)` keyset query verbatim. Fetch `limit + 1` to compute `nextCursor`.

## Validation rules (testable)

- `q` length ≤ 100; else 400.
- Blank `q` (attendance) ⇒ identical result set/order to the pre-012 picker list (regression-asserted).
- Blank `q` (organization) ⇒ the directory in stable `(name, identifier)` keyset order (new endpoint,
  no pre-012 baseline — asserted directly).
- Non-blank `q` ⇒ items ordered by descending similarity, `(name,id)` tiebreak; a single-typo query
  returns the intended employee on page 1 (SC-002).
- `nextCursor` null exactly when no further row (the `limit + 1` probe found none).
- Admin-only: non-admin ⇒ 403 with/without `q` (unchanged).
