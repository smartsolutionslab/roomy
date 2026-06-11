# Contracts: Administrator is also an employee

**No new external interfaces.** This feature changes *how the seeded administrator is bootstrapped*;
it adds no API endpoint, no integration event, and no client contract.

It reuses existing contracts unchanged:

- **Internal command** `HireEmployee(Name, Email, EmployeeRole, InitialPassword)` (organization application)
  — invoked by the new organization-side admin seeder.
- **Integration event** `Contracts.Organization.EmployeeHired(EmployeeId, UserId, Email, DisplayName, Role, InitialPassword, OccurredAt)`
  with `HiredRole.Administrator` — already published by the `Employee` aggregate and consumed by identity
  (`RegisterUser`) and attendance (directory projection).
- **Internal command** `RegisterUser(...)` (identity application) — already issued by identity's
  `EmployeeHiredConsumer`.

The administrator subsequently uses the **existing** attendance endpoints (`POST /reservations`,
`GET /reservations/mine`) with no contract change — the fix is that the admin is now resolvable as an
`Employee`. Those endpoints' wire contracts are owned by spec `003-attendance` and are untouched.

No OpenAPI re-emit and no Angular client regeneration are required.
