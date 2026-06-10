# Implementation Plan: Attendance On-Behalf (Administrator acts for an employee)

**Branch**: `feat/009-attendance-on-behalf` | **Date**: 2026-06-10 | **Spec**: `specs/009-attendance-on-behalf/spec.md`

## Summary

Add an admin-only **on-behalf page**: pick an employee, reserve a place for them, and view/cancel their
reservations. The backend already authorises on-behalf reserve (`onBehalfOf`) and admin cancel; this
slice adds two administrator-gated **reads** on `attendance-api` and the SPA page. Extends `007`'s
`@roomy/attendance-data-access` and `@roomy/attendance-feature`; no new lib; no new ADR (applies
ADR-0036/0035/0040/0013/0016/0024/0030).

## Key decisions

### D-OB1 — Two admin reads under the existing `/reservations` route
`/employees` is owned by organization at the gateway, so attendance exposes the directory under its own
`/reservations` surface (no new gateway route, no collision):
- `GET /reservations/employees` → `[{ employeeId, name }]` from attendance's `Employees` read model
  (`ViewEmployees` query + `IEmployeeCatalog` port + adapter).
- `GET /reservations/by-employee/{employeeId:guid}` → reuses the existing `ViewMyReservations(employee)`
  query for the chosen employee.
Both **require the `administrator` realm role server-side** (403 otherwise) — not merely UI-hidden
(FR-009). Annotate with `.WithName/.Produces/.ProducesProblem`; re-emit + drift-gate the spec/client.

### D-OB2 — Reuse the `007` reserve flow via an `onBehalfOf` input
`ReservePage` gains `onBehalfOf = input<string | null>(null)` (null ⇒ self). When set, `reserve()` passes
it to `AttendanceGateway.reserve(..., onBehalfOf)`. The on-behalf page embeds `<roomy-reserve-page
[onBehalfOf]="selectedEmployeeId()">`, so the office→room→day flow, remaining-places, and error
surfacing (FR-005) are reused unchanged.

### D-OB3 — Admin-gated page + nav
`OnBehalfPage` is routed at `/attendance/on-behalf` behind `authGuard` + `adminGuard` (ADR-0040). The nav
entry shows only for administrators (like the existing Offices/Admin entries). The page hosts: an
employee `<select>` (`listEmployees`); when chosen, the embedded reserve flow + the employee's
reservations (`reservationsFor`) with cancel on upcoming rows (admin cancel is already authorised).

## Technical Context
Angular 22 standalone/signal/zoneless/OnPush; `@jsverse/transloco`; `SessionService` from
`@roomy/shared-data-access` is not needed in the page (the route guard enforces admin) — the picker is
admin-only by routing. Tests: `@testing-library/angular` + `vitest-analog`; backend host + read-model
tests in xUnit + Shouldly.

## Constitution Check
| Principle | Verdict | Notes |
|---|---|---|
| I. Spec-Driven & Test-First | ✅ | testable AC; read-model/endpoint + component specs precede code. |
| II. Clean Arch & DDD | ✅ | `Employee` view model + `EmployeeId` brand at the boundary; `ViewEmployees` query over the read model; no domain change. |
| III. Context Isolation | ✅ | Reads attendance's own read models; everything stays `context:attendance` (+ `context:shared` guards). |
| IV. No Framework in Core | ✅ | Host endpoint + application query/port only. |
| V. Decisions Recorded | ✅ | No new ADR; decisions captured here (D-OB1..3). |
| VI. Green Before Done | ✅ | `dotnet build -warnaserror`/`test`/`format`; `nx affected -t lint test build`. |
| VII. Small, Single-Purpose | ✅ | Backend reads, data-access, page, wiring as separate commits. |

**Gate: PASS.**

## Project Structure (this feature)
```text
libs/attendance/application/
├─ UseCases/ViewEmployees.cs + EmployeeView.cs + ViewEmployeesHandler.cs   # NEW directory query
└─ Ports/IEmployeeCatalog.cs                                               # NEW
libs/attendance/infrastructure/ReadModels/Employees/EmployeeCatalog.cs     # NEW adapter (+ DI)
apps/attendance-api/Endpoints/ReservationEndpoints.cs                      # + GET /reservations/employees, /by-employee/{id} (admin)
libs/attendance/data-access/src/lib/
├─ booking.ts             # + EmployeeId brand
├─ employee.ts            # Employee view model + toEmployee (+spec)
└─ attendance-gateway.ts  # + listEmployees(), reservationsFor(employeeId), reserve(..., onBehalfOf?) (+spec)
libs/attendance/feature/src/lib/
├─ reserve/reserve-page.ts        # + onBehalfOf input, passed to reserve()
├─ on-behalf/on-behalf-page.*     # NEW admin page (+spec)
└─ attendance.routes.ts           # + on-behalf route (authGuard + adminGuard)
apps/web/src/app/app.html         # + admin-only On-behalf nav entry
apps/web/public/i18n/{en,de}.json # + attendance.onBehalf.* (DE/EN parity)
```

## Phasing (see tasks.md)
1. **Backend reads** — `ViewEmployees` (+port/adapter/DI) + `GET /reservations/employees` and
   `GET /reservations/by-employee/{id}`, both admin-gated; annotate + re-emit spec; host + read-model tests.
2. **Data-access** — `Employee`/`EmployeeId` + `listEmployees`/`reservationsFor` + `reserve(onBehalfOf?)`; specs.
3. **Reserve input** — `ReservePage` `onBehalfOf` input (self-service unchanged); spec.
4. **On-behalf page** — employee picker + embedded reserve + employee's reservations with cancel; spec.
5. **Wiring & polish** — adminGuard route, admin-only nav, `attendance.onBehalf.*` DE/EN i18n, WCAG.

## Notes
- RED→GREEN throughout; commit per phase.
- Server-side admin gate is the real control; the route guard + nav visibility are convenience.
- No new gateway route; `/reservations/{**}` already forwards the new sub-paths.
