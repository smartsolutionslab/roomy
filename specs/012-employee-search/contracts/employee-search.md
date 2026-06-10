# API contracts: employee search (012)

These describe the HTTP contract the code must emit. The OpenAPI JSON is generated from the .NET
endpoints at build time (ADR-0036) and drift-gated — these docs are the intent the emitted spec and
generated Angular clients must match. Both endpoints are **administrator-only** and reuse the
ADR-0044 `{ items, nextCursor }` envelope.

## Common query parameters

| Param | Type | Rules |
|---|---|---|
| `q` | string, optional | Trimmed. Blank/omitted ⇒ unfiltered list (existing keyset order). Length > 100 ⇒ 400. |
| `cursor` | string, optional | Opaque (ADR-0044). Absent ⇒ first page. Malformed / wrong-mode for current `q` ⇒ 400. |
| `limit` | int, optional | Default 50, max 100, min 1; out of range ⇒ 400 (ADR-0044). |

Response body (both): `EmployeePage { items: EmployeeResponse[], nextCursor: string | null }`,
`EmployeeResponse { employeeId: guid, name: string }`. `nextCursor` is null exactly at end of list.

## 1. `GET /reservations/employees` (attendance) — **extended**

Existing on-behalf picker (009/011) gains the optional `q`. Behaviour:

- No `q` → unchanged: keyset on `(Name, EmployeeId)`.
- With `q` → only employees whose names are similar to `q`, ordered most-similar first; pages in the
  same similarity order via `nextCursor`.

| Status | When |
|---|---|
| 200 | `EmployeePage` (filtered+ranked when `q` present; full list otherwise). |
| 400 | bad `limit`, malformed/mode-mismatched `cursor`, or `q` over length. |
| 403 | caller is not an administrator (unchanged). |

`operationId` stays `ViewEmployees` (no client method rename / drift).

## 2. `GET /employees` (organization) — **new**

A new read-only employee directory in the organization context (the host today exposes only
`POST /employees`). Same parameters, same envelope.

- No `q` → the directory in a stable name order.
- With `q` → similarity-ranked matches, paged.

| Status | When |
|---|---|
| 200 | `EmployeePage`. |
| 400 | bad `limit`, malformed `cursor`, or `q` over length. |
| 403 | caller is not an administrator. |

`operationId`: `ListEmployees` (distinct from `HireEmployee` on `POST /employees`). The existing
`POST /employees` (hire, 008) is unchanged.

## Drift gate

After implementing, re-emit specs and regenerate clients; CI asserts no diff:

```
apps/attendance-api/Roomy.Attendance.Api.json
apps/organization-api/Roomy.Organization.Api.json
pnpm nx run attendance-data-access:generate-client
pnpm nx run organization-data-access:generate-client
```
