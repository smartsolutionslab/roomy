# Feature Specification: Search employees by name (typo-tolerant)

**Feature Branch:** `feat/012-employee-search`
**Status:** Draft
**Created:** 2026-06-10
**Updated:** 2026-06-10

## Summary

Two employee lists grow with company size and are hard to use once a company has more than a
screenful of people: the **attendance on-behalf employee picker** (`GET /reservations/employees`,
administrator-only — 009) an administrator scrolls to choose a colleague to reserve for, and the
**organization employee directory** (which has no list endpoint today — the Employee aggregate host
only exposes `POST /employees` to hire). This feature lets a user **find an employee by name** in
both places by typing a query, rather than scrolling.

Matching is **typo-tolerant** (fuzzy) and case-insensitive: a query finds the colleagues whose names
are *most similar* to it — tolerating a transposed letter, a missing accent, or a partial token — and
returns them **best match first**. An empty or blank query returns the unfiltered list exactly as
today (the existing keyset order, ADR-0044). Search composes with the existing cursor pagination
envelope `{ items, nextCursor }`: a long result set still pages as the user scrolls.

The two surfaces stay in their own bounded contexts and own databases — the attendance picker
searches attendance's own `Employees` read model; the organization directory searches the
organization context's `Employee` master data. Neither queries the other (ADR-0014). The web app
gains an accessible, localized (DE + EN) search box on each surface. OpenAPI specs and the generated
Angular clients are regenerated and drift-gated (ADR-0036).

## Affected endpoints

- `GET /reservations/employees` (attendance, administrator-only) — **gains** an optional `q` search
  parameter. Without `q`: unchanged (keyset order `(Name, EmployeeId)`). With `q`: results ranked by
  name similarity to `q`.
- `GET /employees` (organization, **new**, administrator-only) — a paginated employee directory with
  the same optional `q` search parameter and the same `{ items, nextCursor }` envelope. (The existing
  `POST /employees` is unchanged.)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Find an employee to act on behalf of (Priority: P1)

An administrator on the on-behalf reservation page has dozens or hundreds of colleagues to choose
from. They type part of a name into a search box and the list narrows to the closest matches, best
match first, so they can pick the right person in a couple of keystrokes instead of scrolling.

**Why this priority**: This is the surface where the pain is real today (009 already ships the picker
and 011 already made it endless because it grows). Search is the natural completion of that list and
the highest-value slice.

**Independent Test**: Seed many employees, call `GET /reservations/employees?q=<query>` as an
administrator, and assert the response contains the intended employee near the top even when the
query has a typo, and that a non-administrator is still forbidden.

**Acceptance Scenarios**:

1. **Given** a company with many employees, **When** an administrator searches the picker for a name
   fragment, **Then** the response contains only employees whose names are similar to the query,
   ordered most-similar first.
2. **Given** a query with a single-character typo or transposition (e.g. "Hannah" → "Hanah"),
   **When** the administrator searches, **Then** the intended employee is still returned and ranked
   highly.
3. **Given** a search that matches more employees than one page, **When** the administrator scrolls,
   **Then** further matches load via `nextCursor` in the same similarity order, with no gap or
   duplicate, until exhausted (`nextCursor: null`).
4. **Given** an empty or whitespace-only `q`, **When** the picker is called, **Then** it returns the
   full directory in the existing keyset name order (behaviour unchanged from 009/011).
5. **Given** a non-administrator, **When** they call the endpoint with or without `q`, **Then** they
   receive 403 (search adds no access).

---

### User Story 2 - Find an employee in the organization directory (Priority: P2)

An administrator managing master data wants to look up a specific colleague by name from the
organization's employee directory — which currently has no way to list or find employees at all.

**Why this priority**: It opens a directory surface the organization context does not yet expose, so
it carries more new scope (a new endpoint and its web view) than Story 1. It reuses the same search
and pagination contract, so it is cheap once Story 1's pattern exists.

**Independent Test**: Hire several employees, call the new `GET /employees?q=<query>` as an
administrator, and assert similarity-ranked matches are returned in the `{ items, nextCursor }`
envelope; assert a non-administrator is forbidden.

**Acceptance Scenarios**:

1. **Given** hired employees, **When** an administrator calls `GET /employees` with no `q`, **Then**
   the directory is returned in a stable name order with the `{ items, nextCursor }` envelope.
2. **Given** a name query, **When** an administrator calls `GET /employees?q=<query>`, **Then** only
   similar employees are returned, best match first, tolerating a typo as in Story 1.
3. **Given** a non-administrator, **When** they call `GET /employees`, **Then** they receive 403.

---

### User Story 3 - Search from the web app (Priority: P3)

A user on either web surface sees a search box above the list. As they type, the list updates to the
closest matches; clearing the box restores the full list. The search box is keyboard- and
screen-reader-operable and available in German and English.

**Why this priority**: The UI is the user-facing payoff, but it depends on Stories 1–2 existing on
the server first; it is the last slice.

**Independent Test**: Render each list view, type a query, and assert the list reflects the matching
results and restores on clear; verify the control is reachable by keyboard, labelled for screen
readers, and renders in both locales.

**Acceptance Scenarios**:

1. **Given** a list view with a search box, **When** the user types a name fragment, **Then** the
   list shows the matching employees, best match first.
2. **Given** an active search, **When** the user clears the box, **Then** the full list returns.
3. **Given** a search returning more than one page, **When** the user scrolls, **Then** further
   matches load (endless scroll, 011) in the same order.
4. **Given** a keyboard or screen-reader user, **When** they reach the search box, **Then** it is
   focusable, labelled, and its result-count change is announced.
5. **Given** the locale is German, **When** the search box renders, **Then** its label and
   placeholder are in German; English in the English locale (no hardcoded strings).

---

### Edge Cases

- **No matches** → `{ items: [], nextCursor: null }`; the web view shows its empty-state.
- **Empty / whitespace `q`** → treated as "no filter": the full list in the existing keyset order.
- **Very short query (1 character)** → still searched; it may match broadly, which is acceptable.
- **Query longer than a reasonable name / oversized input** → rejected with 400 (bounded input,
  no unbounded scan).
- **Accents / casing** ("José" vs "jose") → matched; comparison is case- and accent-insensitive.
- **A match deleted between fetches mid-scroll** → paging resumes after that position, no error
  (keyset semantics from 011 preserved).
- **`401` mid-search** → treated as signed out (existing behaviour), loading halts.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Both `GET /reservations/employees` and the new `GET /employees` MUST accept an optional
  `q` query parameter and, when it is non-blank, return only employees whose names are similar to `q`,
  ordered by descending similarity (best match first).
- **FR-002**: Matching MUST be typo-tolerant (a single transposition / insertion / deletion still
  matches the intended name) and case- and accent-insensitive.
- **FR-003**: A blank or omitted `q` MUST return the unfiltered list in the existing keyset order
  (`(Name, EmployeeId)` for the picker), preserving 009/011 behaviour exactly.
- **FR-004**: Search MUST compose with cursor pagination — results return in the `{ items, nextCursor }`
  envelope and page stably across scroll in the similarity order, never materialize-then-slice
  beyond what ADR-0044 already permits.
- **FR-005**: An over-long or otherwise malformed `q` MUST be rejected with 400; the server MUST NOT
  run an unbounded scan or silently truncate the query.
- **FR-006**: The new `GET /employees` MUST live in the organization context against its own Employee
  master data; the picker search MUST run against attendance's own `Employees` read model. Neither
  surface queries the other context's data (ADR-0014).
- **FR-007**: Both endpoints MUST remain administrator-only; search adds no new access (403 for a
  non-administrator, unchanged from 009).
- **FR-008**: The OpenAPI specs MUST be re-emitted and the generated Angular clients regenerated; the
  drift gate MUST stay green (ADR-0036).
- **FR-009**: Each web surface MUST present a search box that filters its list as the query changes
  and restores the full list when cleared, appending further pages on scroll (011).
- **FR-010**: The search UI MUST meet WCAG 2.2 AA (labelled, keyboard-operable control; result
  changes announced) and be localized DE + EN with no hardcoded strings (ADR-0024).

### Key Entities

- **Employee (picker view)** — the attendance `Employees` read-model row already returned by the
  picker (`EmployeeId`, `Name`); search ranks these by name similarity.
- **Employee (organization directory)** — the organization `Employee` master-data record
  (`EmployeeId`, display name); the new directory list exposes the same `{ EmployeeId, Name }` shape.
- **Search query (`q`)** — a bounded, free-text name fragment; blank means "no filter".

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From a directory of at least 500 employees, a user finds and selects a specific
  colleague by name in under 10 seconds, without scrolling the full list.
- **SC-002**: A name query with one typo (one inserted, deleted, or transposed character) still
  returns the intended employee within the first page of results in at least 95% of cases.
- **SC-003**: Clearing the search box restores the full, unfiltered list in 100% of cases (no
  residual filter).
- **SC-004**: Both list endpoints continue to return their first page within the same latency budget
  as the unsearched list at 10,000 employees (search adds no unbounded scan).
- **SC-005**: The search control is fully operable by keyboard and screen reader and renders correctly
  in both German and English.

## Assumptions

- Search is **by name only** for v1 — not by email, role, office, or any other attribute. Those are
  out of scope (see below).
- "Name" is the employee's single display name as already stored and returned today; no split into
  first/last name is introduced.
- Both surfaces are **administrator-only**, consistent with the existing picker (009) and the
  hiring endpoint (008). No employee-facing employee search is introduced in v1.
- The fuzzy ranking is "good enough to find the right person", not a tuned relevance product; exact
  similarity thresholds are an implementation/plan concern, not a spec guarantee beyond SC-002.
- Endless-scroll list and pagination primitives from 011 (cursor envelope, accessible infinite list)
  are reused rather than re-invented.

## Out of Scope (this feature / deferred)

- Searching by any field other than name (email, role, office, room).
- Filtering/faceting (by office, by role) and sorting controls beyond best-match ranking.
- An employee-facing (non-administrator) people search.
- Editing or managing employees from the new organization directory — it is **read/search only** in
  this slice (hiring stays `POST /employees`).
- Highlighting matched substrings in the UI and search-as-you-type analytics.

## Review & Acceptance Checklist

- [ ] No implementation details in the spec body beyond the ADR contracts it realizes (0014/0024/0036/0044)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] Typo tolerance asserted (SC-002 / FR-002) against real Postgres
- [ ] Blank `q` preserves existing keyset behaviour; 400 on over-long `q`
- [ ] Search composes with the `{ items, nextCursor }` envelope and stable scroll
- [ ] Each context searches only its own data (no cross-context query)
- [ ] Admin-gated reads stay admin-gated
- [ ] OpenAPI + generated clients regenerated, drift-gated
- [ ] Web search accessible + localized DE + EN
- [ ] No open clarification markers remain
