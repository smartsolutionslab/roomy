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
- **400 Bad Request:** a missing/invalid field (bad email, empty name, unknown role, empty password) —
  matching the organization context's existing validation convention (the office endpoints return 400).
- **403:** the caller is not an administrator.
- **401:** no authenticated BFF session.

> The employee's convergence to **`Active`** (or **`Failed`** on compensation) happens asynchronously and
> is **not** observed by this endpoint. A read surface for an employee's provisioning state (e.g.
> `GET /employees`) is a later feature (the admin UI, out of scope here).

## Error body

Validation failures return **400** with a human-readable message (the organization context's convention,
mirroring the office endpoints); authorization failures are **403**/**401** from the policy. No domain
detail leaks beyond a human message.

## Gateway route

The gateway forwards `/employees/**` to the `organization` cluster under the authenticated BFF session
(ADR-0013) — add the `organization-employees` route alongside the existing `organization-offices` route.
The OpenAPI spec emitted by `organization-api` (already wired, ADR-0036) is re-emitted to include
`POST /employees` and is drift-gated in CI.
