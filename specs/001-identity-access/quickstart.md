# Quickstart / Validation Guide: Identity & Access

Runnable scenarios that prove the slice works end-to-end. This is a validation guide, not
implementation — concrete steps land in `tasks.md`.

## Prerequisites

- .NET 10 SDK, Docker (for Testcontainers + Aspire-composed Keycloak/Postgres).
- The Aspire app host brings up: PostgreSQL (identity DB), Keycloak (realm + roles `employee`,
  `administrator`, unique-email + min-length-8 password policy), the `identity-api`, and the YARP
  gateway/BFF.
- `DefaultAdmin` credentials supplied via configuration/secret (not committed).

## Run

```
# from repo root
dotnet run --project backend/apps/identity-api            # or via the Aspire app host once it exists
```

## Scenarios (map to acceptance criteria)

1. **DefaultAdmin first access (IA-1).** Start with an empty DB. The service seeds `DefaultAdmin`
   into Keycloak + its account record. Log in through the BFF with the configured credentials →
   session established; `GET /account/me` returns `role: administrator`.

2. **Invalid credentials (FR-008).** Log in with a wrong password and with an unknown email →
   both return the **same** generic failure; no indication of whether the account exists.

3. **Provision an employee (IA-3, eventual).** Publish an `EmployeeHired` event (role `employee`,
   initial password ≥ 8 chars). Observe `UserRegistered` emitted; then log in as that user → success;
   `GET /account/me` returns `role: employee`. A password < 8 chars yields `UserProvisioningFailed`
   (`password_rejected`) and no login.

4. **Provision/elevate an administrator (IA-4).** Either provision with role `administrator`, or
   `POST /admin/users/{id}:grant-administrator` on an existing employee → that account gains admin
   capabilities (can call `/admin/users`).

5. **Employee cannot manage accounts (FR-007).** As an `employee`, call `GET /admin/users` → `403`.

6. **Administrator is also an employee (IA-5).** `GET /account/me` for the admin shows the employee
   capability; the admin can be the subject of an attendance reservation (verified in `003`).

7. **Duplicate email (FR-009).** Provision a second account with an existing email →
   `UserProvisioningFailed` (`email_taken`); no second account created.

8. **Logout (IA-6).** `POST` the BFF logout route → session cleared; a subsequent `GET /account/me`
   returns `401` until logging in again.

## Automated coverage

- **Unit:** `User` aggregate invariants (role model, email validity/uniqueness guard, status
  transitions) and value objects.
- **Integration (Testcontainers Postgres + Keycloak):** `RegisterUser` on `EmployeeHired` →
  `UserRegistered`; failure paths → `UserProvisioningFailed`; DefaultAdmin seeding idempotency;
  `/admin/*` authorization.
- **Architecture (NetArchTest):** dependency rule + no-MediatR + no framework types in
  `domain`/`application`.
- **e2e (Playwright, later UI slice):** login/logout through the BFF.
