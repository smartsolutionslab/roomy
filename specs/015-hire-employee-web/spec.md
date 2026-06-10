# Feature Specification: Hire Employee Web (admin hiring page)

**Feature Branch:** `feat/015-hire-employee-web`
**Status:** Draft
**Created:** 2026-06-11
**Updated:** 2026-06-11
**Realizes (frontend of):** `008-hire-employee` — User Story 1 (hire a colleague and have their account
provisioned). The backend, the User↔Employee provisioning saga (ADR-0025), and the typed client
(`hireEmployee`, `HireEmployeeRequest`, `HiredEmployeeResponse` in `@roomy/organization-api`) are already
built; `008` explicitly deferred the admin UI to "a separate feature" — this is that feature.

## Summary

The administrator-only screen that lets an admin **hire a colleague**: a form for the colleague's display
name, work email, role (Employee or Administrator), and an initial password. Submitting calls
`POST /employees` through the gateway (ADR-0013/0030). Hiring **records the employee immediately in a
provisioning state and starts account provisioning**; the colleague can sign in **once provisioning
completes** — eventual consistency, not within the request (ADR-0025, `008` FR-004). The page makes this
explicit: a successful hire is acknowledged as *provisioning started*, not *ready to sign in*.

The endpoint answers **202 Accepted** with `{ employeeId, userId, state: "Provisioning" }` on success and
**400** for a missing/invalid field (bad email, empty name, unknown role, empty password). It does **not**
synchronously detect an email already in use — that surfaces asynchronously during provisioning and is not
observed here (`008` contract) — so this page must **not** present an "email already taken" result at hire
time. The route is gated to administrators (`authGuard + adminGuard`); a non-administrator never reaches it,
and the API independently returns **403** (handled defensively).

This follows the established `*-web` slice shape (006/007/008-web): a thin **`EmployeesGateway`** facade in
`@roomy/organization-api` wrapping the generated `hireEmployee` and mapping the DTO to a branded view model
at the boundary (ADR-0020); a routed, admin-gated **`HireEmployeePage`** (`organization-feature`) built from
the shared design system (`roomy-page`, `roomy-form-field`, `roomy-select`, `roomy-message`, `roomyButton`);
and a **navigation entry** declared on the route via `data.nav` (ADR-0050), so the sidebar and dashboard pick
it up automatically. It is standalone/signal-based/zoneless/OnPush (ADR-0016/0035), localized DE + EN
(Transloco, ADR-0024), accessible (WCAG 2.2 AA, ADR-0024), and same-origin with no token in the SPA
(ADR-0013/0030). The typed client stays generated and drift-gated (ADR-0036).

> **Not here:** no employee **list** or provisioning-state **read** surface (`GET /employees` is a later
> feature per the `008` contract), so the page cannot show an employee's convergence to *active*/*failed* —
> only that provisioning has started. No employee edit/offboarding. No backend change (the hiring endpoint
> and saga already exist).

## User Scenarios & Testing

### Primary User Story

As an administrator, I want to hire a colleague by entering their details, so that they are recorded and a
login account is provisioned for them to sign in with once it is ready.

### Acceptance Scenarios

1. **Hire a colleague with valid details**
   - GIVEN a signed-in administrator on the hire page
   - WHEN they enter a display name, a well-formed work email, choose the Employee role, and an initial
     password, and submit
   - THEN `POST /employees` is called with exactly those values
   - AND on **202** the page confirms the colleague was hired and account provisioning has started (they can
     sign in once it completes), and the form is cleared for the next hire

2. **Choose the Administrator role**
   - GIVEN the role selector
   - WHEN the administrator selects Administrator and submits an otherwise valid hire
   - THEN the request body carries `role: "Administrator"`

3. **Required fields are enforced before calling the API**
   - GIVEN the form with a missing display name, email, or initial password
   - WHEN the administrator tries to submit
   - THEN no request is made and the missing fields are indicated

4. **A malformed email is rejected before calling the API**
   - GIVEN an email that is not well-formed
   - WHEN the administrator tries to submit
   - THEN no request is made and the email is indicated as invalid

5. **The server rejects the hire as invalid**
   - GIVEN a submitted hire
   - WHEN the API answers **400**
   - THEN the page shows a validation error and does not claim the colleague was hired

6. **The hire fails unexpectedly**
   - GIVEN a submitted hire
   - WHEN the API answers with an unexpected error (5xx/offline) or **403**
   - THEN the page shows a generic failure message and does not claim the colleague was hired

### Edge Cases

- **Email already in use:** not detectable at hire time (it fails later, asynchronously, during
  provisioning and is not observed by this endpoint). The page does **not** present an "email taken" result;
  a successful **202** is honestly reported as *provisioning started*.
- **Convergence window:** between hire and provisioning completion the colleague exists but cannot yet sign
  in. The success message communicates this; the page does not poll for or assert *active*.
- **Non-administrator:** cannot reach the route (admin guard). If the API is hit directly and returns 403,
  it is treated as a generic failure (scenario 6) — the UI never implies a non-admin can hire.

## Requirements

### Functional Requirements

- **FR-1** The page MUST collect a display name, work email, role (Employee/Administrator), and initial
  password, and submit them to `POST /employees` via the typed gateway.
- **FR-2** The role MUST be chosen from exactly {Employee, Administrator} and sent as the contract's string
  value (`"Employee"` / `"Administrator"`).
- **FR-3** The page MUST validate required fields and email well-formedness client-side and MUST NOT call the
  API while the form is invalid.
- **FR-4** On **202** the page MUST acknowledge the hire as *recorded, provisioning started* (sign-in
  available once provisioning completes), and reset the form. It MUST NOT state the account is ready.
- **FR-5** On **400** the page MUST show a validation error; on any other error (incl. 403) a generic failure
  message. In neither case may it claim the colleague was hired.
- **FR-6** The page MUST NOT present an "email already in use" outcome (not observable at hire time).
- **FR-7** The hiring route MUST be gated to administrators (`authGuard + adminGuard`) and MUST declare a
  `data.nav` entry (ADR-0050) so it appears in the admin navigation and dashboard.
- **FR-8** All text MUST be localized (DE + EN); the screen MUST meet the WCAG 2.2 AA baseline (labelled
  fields, error association, focus management consistent with the other forms).

### Non-Functional / Constraints

- **`EmployeesGateway`** lives in `@roomy/organization-api` (`type:api`, `context:organization`) and maps the
  generated `HiredEmployeeResponse` to a branded `HiredEmployee` view model (`EmployeeId`, `UserId`, `state`)
  at the boundary (ADR-0020); the page never touches the generated DTO. No Nx boundary is crossed
  (`feature → ui/api/util`, `api → util`).
- Same-origin relative URL through the gateway; the BFF forwards the token, the SPA holds none
  (ADR-0013/0030).

## Key Entities

- **EmployeeRole** — `'Employee' | 'Administrator'`, the contract's hiring roles.
- **HiredEmployee** — the boundary view model of a 202 hire: branded `employeeId`, `userId`, and `state`
  (`"Provisioning"`).

## Review & Acceptance Checklist

- [ ] Every acceptance scenario has a test written before its implementation.
- [ ] Valid hire posts the exact `{ displayName, email, role, initialPassword }` and honours 202 as
      *provisioning started* (never "ready"); form resets.
- [ ] Required-field and email validation block the API call; 400 → validation error; other/403 → generic
      failure; no "email already in use" outcome anywhere.
- [ ] Route is admin-gated and declares `data.nav`; the link appears for admins in sidebar + dashboard and is
      absent for employees.
- [ ] DE + EN render; no hardcoded strings; no a11y regressions.
- [ ] All quality gates green; no suppressions; client stays generated/drift-gated.
