# Research: typo-tolerant employee name search (012)

Resolves the open technical choices behind ADR-0047. Format: Decision · Rationale · Alternatives.

## R1 — Matching engine: `pg_trgm` (not FTS, not `ILIKE`)

- **Decision:** PostgreSQL `pg_trgm` trigram matching.
- **Rationale:** Names are short proper nouns; the requirement is *typo* tolerance (transposition /
  insertion / deletion) and fragment matching, which trigram similarity handles natively and which a
  GIN index can serve. It ships with our Postgres (no new infrastructure, ADR-0014 stays intact).
- **Alternatives:** Full-text search (`tsvector`/`tsquery`) matches lexemes/stems, not misspellings —
  wrong tool for typos. `ILIKE '%q%'` is neither typo-tolerant nor index-served for a leading
  wildcard (seq scan, fails SC-004). External search engine — massive over-build for two admin-only
  name lookups.

## R2 — Operator: word-similarity `<%` (not whole-string `%`)

- **Decision:** Filter with the word-similarity operator (`@q <% name`) and rank by
  `word_similarity(@q, name)` descending.
- **Rationale:** A user types a fragment ("dan", "müller") against a full display name ("Daniel
  Müller"). Whole-string `similarity()` divides shared trigrams by the union over the *whole* string,
  so a short fragment against a long name scores low and good matches get filtered out. Word-similarity
  measures how well the query matches the *best-matching contiguous extent* of the name — exactly
  fragment-in-name. The GIN trigram index (`gin_trgm_ops`) serves the `<%` operator.
- **Alternatives:** Whole-string `%`/`similarity()` (penalises length mismatch — rejected). Strict
  word-similarity `<<%` (anchors to word boundaries; slightly stricter, kept as a tuning knob if `<%`
  proves too loose, but `<%` is the v1 default).
- **Note (asymmetry):** `word_similarity(a, b)` is directional — it scores how well `a` matches within
  `b`. We always pass `(query, name)`; the `<%` operator's left operand is the query.

## R3 — Case & accent folding

- **Decision:** `pg_trgm` already lowercases when extracting trigrams → case-insensitivity is free.
  For accents, wrap the name column and the query in an **`IMMUTABLE`** SQL function over the stock
  `unaccent`, and build the index on that expression.
- **Rationale:** Stock `unaccent(regdictionary, text)` is only `STABLE`, so it cannot back a
  functional index. The standard recipe is a thin `IMMUTABLE` wrapper:

  ```sql
  CREATE EXTENSION IF NOT EXISTS unaccent;
  CREATE EXTENSION IF NOT EXISTS pg_trgm;

  CREATE OR REPLACE FUNCTION immutable_unaccent(text)
      RETURNS text
      LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
      RETURN public.unaccent('public.unaccent', $1);

  CREATE INDEX ix_<table>_name_trgm
      ON <table> USING gin (immutable_unaccent(<name_col>) gin_trgm_ops);
  ```

  Both the filter and the ranking use `immutable_unaccent(<name_col>)`, and the query term is
  `immutable_unaccent(@q)`, so "José" ⇄ "jose" match and the index is used (FR-002).
- **Alternatives:** `citext` (case only, no accents, and trigram ops still need the GIN index anyway).
  A stored, app-maintained normalized column (extra write-path complexity for no index benefit over
  an expression index). Skipping accents (violates FR-002).

## R4 — Pagination of a similarity-ranked result (the ADR-0044 fit)

- **Decision:** Treat search as a **new opaque sort key** under ADR-0044: keyset on
  `(word_similarity DESC, name ASC, id ASC)`; cursor encodes `(similarity, name, id)`. Blank `q`
  keeps today's `(name, id)` keyset.
- **Rationale:** ADR-0044 made the cursor opaque *specifically* so the sort key can evolve. Similarity
  is deterministic for a fixed `q`, and `(name, id)` breaks the frequent similarity ties, giving a
  stable total order → no skip/duplicate across inserts (the ADR-0044 stability guarantee holds). The
  keyset predicate is the ADR-0044 shape plus the similarity term (see ADR-0047 §2 for the SQL).
- **Alternatives:** Offset paging for search (unstable across inserts, forks the model — rejected).
  Top-N no-pagination (violates Story 1 AC-3 — rejected). Encoding `q` into the cursor (unnecessary —
  the client resends `q`; a cursor is only valid for its issuing `q`, and a mode mismatch fails to
  decode → 400, same path as any malformed cursor).

## R5 — Threshold & `q` bounds

- **Decision:** Set the word-similarity threshold per request via `set_limit()` / the
  `pg_trgm.word_similarity_threshold` GUC at a value tuned so a single-typo query keeps the intended
  name on page 1 (SC-002) — start at **0.3** and lock the final value with the SC-002 integration
  test. Reject `q` longer than a sane name bound (**100 chars**, matching the existing
  `PageRequest`/name-length ethos) with **400**; a 1-char `q` is allowed (may match broadly — an
  accepted edge case).
- **Rationale:** The default `word_similarity_threshold` (0.6) is too strict for short fragments; a
  data-driven threshold pinned by a test prevents silent regressions. Bounding `q` keeps the trigram
  input finite (no pathological input).
- **Alternatives:** Hard-coded 0.6 (drops good fragment matches). No cap on `q` (unbounded input).

## R6 — Where the read path lives (organization)

- **Decision:** New **read port** `IEmployeeDirectory` in `organization/application/Ports` returning
  `Result<Page<EmployeeListItem>>`, implemented in `organization/infrastructure` with a raw-SQL
  query (mirroring attendance's `EmployeeCatalog`). The write-side `IEmployeeRepository`
  (`AddAsync`/`GetByIdentifierAsync`) is **unchanged**.
- **Rationale:** CQRS read/write separation; search/list is a read concern and does not belong on the
  aggregate's write repository. Matches the attendance picker's existing port shape, so the two
  surfaces read the same way.
- **Alternatives:** Add a `SearchAsync` to `IEmployeeRepository` (mixes read projections into the
  write contract — rejected). A separate read-model table fed by events (the org table already holds
  the master data in-process; a projection adds sync complexity for no isolation benefit — rejected).

## R7 — Testing against real Postgres

- **Decision:** Backend search tests run against real Postgres via the existing Aspire integration
  fixture (attendance-integration / organization-integration).
- **Rationale:** Trigram operators, `unaccent`, and the GIN index are Postgres-only — there is no
  in-memory substitute (this is precisely the no-SQLite rule). Migrations (extension + index) must be
  exercised so CI catches a missing extension in a fresh database.
- **Alternatives:** none viable (in-memory/SQLite cannot evaluate `<%`).
