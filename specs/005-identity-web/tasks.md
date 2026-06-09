---
description: "Task list for Identity Web — Account & Admin UI (005)"
---

# Tasks: Identity Web (Account & Admin UI)

**Input**: Design documents in `specs/005-identity-web/` (plan.md, spec.md)

**Tests**: REQUIRED. Each acceptance criterion becomes a failing `@testing-library/angular` spec
before the component/guard exists.

**Organization**: Grouped by user story; each is independently testable.

## Story label map

| Label | Story | Scenarios | Priority |
|---|---|---|---|
| US1 | Account page | 1, 7 | P1 (MVP) |
| US2 | Route guards (auth + redirect) | 2 | P1 (MVP) |
| US3 | Admin user list | 3, 6, edge: empty | P2 |
| US4 | Grant administrator | 4, 5, edge: failure | P2 |

## Format: `[ID] [P?] [Story] Description with file path`

- **[P]**: parallelizable (different files, no incomplete dependency)
- All backend calls are relative URLs through the gateway; no tokens in the SPA.

---

## Phase 1: Setup / Foundational

- [x] T001 [P] Typed view models + data-access clients in `apps/web/src/app/identity/` — `account.ts`
  (`Account`, `AdminUser`, `AccountRole`), `account-client.ts` (`GET /account/me`), `admin-users-client.ts`
  (`GET /admin/users`, `POST /admin/users/{id}:grant-administrator`). Injectable, `HttpClient`, relative
  URLs. Spec: each client calls the expected URL and maps the response (HttpTestingController).
- [x] T002 [P] Extend `SessionService` (D-FE3) with a readiness signal so guards can await the first
  session resolution; `load()` flips it once `/bff/user` resolves (success or 401). Spec covers both.
- [x] T003 Add the gateway `/admin/{**}` YARP route in `apps/gateway/appsettings.json` (mirror
  `identity-account`: cluster `identity`, `AuthorizationPolicy: default`). Backend enablement for US3/US4.

---

## Phase 2: US1 — Account page (P1, MVP)

**Independent Test**: a signed-in user on `/account` sees display name, email, and localized role;
switching language re-renders labels.

- [x] T004 [US1] `account-page.spec.ts` (RED) — renders the account from a stubbed `account-client`
  (name, email, role label); role renders localized; loading + error states. `@testing-library/angular`.
- [x] T005 [US1] `account-page.ts/.html/.css` (standalone, OnPush, signal-based; `TranslocoDirective`),
  the `/account` route, and `account.*` i18n keys in `en.json` + `de.json`. T004 green.

---

## Phase 3: US2 — Route guards (P1, MVP)

**Independent Test**: navigating to `/account` (or `/admin/users`) without a session redirects to
`/bff/login?returnUrl=…`.

- [x] T006 [US2] `auth.guard.spec.ts` (RED) — null session → redirects browser to
  `/bff/login?returnUrl=<path>`; present session → allows. Awaits session readiness (T002).
- [x] T007 [US2] `auth.guard.ts` (functional `CanActivateFn`), applied to `/account` (and later
  `/admin`). T006 green.

---

## Phase 4: US3 — Admin user list (P2)

**Independent Test**: an administrator on `/admin/users` sees the account list; an employee is sent to
the not-authorized view and the admin nav entry is absent; an empty list shows the empty state.

- [x] T008 [US3] `admin.guard.spec.ts` (RED) — roles include `administrator` → allows; otherwise →
  not-authorized route. `admin-users-page.spec.ts` (RED) — renders rows from a stubbed client (name,
  email, role, status); empty list → empty-state message.
- [x] T009 [US3] `admin.guard.ts`, `admin-users-page.ts/.html/.css`, `not-authorized.ts/.html`, the
  `/admin/users` route (guarded by `authGuard` + `adminGuard`), the admin nav entry shown only to
  admins (in the shell), and `admin.*` i18n keys (en + de). T008 green.

---

## Phase 5: US4 — Grant administrator (P2)

**Independent Test**: an admin grants administrator to an employee row → the role updates; repeating
is a no-op with no error; a failed grant leaves the row unchanged with a non-blocking error.

- [x] T010 [US4] Extend `admin-users-page.spec.ts` (RED) — clicking "Grant administrator" calls the
  client and updates the row's role on success (`204`); idempotent repeat stays administrator; a `5xx`
  shows an error and leaves the row unchanged; the action has an accessible name and an announced result.
- [x] T011 [US4] Wire the grant action in `admin-users-page.ts` (+ confirm affordance, `aria-live`
  result region per FR-007) and the i18n keys. T010 green.

---

## Phase 6: Polish

- [x] T012 [P] Accessibility pass (WCAG 2.2 AA): keyboard operability, focus visibility, roles/names,
  `lang` correctness across both languages; verify with the testing-library role queries already in the
  specs. Confirm `de.json` parity with `en.json` (no missing keys).

---

## Dependencies & Execution Order

- **Setup (T001–T003)** → stories. T003 (gateway route) unblocks US3/US4 end-to-end but the component
  specs (stubbed clients) don't need it.
- **US1** and **US2** are the MVP. **US3** depends on US2 (auth guard) + T003. **US4** depends on US3.
- Within each story: **spec first (must FAIL)** → component/guard → i18n. Commit per task or logical group.

## Notes

- Tests fail before implementation (verify RED).
- No new ADR required; D-FE1 (inline vs Nx lib) is recorded in plan.md and flagged for reviewer agreement.
- Token-free throughout (ADR-0013); same-origin relative URLs (ADR-0030).

## Status reconciliation

All tasks are complete. The implementation lives in the Nx feature lib
`libs/identity/feature/` and the data-access lib `libs/identity/data-access/` (per **ADR-0035**),
not the original `apps/web/src/app/identity/` path named in T001/T004 — D-FE1 was resolved in
favour of feature libs after this task list was written. The session readiness signal (T002) is
`ensureLoaded()` in `libs/shared/data-access/`. T010–T012 were finished last: the grant action now
has an **inline confirm** affordance (Grant → in-row Confirm / Cancel), an `aria-live="polite"`
`role="status"` success announcement naming the account, focus-visible styling on the action
buttons (`--roomy-focus-outline`), and an explicit idempotency test (no grant control for an
account already an administrator).
