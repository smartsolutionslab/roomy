# Quickstart / Validation: Administrator is also an employee

Proves the seeded administrator becomes a first-class employee who can reserve and view their own
reservations. See [data-model.md](./data-model.md) for the identifier flow and [research.md](./research.md)
for the bootstrap decision.

## Prerequisites

- A clean environment (fresh Keycloak + Postgres volumes) so the organization admin seeder runs on a
  blank slate. Under Aspire: remove the persistent volumes, then `aspire run`.
- Admin credentials from `DefaultAdmin:Email` / `DefaultAdmin:InitialPassword` (default `admin@roomy.local`).

## Automated validation (authoritative)

Run on affected projects (test-first — these are written failing before the implementation):

```
dotnet test   # organization-integration + identity-integration + attendance-integration + saga-e2e
```

Expected coverage:
- **organization-integration** — the admin seeder creates exactly one `Administrator` `Employee` for the
  seeded company; running it twice creates no duplicate (FR-001, FR-006, FR-007).
- **saga-e2e** — after startup, the admin appears in the attendance employee directory with a `UserId`
  matching the identity `User`, and a token for the admin carries `roomy_user_id` (FR-004).
- **attendance-integration / identity-integration** — `POST /reservations` as the admin returns `201`;
  `GET /reservations/mine` returns `200` (empty, then the booking) (FR-002, FR-003). Existing employee
  flows still pass (SC-003).

## Manual end-to-end (smoke)

1. Bring up the stack on a clean slate and wait for the admin saga to converge.
2. Sign in to the app at the gateway HTTPS URL as `admin@roomy.local`.
3. Open **My reservations** → expect an empty list, **not** a 404 / "unknown employee".
4. Reserve a desk for a bookable future day → expect success (201-equivalent in the UI).
5. Reopen **My reservations** → the new booking is listed.
6. Perform an admin action (manage offices/rooms/users) → still succeeds (admin role retained).

## Success signals

- `unknown_employee` no longer occurs for the administrator on reserve or my-reservations.
- Exactly one admin `Employee` exists; repeated startups add none.
- All pre-existing employee reservation flows remain green.
