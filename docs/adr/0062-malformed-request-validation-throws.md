# 0062. Malformed-request input throws a BadRequestException mapped to 400

- **Status:** Accepted
- **Date:** 2026-06-13
- **Deciders:** Heiko Weiß

## Context and problem statement

Query-string input that the client got wrong — an out-of-range page `limit`, a malformed cursor — is
parsed at the API edge by a value factory (`PageRequest.From(cursor, limit)`). That factory returned
`Result<PageRequest>`, so every endpoint repeated the same unpacking before it could use the value:

```csharp
var request = PageRequest.From(cursor, limit);
if (request.IsFailure) return request.Error.ToBadRequest();
... request.Value ...
```

Four endpoints carried this boilerplate. The `Result` is never propagated anywhere — it is unpacked
to a 400 on the spot — so it buys nothing over a throw, while the value-object convention
(`From` is `TryParse(raw) ?? throw`, see CLAUDE.md) already says `From` should throw on bad input.

## Decision drivers

- A factory named `From` should follow the house convention and throw on invalid input; the `Result`
  form here only exists to be immediately unpacked at one call depth.
- The throw must still produce the *same* structured 400 body (`{code, message}`) the `Result` path
  produced — the wire contract must not change.
- A bare `throw` must not become a 500 for input the client got wrong.

## Decision

**`PageRequest.From` throws `BadRequestException` (carrying the structured `Error`) on invalid input and
returns a `PageRequest` directly.** A single global `IExceptionHandler` translates malformed-input
exceptions into the existing 400 `{code, message}` body:

- `BadRequestException` → its structured `Error` (`{code, message}`), unchanged from the `Result` path.
- `ArgumentException` → a 400 with code `bad_request` and the exception message. This is what the
  value-object `From` factories already throw (`From` is `TryParse(raw) ?? throw new ArgumentException`,
  see CLAUDE.md), so the same edge factories used elsewhere (`WorkEmail.From`, `OfficeName.From`, …)
  also surface a 400 when handed bad client input, instead of each endpoint pre-checking with `TryParse`.

Any other exception falls through to the default 500.

> **Trade-off, accepted:** mapping `ArgumentException` broadly means an `ArgumentException` thrown by a
> genuine server-side bug (not just edge parsing) also returns 400, which can mask a 500. We accept this
> to keep the value-object `From` convention usable directly at the API edge without a per-call `TryParse`
> guard.

- `BadRequestException(Error error)` lives in `shared-kernel` (`…SharedKernel.Results`) next to `Error`,
  so the value factory that throws it does not reach outside its layer.
- `BadRequestExceptionHandler` and `AddRoomyExceptionHandling()` live in `web-http` (the existing
  HTTP-edge helper, ADR-0046). It reuses the `ErrorResponse` body so a malformed-request 400 is
  byte-for-byte what `Error.ToBadRequest()` produced.
- Hosts opt in with `builder.Services.AddRoomyExceptionHandling()` + `app.UseExceptionHandler()`
  (identity, organization, and attendance APIs).

`PageRequest.DecodeCursor` keeps returning `Result` — it is decoded deep inside the page query, not at
the trust boundary, and its failure already surfaces as a 400 through the query result. Only the edge
factory `From` changes.

## Consequences

**Positive**
- Endpoints read the value in one line; the repeated `IsFailure → ToBadRequest` unpacking is gone.
- `From` now matches the value-object convention (`From` throws, the `Try`/`Result` form is the
  non-throwing variant).
- The 400 body is unchanged — same `{code, message}`, same status — so the OpenAPI contract and the
  generated Angular client are untouched.

**Negative / trade-offs**
- Expected client mistakes now travel as exceptions. Exceptions cost more than a returned value, but
  this is a single throw per bad request on a non-hot path, and the handler scopes the cost to genuinely
  invalid input.
- A new cross-cutting pipeline step (`UseExceptionHandler`) that each API host must wire.

## Related

- ADR-0046 (`web-http` domain-`Error` → HTTP mapping), CLAUDE.md value-object `From`/`TryParse` convention.
- `SearchTerm.From` follows the same shape — it throws `BadRequestException` on an over-long term, so the
  employee-directory endpoint no longer unpacks a `Result` either.
