# Internal REST Contract: Identity & Access

Internal API of the `identity` service, reachable only through the YARP gateway/BFF (ADR-0013/0018).
Login/logout are **not** endpoints here — they are handled by Keycloak + the BFF (OIDC). This surface
is the account/role read model the app needs, plus the admin elevation. Account *creation* is not a
REST endpoint: it happens via the `EmployeeHired` saga (see `integration-events.md`).

All routes require an authenticated BFF session. Authorization is by role claim.

## `GET /account/me`
Returns the current user's account/role projection (IA-2/IA-5 — "who am I, what can I do").

- **Auth:** any authenticated user.
- **200:** `{ userId, email, displayName, role }` where `role ∈ { employee, administrator }`.
- **401:** no valid session.

## `GET /admin/users`
Lists accounts (IA-1/IA-3 admin overview).

- **Auth:** `administrator` only.
- **200:** `[ { userId, email, displayName, role, status } ]`.
- **403:** authenticated but not an administrator (FR-007).

## `GET /admin/users/{userId}`
- **Auth:** `administrator` only.
- **200:** `{ userId, email, displayName, role, status }`.
- **404:** unknown user. **403:** not an administrator.

## `POST /admin/users/{userId}:grant-administrator`
Elevates an existing account to Administrator (IA-4).

- **Auth:** `administrator` only.
- **204:** elevation applied (idempotent); raises `AdministratorGranted`.
- **403:** not an administrator. **404:** unknown user.

## Notes

- **Login (IA-1/IA-2):** browser → BFF → Keycloak (OIDC auth code); the BFF sets the session cookie.
  No tokens reach the SPA.
- **Logout (IA-6):** `POST` to the BFF logout route → clears session + Keycloak end-session.
- **Generic auth failure (FR-008):** surfaced by Keycloak/BFF, not by these endpoints.
- A typed Angular client is generated from the OpenAPI rendering of this surface (ADR-0018); the
  authoritative OpenAPI document is produced by the host.
