# 0047. Typo-tolerant employee name search via Postgres trigrams (pg_trgm)

- **Status:** Accepted
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

Two employee lists grow with company size and become unusable by scrolling alone:

- `GET /reservations/employees` (attendance, administrator-only) — the on-behalf picker (009),
  already keyset-paginated on `(Name, EmployeeId)` (ADR-0044).
- the organization employee directory — which has **no list endpoint today** (the Employee
  aggregate host only exposes `POST /employees` to hire).

Feature `012-employee-search` adds **find-an-employee-by-name** to both, and the requester chose
**typo-tolerant (fuzzy)** matching: a query must still find the intended colleague through a
transposed/inserted/deleted character, a missing accent, or a partial name token, and rank the
closest match first. A blank query must behave exactly as today (the ADR-0044 keyset list).

Two structural questions fall out, both cross-cutting enough to record before code (golden rule 4):

1. **How do we match fuzzily in Postgres** without an unbounded scan (ADR-0044 forbids
   materialize-then-slice; SC-004 requires no scan blow-up at 10k employees)?
2. **How does a similarity-ranked result still paginate** under the ADR-0044 keyset contract, whose
   worked examples all assume a *stable column* sort key (`Email`, `Date`, `(Name, EmployeeId)`)?

## Decision drivers

- Typo tolerance and fragment matching (SC-002), not just prefix/`LIKE`.
- Stay inside ADR-0044: opaque cursor, keyset predicate pushed into SQL, `{ items, nextCursor }`
  envelope, default 50 / max 100, 400 on bad input — search must *extend*, not fork, the contract.
- Each context searches **only its own data** (ADR-0014): attendance searches its `Employees`
  read model, organization searches its `Employee` master-data table. No cross-context query.
- Bounded work per request — a trigram index must serve the match so the candidate set is capped
  by the index, not a full-table similarity computation.
- Reuse the existing read-model raw-SQL style (the attendance `EmployeeCatalog` already issues
  `FromSql` keyset queries) and the shared `Page<T>`/`PageRequest`/`CursorCodec` primitives.

## Decision

Adopt **PostgreSQL `pg_trgm` trigram matching**, ranked by **word-similarity**, paginated as a
**new keyset sort key under the existing ADR-0044 contract**.

### 1. Matching — `pg_trgm` word-similarity, accent-folded

- Enable the `pg_trgm` and `unaccent` extensions (per database) in an EF Core migration, in **both**
  the attendance read-model database and the organization database. No shared database (ADR-0014) —
  each owns its own extension + index.
- Define an `IMMUTABLE` SQL wrapper over `unaccent(...)` (the stock `unaccent` is only `STABLE`, so
  it cannot back an index) and a **GIN trigram index** on `immutable_unaccent(<name column>)
  gin_trgm_ops`. `pg_trgm` already folds case, so the index gives case- **and** accent-insensitive
  matching (FR-002).
- Match with the **word-similarity** operator `@q <% name` (a query that is a *fragment/token* of a
  longer name matches well — plain whole-string `similarity` would penalise the length gap) and rank
  by `word_similarity(@q, name) DESC`. The `<%` threshold pre-filter uses the GIN index, so the
  candidate set is index-bounded — never a full scan (SC-004). The exact threshold lives in
  `research.md` and is tuned to SC-002, not frozen here.

### 2. Search is a new sort key, not a new pagination model

ADR-0044 already states the sort key may evolve and the cursor is opaque precisely so it can. Search
**uses that latitude** rather than inventing offset paging for the searched case:

- **Blank/omitted `q`** → unchanged: keyset on `(Name, EmployeeId)` (attendance) / name (organization),
  cursor `= (name, id)`.
- **Non-blank `q`** → keyset on `(word_similarity DESC, Name, EmployeeId)`; the opaque cursor encodes
  `(similarity, name, id)`. Names tie on similarity often, so `(name, id)` remains the deterministic
  tiebreaker — the order stays **stable and total**, so paging never skips or duplicates. The SQL is
  the ADR-0044 shape with the similarity term added:

  ```sql
  WHERE @q <% name                                   -- index-bounded candidate set
    AND ( word_similarity(@q, name) < @cursorSim
       OR ( word_similarity(@q, name) = @cursorSim
            AND (name, id) > (@cursorName, @cursorId) ) )
  ORDER BY word_similarity(@q, name) DESC, name, id
  LIMIT @limit + 1
  ```

- **`q` is a separate, repeated query parameter — it is *not* baked into the cursor.** A cursor is
  only meaningful for the `q` it was issued with; the web client resends `q` alongside `nextCursor`,
  and **changing `q` restarts paging from no cursor**. A cursor whose shape does not match the
  current `q` mode (a similarity cursor sent with a blank `q`, or vice-versa) fails to decode and is
  rejected **400**, like any malformed cursor (ADR-0044).

### 3. Surfaces

- **Attendance** `GET /reservations/employees` gains an optional `q`. The existing `IEmployeeCatalog`
  read port + `EmployeeCatalog` infrastructure query and the `ViewEmployees` query gain the search
  term; the HTTP boundary keeps returning the concrete `EmployeePage` record (ADR-0044/0036).
- **Organization** gains a **new** `GET /employees` (administrator-only) returning a new concrete
  `EmployeePage` record in the same `{ items, nextCursor }` envelope. It is served by a **new read
  port** in `organization/application` (a query-side `IEmployeeDirectory`, returning
  `Page<…>`), implemented in `organization/infrastructure` against the `Employee` table — the
  write-side `IEmployeeRepository` (aggregate load/save) is **not** widened with search queries
  (CQRS read/write separation; the directory is read/search-only — editing stays out of scope).

OpenAPI specs are re-emitted and the generated Angular clients regenerated and drift-gated
(ADR-0036). The web surfaces consume it through their `data-access` facades and the existing
`@roomy/shared-ui` infinite-scroll list (ADR-0044), adding an accessible, localized (DE+EN) search
box (ADR-0024).

## Considered options

- **A — `pg_trgm` word-similarity + GIN index, keyset on `(similarity, name, id)` (chosen).**
  Typo-tolerant, fragment-friendly, index-bounded, and stays inside the ADR-0044 keyset contract by
  treating search as a new (opaque) sort key.
- **B — `ILIKE '%q%'` substring match.** Simple, but not typo-tolerant (fails SC-002), and a leading
  `%` cannot use a B-tree index → sequential scan at scale (fails SC-004). Rejected.
- **C — `pg_trgm` whole-string `similarity()` / `%` operator.** Trigram-indexable, but whole-string
  similarity penalises matching a short fragment against a long full name, so good fragment queries
  rank poorly. Word-similarity (`<%`) is the right pg_trgm tool for "fragment within a name."
  Rejected in favour of A.
- **D — Postgres full-text search (`tsvector`/`tsquery`).** Strong for documents/stemming, weak for
  *typos* in short proper nouns (it matches lexemes, not fuzzy spellings). Rejected — wrong tool for
  name search.
- **E — Top-N search results, no pagination (`nextCursor` always null).** Simpler, but spec Story 1
  AC-3 / FR-004 require searched results to scroll like every other list. Rejected.
- **F — Offset/`LIMIT … OFFSET` for the searched case only.** Forks the pagination model ADR-0044
  unified and is unstable across inserts. Rejected.
- **G — A dedicated search engine (Elasticsearch/Meilisearch/Typesense).** Real infrastructure and
  an index to keep in sync via events, for two admin-only name lookups. Massive over-build for v1
  (simplicity first). Rejected; `pg_trgm` already ships with our Postgres.

## Consequences

- Two new migrations (attendance read-model DB, organization DB): `CREATE EXTENSION IF NOT EXISTS
  pg_trgm`/`unaccent`, the `immutable_unaccent` wrapper, and a GIN trigram index per searchable name
  column. Backend tests run against **real Postgres** (no SQLite — the trigram operators are
  Postgres-only; this is exactly why the no-SQLite rule exists), asserting: a typo still returns the
  intended employee on page 1 (SC-002), best-match-first ordering, search paging is stable across an
  insert, blank `q` reproduces the ADR-0044 list verbatim, an over-long/malformed `q` or a
  mode-mismatched cursor → 400, and 403 for a non-administrator (unchanged).
- A new organization read port + infrastructure query and a new `GET /employees` endpoint; the
  organization context gains its first read/list surface. Architecture tests already cover its
  layers (no new project reference needed).
- The opaque cursor now carries one of two shapes per list; this is invisible to clients (the whole
  point of ADR-0044's opacity) but is a documented invariant of the read model and is unit-tested in
  the cursor round-trip.
- `q` length is capped (rejected with 400 above the cap) so the trigram match input is bounded.
- Search is **name-only, administrator-only, read-only** for v1 (email/role/office search, faceting,
  employee-facing search, and editing from the directory are out of scope — see the spec).

## References

- Spec: `specs/012-employee-search/spec.md`; plan + `research.md` (threshold/operator tuning).
- Builds on **ADR-0044** (cursor/keyset pagination) and **ADR-0036** (OpenAPI client codegen).
- Constrained by **ADR-0014** (a service per context, own database, no cross-context query) and
  **ADR-0024** (WCAG 2.2 AA + DE/EN localization).
