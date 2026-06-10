# Implementation Plan: Search employees by name (typo-tolerant) (012)

**Branch:** `feat/012-employee-search` · **Date:** 2026-06-10
**Spec:** `specs/012-employee-search/spec.md` · **Decision:** ADR-0047 (builds on ADR-0044, ADR-0036)

## Summary

Add typo-tolerant, name-only employee search to two administrator-only lists, ranked best-match
first and paginated under the existing ADR-0044 keyset contract. Matching is Postgres `pg_trgm`
**word-similarity** over an accent-folded GIN trigram index; a blank `q` reproduces today's list
verbatim. Attendance's existing picker (`GET /reservations/employees`) gains an optional `q`; the
organization context gains a **new** read-only directory (`GET /employees`) with the same `q` and
`{ items, nextCursor }` envelope. OpenAPI specs + Angular clients are regenerated and drift-gated;
each web surface gets an accessible, DE/EN search box reusing the `@roomy/shared-ui` infinite list.

## Technical Context

**Language/Version:** .NET 10 / C# (backend); Angular 22, TypeScript (frontend).
**Primary Dependencies:** EF Core + Npgsql; Postgres `pg_trgm` + `unaccent` extensions; Wolverine
(unaffected — no new cross-context flow); `ng-openapi-gen`; Transloco; Angular CDK.
**Storage:** PostgreSQL — attendance read-model DB (`Employees` read model) and organization DB
(`Employee` master data). Separate databases (ADR-0014); each gets its own extension + index.
**Testing:** xUnit v3 + Shouldly; integration tests against **real Postgres** via the Aspire fixture
(no SQLite — trigram operators are Postgres-only); `vitest-analog` + `@testing-library/angular`.
**Target Platform:** Linux containers behind the YARP gateway.
**Project Type:** Web (multi-service backend + single Angular SPA).
**Performance Goals:** First page within the unsearched list's latency budget at 10k employees
(SC-004); the `<%` threshold pre-filter is GIN-index-served (no full-table similarity scan).
**Constraints:** Default limit 50 / max 100; `q` length-capped; admin-only (403 otherwise);
WCAG 2.2 AA + DE/EN (ADR-0024).
**Scale/Scope:** Two endpoints (one extended, one new), one org read port + infra query, two
migrations, two web search boxes. No new bounded context, no new cross-context event.

## Constitution Check

*GATE: must pass before Phase 0 and re-checked after design.*

- **I. Spec-driven & test-first** — spec `012` + acceptance criteria exist; every criterion lands as
  a failing test before code (see Test strategy). ✅
- **II. Clean Architecture & DDD** — search term flows domain-free: read **ports** in `application`
  (`IEmployeeCatalog` extended; new org `IEmployeeDirectory`), raw-SQL impls in `infrastructure`.
  No EF/Npgsql type escapes inward. ✅
- **III. Context isolation** — attendance searches its own read model; organization searches its own
  table; neither queries the other (ADR-0014). No cross-context reference. ✅
- **IV. No framework in core** — ports are owned abstractions; `pg_trgm`/EF live only in
  `infrastructure`. ✅
- **V. ADR-before-code** — **ADR-0047** written in this plan, before implementation. ✅
- **VI. Green before done** — full gate suite (below); no suppressions. ✅
- **VII. Small, single-purpose** — one story, one short-lived branch. The three prioritized stories
  (P1 attendance search · P2 org directory · P3 web) are independently testable and **may land as
  separate commits/PRs** off this branch if review size demands. ✅

No violations → Complexity Tracking omitted.

## Bounded contexts touched

- **attendance** — extend `IEmployeeCatalog.GetAsync` + `EmployeeCatalog` (search SQL), `ViewEmployees`
  query, and the `ViewEmployeesAsync` endpoint with optional `q`. New migration: `pg_trgm`/`unaccent`
  + GIN trigram index + `immutable_unaccent` wrapper on the read-model `display_name`.
- **organization** — **new** read port `IEmployeeDirectory` (`…Organization.Application.Ports`)
  returning `Page<EmployeeListItem>`; infra impl against the `Employee` table; new `GET /employees`
  endpoint + concrete `EmployeePage`/`EmployeeResponse` records; new migration mirroring attendance's
  extension + index on the `name` column. Write-side `IEmployeeRepository` is untouched.
- **web** — attendance + organization `data-access` facades gain a `q` param; two feature list views
  gain a search box (debounced signal → query → reset cursor on change) over the existing
  `@roomy/shared-ui` infinite list; DE/EN Transloco keys; accessible labelled control.
- **shared-kernel** — reused as-is (`Page<T>`, `PageRequest`, `CursorCodec`). A new search-cursor
  record lives **with each read model**, not in shared-kernel (each list owns its sort-key record,
  per ADR-0044).

## Key design points

- **Word-similarity, accent-folded, index-bounded** — `@q <% immutable_unaccent(name)` pre-filter on
  a GIN trigram index, ranked `word_similarity(@q, immutable_unaccent(name)) DESC, name, id`. See
  `research.md` for operator/threshold rationale (word-similarity over whole-string `similarity`;
  threshold tuned to SC-002).
- **Search = a new opaque sort key under ADR-0044** — blank `q`: cursor `(name, id)` (today). Non-blank
  `q`: cursor `(similarity, name, id)`, keyset predicate adds the similarity term (ADR-0047 §2). The
  read model decodes the cursor for the current `q` mode; a mode mismatch / over-long `q` → **400**.
- **CQRS read path in organization** — new read port + raw-SQL infra query, not a new method on the
  aggregate's write repository.
- **Stable HTTP schema** — both endpoints return concrete `*Page`/`*Response` records (not generic
  `Page<T>`) so emitted OpenAPI schema names stay clean for the drift gate (ADR-0036).
- `q` carried as a repeated query param alongside `cursor`; the web client drops the cursor when `q`
  changes (fresh search), restoring the full list on clear.

## Test strategy (RED → GREEN)

- **attendance integration (real Postgres):** typo on page 1 (SC-002); best-match-first order;
  search paging stable across an insert; blank `q` reproduces the ADR-0044 list verbatim; over-long
  `q` / mode-mismatched cursor → 400; 403 for non-admin unchanged.
- **organization integration (real Postgres):** new `GET /employees` envelope + ordering + typo
  tolerance; 403 for non-admin; blank `q` lists the directory in stable name order.
- **cursor unit tests:** round-trip of the new `(similarity, name, id)` search cursor.
- **OpenAPI/codegen:** `OpenApiDocumentTests` re-emit; `generate-client` drift gate green for both
  attendance and organization clients.
- **web:** `@testing-library/angular` — typing filters + ranks; clearing restores; scroll appends in
  similarity order; control is keyboard/screen-reader operable; DE/EN labels render.

## Gates

`dotnet build -warnaserror` · `dotnet test` · `dotnet format --verify-no-changes` ·
`pnpm nx affected -t lint test build` · drift gates (`git diff --exit-code` after OpenAPI re-emit +
both `*-data-access:generate-client`). New migrations applied by the dedicated migration runner
(ADR-0033). No suppressions, no skipped tests.

## Project structure (this feature)

```text
specs/012-employee-search/
├── plan.md            # this file
├── research.md        # pg_trgm operator/threshold/accent decisions
├── data-model.md      # read shapes, cursor shapes, index/migration shape
├── contracts/         # the two endpoint contracts (q param + envelope)
└── quickstart.md      # how to validate end-to-end
```
