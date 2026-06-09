# Implementation Plan: Identity Web (Account & Admin UI)

**Branch**: `feat/005-identity-web` | **Date**: 2026-06-09 | **Spec**: `specs/005-identity-web/spec.md`

## Summary

Add the identity SPA surface — an **account page**, an **admin user-management page**, and the
**auth/role route guards** — to the Angular app. Authentication is already handled by the BFF: the
shell exposes Sign in / Sign out and reads the session from `/bff/user`. This slice adds the
authenticated screens that call the identity API **through the gateway** (`/account/me`,
`/admin/users`, `:grant-administrator`), localized (DE+EN) and accessible (WCAG 2.2 AA), with no
tokens in the SPA. One small backend enablement is required: a gateway YARP route for `/admin/{**}`
(only `/account/{**}` exists today).

## Technical Context

**Language/Version**: TypeScript / Angular 22 — standalone, **signal-based**, **zoneless**,
**OnPush**, `inject()`, signal `input()/output()`, no `NgModule` (ADR-0016/0027).

**Primary Dependencies**: `@angular/router` (functional guards), `@angular/common/http`
(`HttpClient`, relative URLs), `@jsverse/transloco` (i18n, ADR-0024), Angular CDK for accessible
behaviours (ADR-0021/0024). Tests: `@testing-library/angular` + `@testing-library/user-event` on the
`@angular/build:unit-test` runner (matching `language-switcher.spec.ts`).

**Backend surface (through the gateway, same-origin, ADR-0030)**:
- `GET /bff/user` → `{ name, roles[] }` (session; already consumed by `SessionService`).
- `GET /account/me` → `{ userId, email, displayName, role }` (identity API via `/account/{**}`).
- `GET /admin/users` → `[{ id, email, displayName, role, status }]` (identity API).
- `POST /admin/users/{id}:grant-administrator` → `204` (idempotent) / `404`.
- **New**: gateway route `/admin/{**}` → identity cluster (default authz policy + token forwarding),
  mirroring the existing `/account/{**}` route. The identity API enforces `RequireRole` → `403`.

**Target Platform**: browser SPA served through the gateway (single origin, ADR-0030).

**Project Type**: frontend feature within `apps/web` (built inline — see D-FE1).

**Constraints**: no tokens in the SPA (ADR-0013); no hardcoded strings (ADR-0024); WCAG 2.2 AA
(ADR-0024); zoneless + OnPush + signals (ADR-0016).

## Constitution Check

| Principle | Verdict | Notes |
|---|---|---|
| I. Spec-Driven & Test-First | ✅ | `spec.md` has testable AC (1–7); component/guard specs precede implementation. |
| II. Clean Architecture & DDD | ✅ (frontend) | UI mirrors the identity context; view models (`Account`, `AdminUser`) are typed, not loose primitives. |
| III. Context Isolation | ✅ | The SPA talks only to the gateway; no cross-context coupling. Identity screens live under a single `context:identity`-aligned area. |
| IV. No Framework in the Core | n/a | Frontend feature. |
| V. Decisions Recorded | ✅ | No **new** ADR: ADR-0013 (BFF), ADR-0016/0027 (Angular), ADR-0024 (i18n/a11y), ADR-0030 (single origin) cover this. The inline-vs-lib choice (D-FE1) is recorded here and flagged for review. |
| VI. Green Before Done | ✅ | `pnpm nx affected -t lint test build`. |
| VII. Small, Single-Purpose Changes | ✅ | One feature, one branch; the gateway route addition is a single, clearly-scoped commit. |

**Gate: PASS.**

## Key decisions

### D-FE1 — Build inline under `apps/web/src/app`, defer the Nx feature lib

**Decision.** Implement the identity screens as standalone components under
`apps/web/src/app/account/` and `apps/web/src/app/admin/`, with a small `identity/` data-access area
(typed gateway clients + view models), mirroring the existing inline `session/`, `shell/`, `home/`
structure. Do **not** create an Nx `@roomy/identity-feature-*` lib in this slice.

**Why.** The web app has no feature-lib infrastructure today (everything is inline), and ADR-0016's
per-context frontend libs imply a structural decision the tag taxonomy doesn't yet resolve for
frontend (the `type:*` axis is domain/application/infrastructure/app/util — no "feature" value).
Establishing that structure + tag semantics is a cross-cutting change that warrants its own ADR
(golden rule 4). Keeping this slice inline makes it small and reviewable; the lib extraction is a
clean, separately-ADR'd follow-up. **Flagged for reviewer agreement.**

### D-FE2 — Account data from `/account/me`, session gate from `/bff/user`

The header/session state stays sourced from `/bff/user` (name + roles, already wired). The account
**page** uses the richer `/account/me` (email, display name, role) so it can show a full profile.
Guards read roles from the session (`SessionService.currentUser().roles`) to avoid an extra round
trip and to keep the gate cheap.

### D-FE3 — Functional route guards, redirect to the BFF

`authGuard` (CanActivateFn): if `currentUser()` is null, redirect the browser to
`/bff/login?returnUrl=<attempted path>`. `adminGuard`: requires `roles` to include `administrator`,
otherwise route to a not-authorized view (and the admin nav entry is hidden when not an admin).
Because the session loads asynchronously at startup, the guards await the first session resolution
(the `SessionService` is extended with a `whenLoaded()`/readiness signal) before deciding.

## Project Structure (this feature)

```text
apps/web/src/app/
├─ identity/
│  ├─ account.ts            # typed view models (Account, AdminUser, AccountRole)
│  ├─ account-client.ts     # GET /account/me  (relative URL, token-free)
│  └─ admin-users-client.ts # GET /admin/users, POST :grant-administrator
├─ account/
│  ├─ account-page.ts/.html/.css        # FR-001
│  └─ account-page.spec.ts
├─ admin/
│  ├─ admin-users-page.ts/.html/.css    # FR-004/005
│  ├─ admin-users-page.spec.ts
│  └─ not-authorized.ts/.html           # FR-003/006 fallback
├─ auth/
│  ├─ auth.guard.ts + auth.guard.spec.ts        # FR-002
│  └─ admin.guard.ts + admin.guard.spec.ts      # FR-003
└─ session/session.service.ts            # extend with readiness for guards (D-FE3)

apps/web/public/i18n/{en,de}.json        # add `account.*` and `admin.*` namespaces (FR-006)
apps/gateway/appsettings.json            # add the `/admin/{**}` YARP route (backend enablement)
```

## Complexity Tracking

> No complexity exceptions requested.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
