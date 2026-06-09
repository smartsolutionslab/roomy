# Internal REST Contract: Hire Employee (008)

Adds the hiring surface to the `organization` service, reachable only through the YARP gateway/BFF
(ADR-0013/0018). The forwarded Keycloak token identifies the acting user and carries the `administrator`
realm role (flattened to a role claim by the host). All dates are UTC.

## `POST /employees`

Hire a colleague (FR-001/002/003, US1). Records the employee in a **provisioning** state and starts
account provisioning; the colleague can sign in **once provisioning completes** (FR-004, eventual
consistency — the response does **not** mean the login exists yet).

- **Auth:** **administrator only** (FR-001). A non-administrator ⇒ **403**.
- **Body:** `{ displayName, email, role, initialPassword }`
  - `role` ∈ `"Employee" | "Administrator"`.
  - `email` must be well-formed; `displayName`, `role`, `initialPassword` are required.
  - `initialPassword` is a transient secret — used to set the credential, never persisted (FR-009).
- **202 Accepted:** `{ employeeId, userId, state: "Provisioning" }` — the employee is recorded and
  provisioning has started. **202**, not 201, signals the resource is not yet fully usable (the login is
  provisioned asynchronously).
- **422 `invalid_hire`:** a missing/invalid field (bad email, empty name, unknown role, empty password).
- **403:** the caller is not an administrator.
- **401:** no authenticated BFF session.

> The employee's convergence to **`Active`** (or **`Failed`** on compensation) happens asynchronously and
> is **not** observed by this endpoint. A read surface for an employee's provisioning state (e.g.
> `GET /employees`) is a later feature (the admin UI, out of scope here).

## Error body

All non-2xx carry `{ code, message }` mapped from `Result`/`Error` (`ErrorType` → status: Validation→422,
Forbidden→403). No domain detail leaks beyond `code` + a human `message`.

## Gateway route

The gateway forwards `/employees/**` to the `organization` cluster under the authenticated BFF session
(ADR-0013) — add the `organization-employees` route alongside the existing `organization-offices` route.
The OpenAPI spec emitted by `organization-api` (already wired, ADR-0036) is re-emitted to include
`POST /employees` and is drift-gated in CI.
