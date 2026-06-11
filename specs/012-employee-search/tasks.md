# Tasks: Search employees by name (typo-tolerant) (012)

**Input:** `specs/012-employee-search/` — plan.md, spec.md, research.md, data-model.md, contracts/
**Decision:** ADR-0047 (builds on ADR-0044, ADR-0036)

**Tests are REQUIRED here**: the constitution (Principle I, non-negotiable) mandates test-first —
each acceptance criterion lands as a failing test *before* its implementation (Red → Green).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: parallelizable (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2 / US3 (from spec.md); Setup/Foundational/Polish carry no story label
- Each task names exact file paths.

## Conventions / paths

- Backend libs: `backend/libs/<context>/{domain,application,infrastructure}`; hosts: `backend/apps/<context>-api`.
- Frontend libs: `backend/libs/<context>/{feature,ui,data-access}`; SPA: `apps/web`.
- Backend tests against **real Postgres** (Aspire fixture, no SQLite). Architecture tests already
  reference attendance + organization layers — **no new project reference needed**.

---

## Phase 1: Setup (shared)

**Purpose**: shared test data the backend stories assert against.

- [x] T001 [P] Add a bulk employee test-data builder to `backend/tests/` TestSupport (e.g.
  `Roomy.TestSupport`) that seeds many employees with diverse, near-duplicate, typo-adjacent, and
  accented names (e.g. "Hannah Schmidt", "José Müller") for both the attendance read model and the
  organization `Employee` table — file under `backend/libs/test-support`/`backend/tests/...TestSupport`.

---

## Phase 2: Foundational (blocking prerequisites)

**Purpose**: the shared `q` validation both backend stories depend on. **No story work starts until
this is done.**

- [x] T002 [P] RED: unit tests for `SearchTerm` in `backend/tests/...SharedKernel.Tests/Search/SearchTermTests.cs`
  — trims input, blank/whitespace ⇒ "no filter" (empty), length > 100 ⇒ `Result` failure
  (`Error.Validation`).
- [x] T003 `SearchTerm` value object in `backend/libs/shared-kernel/src/Search/SearchTerm.cs`
  (`…SharedKernel.Search`) — `SearchTerm.From(string?) → Result<SearchTerm>`, exposes `IsEmpty` and
  the normalized term; `Ensure.That(...)` guards (GREEN for T002).

**Checkpoint**: `q` validation reusable by both contexts.

---

## Phase 3: User Story 1 — Find an employee to act on behalf of (P1) 🎯 MVP

**Goal**: `GET /reservations/employees?q=` returns typo-tolerant, best-match-first matches, paged;
blank `q` is unchanged.
**Independent test**: seed many employees; call the endpoint as admin with a typo'd `q` → intended
employee on page 1; non-admin → 403.

### Tests (RED first)

- [x] T004 [P] [US1] RED integration tests in `backend/tests/attendance-integration/EmployeesEndpointTests.cs`:
  (a) single-typo `q` returns the intended employee on page 1 (SC-002); (b) results ordered
  most-similar first; (c) search paging stable across an insert (page 1, insert a matching employee,
  page via `nextCursor` — no skip/duplicate); (d) blank/omitted `q` reproduces the existing
  ADR-0044 keyset list verbatim; (e) `q` > 100 chars ⇒ 400; (f) a cursor whose mode mismatches `q`
  ⇒ 400; (g) non-admin ⇒ 403 with and without `q`.

### Implementation (GREEN)

- [x] T005 [US1] EF migration on the **attendance read-model DB** in
  `backend/libs/attendance/infrastructure/Persistence/Migrations/` — `CREATE EXTENSION IF NOT EXISTS pg_trgm`
  + `unaccent`, the `immutable_unaccent(text)` IMMUTABLE wrapper, and a GIN trigram index on
  `immutable_unaccent(display_name)` (research.md R3).
- [x] T006 [US1] Extend the read port to carry the term:
  `IEmployeeCatalog.GetAsync(SearchTerm term, PageRequest request, ct)` in
  `backend/libs/attendance/application/Ports/IEmployeeCatalog.cs`.
- [x] T007 [US1] In `backend/libs/attendance/infrastructure/ReadModels/Employees/EmployeeCatalog.cs`: add the
  `EmployeeSearchCursor(double Similarity, string Name, Guid EmployeeId)` record and the non-blank-`q`
  raw-SQL branch — `@q <% immutable_unaccent(display_name)` keyset on
  `(word_similarity DESC, display_name, employee_id)` per data-model.md; blank `q` keeps the existing
  `(display_name, employee_id)` query.
- [x] T008 [US1] Thread the term through `ViewEmployees` + handler in
  `backend/libs/attendance/application/UseCases/ViewEmployees.cs` and `ViewEmployeesHandler.cs`.
- [x] T009 [US1] Add optional `q` to `ViewEmployeesAsync` in
  `backend/apps/attendance-api/Endpoints/ReservationEndpoints.cs` — parse via `SearchTerm.From`, 400 on
  failure; keep `operationId` `ViewEmployees` and the `EmployeePage` response (no schema drift).
- [x] T010 [US1] Re-emit attendance OpenAPI (`backend/apps/attendance-api/Roomy.Attendance.Api.json`) +
  `pnpm nx run attendance-data-access:generate-client`; commit regenerated client (drift gate).

**Checkpoint**: US1 independently shippable (MVP). Tests T004 green.

---

## Phase 4: User Story 2 — Find an employee in the organization directory (P2)

**Goal**: new `GET /employees` (admin-only) lists + searches the organization `Employee` master data
in the same envelope.
**Independent test**: hire several employees; `GET /employees?q=<typo>` as admin → ranked matches;
non-admin → 403; blank `q` → stable name order.

### Tests (RED first)

- [ ] T011 [P] [US2] RED integration tests in `backend/tests/organization-integration/EmployeeEndpointsTests.cs`:
  `GET /employees` returns `{ items, nextCursor }`; ranks a typo'd `q` (SC-002); blank `q` lists in
  stable name order; `q` > 100 / bad cursor ⇒ 400; non-admin ⇒ 403; `POST /employees` unaffected.

### Implementation (GREEN)

- [ ] T012 [US2] EF migration on the **organization DB** in
  `backend/libs/organization/infrastructure/Persistence/Migrations/` — `pg_trgm` + `unaccent` +
  `immutable_unaccent` wrapper + GIN trigram index on `immutable_unaccent(name)`.
- [ ] T013 [US2] New read port `IEmployeeDirectory.SearchAsync(SearchTerm term, PageRequest request, ct)
  → Result<Page<EmployeeListItem>>` and `EmployeeListItem(EmployeeIdentifier Employee, string Name)`
  in `backend/libs/organization/application/Ports/` + `…/UseCases/`. Write-side `IEmployeeRepository`
  untouched.
- [ ] T014 [US2] Infra impl `EmployeeDirectory` (raw-SQL keyset + word-similarity, same shape as
  attendance) in `backend/libs/organization/infrastructure/Persistence/EmployeeDirectory.cs`; register in the
  organization composition root DI.
- [ ] T015 [US2] `ListEmployees` query + handler in `backend/libs/organization/application/UseCases/`.
- [ ] T016 [US2] New `GET /employees` in `backend/apps/organization-api/Endpoints/EmployeeEndpoints.cs` —
  admin-only (403 otherwise), optional `q` via `SearchTerm.From` (400 on failure), concrete
  `EmployeePage`/`EmployeeResponse` records, `operationId` `ListEmployees`. `POST /employees`
  unchanged.
- [ ] T017 [US2] Re-emit organization OpenAPI (`backend/apps/organization-api/Roomy.Organization.Api.json`) +
  `pnpm nx run organization-data-access:generate-client`; commit (drift gate).

**Checkpoint**: US2 independently shippable.

---

## Phase 5: User Story 3 — Search from the web app (P3)

**Goal**: an accessible, DE/EN search box on both list surfaces over the existing `@roomy/shared-ui`
infinite list; typing filters/ranks, clearing restores, scrolling appends.
**Independent test**: render each view, type → ranked results; clear → full list; tab → labelled,
operable; DE/EN labels render.

### Tests (RED first)

- [ ] T018 [P] [US3] RED `@testing-library/angular` specs for the **attendance** picker search box
  (typing filters + re-ranks, clearing restores full list, scroll appends next page in similarity
  order, control focusable + labelled, DE/EN) in `libs/attendance/feature/...spec.ts`.
- [ ] T019 [P] [US3] RED specs for the **organization** directory view (same behaviours) in
  `libs/organization/feature/...spec.ts`.

### Implementation (GREEN)

- [ ] T020 [US3] Extend the attendance employee-picker data-access facade with the `q` param and
  **reset the cursor when `q` changes** in `libs/attendance/data-access/`.
- [ ] T021 [US3] Add an organization employees data-access facade for `GET /employees` (`q` + cursor)
  in `libs/organization/data-access/`.
- [ ] T022 [US3] Add a debounced search box (signal `input`) to the attendance on-behalf picker view
  in `libs/attendance/feature/`, wired to the `@roomy/shared-ui` infinite list.
- [ ] T023 [US3] Add the organization directory feature view + route with the same search box in
  `libs/organization/feature/` (+ route registration in `apps/web`).
- [ ] T024 [P] [US3] Add DE + EN Transloco keys (label, placeholder, "no results", result-count
  announcement) for both search boxes; verify WCAG 2.2 AA labelling/announcement (ADR-0024).

**Checkpoint**: US3 shippable; all three stories integrated.

---

## Phase 6: Polish & cross-cutting

- [ ] T025 [P] Lock the word-similarity threshold to SC-002: set the per-request
  `pg_trgm.word_similarity_threshold` (via `set_limit()`/GUC) in both read models and pin the value
  with the SC-002 assertion (research.md R5).
- [ ] T025a [P] Assert **SC-004** (no unbounded scan): in the attendance + organization integration
  tests, seed a large employee set and assert the searched query is **GIN-index-served** — e.g.
  `EXPLAIN (ANALYZE, FORMAT JSON)` on the search SQL shows a Bitmap Index Scan on the trigram index
  (not a Seq Scan) — so the candidate set stays index-bounded at scale, not just that the index
  exists (T005/T012).
- [ ] T026 [P] Run the full gate suite — `dotnet build -warnaserror`, `dotnet test`,
  `dotnet format --verify-no-changes`, `pnpm nx affected -t lint test build`, and both drift gates
  (`git diff --exit-code` after OpenAPI re-emit + both `generate-client`). Fix causes, no
  suppressions. Run prettier `--write` on changed FE files before committing.
- [ ] T027 Verify the push-to-main CI run after merge (not just the PR run), incl. the saga-isolated
  steps and codegen-verify, are green.

---

## Dependencies & order

- **Setup (T001)** → everything that asserts on seeded data.
- **Foundational (T002–T003 `SearchTerm`)** → blocks both backend stories (T006/T009, T013/T016).
- **US1 (P1)** is the MVP and depends only on Setup + Foundational.
- **US2 (P2)** depends on Setup + Foundational; **independent of US1** (different context/files) — can
  run in parallel with US1 once Foundational is done.
- **US3 (P3)** depends on the endpoints it consumes: the attendance half needs US1 (T010 client); the
  organization half needs US2 (T017 client). T018/T020/T022 follow US1; T019/T021/T023 follow US2.
- **Polish (T025–T027)** last.

## Parallel opportunities

- After Foundational: **US1 and US2 backends run in parallel** (separate contexts, separate migrations
  and test files) — e.g. one agent on T004–T010, another on T011–T017.
- Within a story, `[P]` tasks touch different files: T018 ∥ T019; T024 ∥ the view tasks.
- Migrations T005 ∥ T012 (different databases).

## Implementation strategy

- **MVP = US1** (attendance picker search) — the surface with real pain today; ship it first behind
  the existing endpoint.
- Then **US2** (new org directory) reusing US1's read/SQL pattern, then **US3** (web) on top.
- Per golden rule 5, the three stories may land as **separate atomic commits/PRs** off
  `feat/012-employee-search` to keep each review small.
