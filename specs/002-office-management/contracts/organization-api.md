# Internal HTTP API: Organization (Offices & Rooms)

Internal surface, reached only through the YARP gateway/BFF (ADR-0013/0018). The BFF forwards the
Keycloak access token; the host validates it as a JWT bearer and flattens realm roles to
`ClaimTypes.Role`.

- **Writes** (`POST`/`PATCH`) require the **Administrator** role → `403` for employees (FR-009),
  `401` without a session.
- **Reads** (`GET`) require any authenticated account.
- Validation failures (empty name/location, capacity < 1) → `400`. Uniqueness conflicts → `409`.
  Unknown office/room → `404`.

Gateway route: `/offices/{**}` → the `organization` cluster (`http://organization-api`), default
authorization policy + access-token forwarding.

## Projections

```jsonc
// OfficeResponse
{
  "id": "uuid",
  "name": "string",
  "location": "string",
  "capacity": 0,          // derived: sum of room capacities
  "rooms": [ /* RoomResponse */ ]
}

// RoomResponse
{ "id": "uuid", "name": "string", "capacity": 0 }
```

## Endpoints

| Method & path | Auth | Body | Success | Errors |
|---|---|---|---|---|
| `POST /offices` | admin | `{ name, location }` | `201` + `OfficeResponse` (Location header) | `400` invalid, `409` name taken |
| `GET /offices` | authenticated | — | `200` `OfficeResponse[]` | — |
| `GET /offices/{officeId}` | authenticated | — | `200` `OfficeResponse` | `404` |
| `PATCH /offices/{officeId}/name` | admin | `{ name }` | `200` `OfficeResponse` | `400`, `404`, `409` |
| `PATCH /offices/{officeId}/location` | admin | `{ location }` | `200` `OfficeResponse` | `400`, `404` |
| `POST /offices/{officeId}/rooms` | admin | `{ name, capacity }` | `201` + `RoomResponse` | `400` (capacity < 1), `404` office, `409` room name |
| `PATCH /offices/{officeId}/rooms/{roomId}/name` | admin | `{ name }` | `200` `RoomResponse` | `400`, `404`, `409` |

Notes:
- Room capacity is **not** editable (FR-006) — there is deliberately no capacity-change endpoint.
- There are no delete endpoints (out of scope).
- The seeded `Company` is implicit: every office is created under the single seeded company, so the
  create body carries no company id.
