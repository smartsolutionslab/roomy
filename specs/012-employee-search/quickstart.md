# Quickstart / validation: employee search (012)

How to prove the feature works end-to-end. Assumes the Aspire stack and the demo dataset (the Obex
Labs seeder — ~42 logins) per the repo's run docs.

## Prerequisites

- ADR-0047 read; migrations applied by the migration runner (ADR-0033) — `pg_trgm`/`unaccent` +
  the GIN trigram index exist in both the attendance read-model DB and the organization DB.
- Backend integration tests run against **real Postgres** (Aspire fixture) — no SQLite.

## Backend validation (the acceptance criteria, as tests)

Run the affected integration projects:

```
dotnet test --filter FullyQualifiedName~Attendance
dotnet test --filter FullyQualifiedName~Organization
```

These must cover (RED first):

1. **Typo on page 1 (SC-002)** — seed e.g. "Hannah Schmidt"; `GET /reservations/employees?q=Hanah`
   returns her within the first page.
2. **Best-match-first** — closer names rank ahead of looser ones.
3. **Stable search paging** — read page 1 with `q`, insert a new matching employee, page with
   `nextCursor`: no skip/duplicate of page-1 items.
4. **Blank `q` = today** — `?q=` (or omitted) returns the exact ADR-0044 keyset list.
5. **400s** — `q` over 100 chars, or a `cursor` whose mode doesn't match `q`, → 400.
6. **403** — a non-administrator gets 403 with and without `q`.
7. **New org directory** — `GET /employees` returns the `{ items, nextCursor }` envelope, ranks a
   typo'd query, and 403s a non-admin.

## Manual / HTTP validation

Authenticate as the seeded administrator through the gateway (BFF), then:

```
GET /reservations/employees?q=mueller        # attendance picker, fuzzy
GET /reservations/employees?q=mueller&cursor=<nextCursor from page 1>
GET /employees?q=mull                          # new org directory, fuzzy
GET /employees                                 # full directory, stable name order
```

Expect ranked matches, a `nextCursor` that pages contiguously, and `nextCursor: null` at the end.
"José" should be found by `?q=jose` (accent-folded).

## Web validation

```
pnpm nx run web:test            # @testing-library/angular specs
pnpm nx affected -t lint build
```

In the running app (admin), on each search-enabled list:

- Type a name fragment → list narrows and re-ranks; scrolling loads more in the same order.
- Clear the box → full list returns.
- Tab to the search box → it is focusable + labelled; result-count change announced (WCAG 2.2 AA).
- Switch DE/EN → label + placeholder localized, no hardcoded strings.

## Drift gate (must be green before done)

```
dotnet build Roomy.slnx -warnaserror -p:OpenApiGenerateDocumentsOnBuild=true   # re-emit specs
git diff --exit-code -- apps/attendance-api/Roomy.Attendance.Api.json apps/organization-api/Roomy.Organization.Api.json
pnpm nx run attendance-data-access:generate-client && git diff --exit-code -- libs/attendance/data-access/src/lib/generated
pnpm nx run organization-data-access:generate-client && git diff --exit-code -- libs/organization/data-access/src/lib/generated
```

## Full gate suite

```
dotnet build -warnaserror && dotnet test && dotnet format --verify-no-changes
pnpm nx affected -t lint test build
```
