# Feature Specification: Identity Web (Account & Admin UI)

**Feature Branch:** `feat/005-identity-web`
**Status:** Draft
**Created:** 2026-06-09
**Updated:** 2026-06-09
**Realizes (frontend of):** `001-identity-access` stories IA-1 (admin login), IA-2 (employee login), IA-4 (provision/elevate admins), IA-6 (logout)

## Summary

The Angular SPA's identity surface: the screens that let a signed-in user see their account, and
let an administrator view accounts and elevate an employee to administrator. Authentication itself
is handled by the YARP BFF (ADR-0013) — the shell already exposes **Sign in** (`/bff/login`) and
**Sign out** (`POST /bff/logout`), and the session (`/bff/user`) drives the header. This feature
adds the **account page**, the **admin user-management page**, and the **route guards** that gate
them, all token-free through the gateway. No tokens ever reach the SPA.

The UI is **localized** (DE + EN, Transloco, no hardcoded strings — ADR-0024), **accessible**
(WCAG 2.2 AA baseline, CDK behaviours — ADR-0024), and built with **standalone, signal-based,
zoneless, OnPush** components (ADR-0016).

## User Scenarios & Testing

### Primary User Story
As a signed-in user, I want to see my account and (if I am an administrator) manage who else is an
administrator, so that the team's access reflects who should have it.

### Acceptance Scenarios

1. **See my account**
   - GIVEN a signed-in user on the account page
   - WHEN the page loads
   - THEN it shows their display name, email, and role (Employee or Administrator)

2. **Unauthenticated visitor is sent to sign in**
   - GIVEN no session
   - WHEN they navigate to a protected route (account or admin)
   - THEN they are redirected to the BFF sign-in (`/bff/login`) with a return URL back to that route

3. **Administrator sees the user list**
   - GIVEN a signed-in administrator on the admin users page
   - WHEN the page loads
   - THEN it lists every account with name, email, role, and status

4. **Administrator elevates an employee**
   - GIVEN an administrator viewing an employee in the list
   - WHEN they choose "Grant administrator" and confirm
   - THEN the account becomes an administrator and the list reflects the new role

5. **Granting is idempotent in the UI**
   - WHEN an administrator grants administrator to an account that is already an administrator
   - THEN the action succeeds with no error and the role is unchanged

6. **Employee cannot reach the admin page**
   - GIVEN a signed-in employee
   - WHEN they navigate to the admin users route
   - THEN the admin UI is not shown (they are redirected away / shown a not-authorized view), and the
     admin navigation entry is not offered

7. **Language switch localizes the identity screens**
   - WHEN the user switches language
   - THEN all account/admin labels, headings, and actions render in the chosen language (DE or EN)

### Edge Cases
- The session/account request fails or returns 401 mid-session → treat as signed out (no error dump;
  offer sign-in).
- The admin list is empty → show an empty-state message, not a blank table.
- A grant action fails (network/5xx) → show a non-blocking error and leave the row unchanged.

## Requirements

### Functional Requirements
- **FR-001:** A signed-in user MUST be able to view their own account (display name, email, role) on
  an account page, sourced from the gateway (`GET /account/me`), never from a token in the SPA.
- **FR-002:** Protected routes (account, admin) MUST require a session; an unauthenticated visitor
  MUST be redirected to `/bff/login` with a `returnUrl` back to the attempted route.
- **FR-003:** The admin user-management route MUST require the Administrator role; a non-administrator
  MUST NOT see it (redirected / not-authorized view) and MUST NOT be offered its navigation entry.
- **FR-004:** An administrator MUST be able to view the list of accounts (`GET /admin/users`) with
  name, email, role, and status.
- **FR-005:** An administrator MUST be able to grant the Administrator role to an account
  (`POST /admin/users/{id}:grant-administrator`); the result MUST update the displayed role. The
  action MUST be safe to repeat (idempotent) with no error.
- **FR-006:** All identity-screen text MUST be localized via Transloco (DE + EN); no hardcoded
  user-facing strings (ADR-0024).
- **FR-007:** The identity screens MUST meet the WCAG 2.2 AA baseline — keyboard operable, correct
  roles/names, visible focus, and a labelled, announced result for the grant action (ADR-0024).
- **FR-008:** No access token or refresh token is ever read, stored, or handled by the SPA
  (ADR-0013); all backend calls are same-origin relative URLs through the gateway (ADR-0030).

### Key Entities (view models)
- **Account** — display name, email, role (employee | administrator). Shown on the account page.
- **AdminUser** — id, name, email, role, status (provisioning | active). A row in the admin list.

## Out of Scope (this feature / deferred)
- Creating/hiring accounts from the UI (that is the organization-led `HireEmployee` saga, ADR-0025 —
  a separate feature once organization exposes hiring).
- Editing a user's email/name/password from the UI.
- Revoking administrator (no backlog story; grant is one-way in the MVP).
- Office/room/attendance screens (separate features).
- Extracting identity UI into an Nx feature lib (ADR-0016) — deferred to a dedicated structural slice
  (see plan.md decision D-FE1); this feature builds inline under `apps/web/src/app`.

## Review & Acceptance Checklist
- [ ] No implementation details (no component/service mechanics in the spec body)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] Token-free (BFF) posture is explicit
- [ ] Localization + accessibility requirements are present
- [ ] No open clarification markers remain
