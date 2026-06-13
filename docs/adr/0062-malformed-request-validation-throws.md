# 0062. Malformed-request input throws an Argument exception mapped to 400

- **Status:** Accepted
- **Date:** 2026-06-13
- **Deciders:** Heiko Weiß

## Context and problem statement

Input the client got wrong — an out-of-range page `limit`, an over-long search term, a blank name or
malformed email — is parsed at the API edge by a value factory (`PageRequest.From`, `SearchTerm.From`,
`WorkEmail.From`, …). Two styles had grown up side by side:

- Value objects follow the house convention: `From` is `TryParse(raw) ?? throw new ArgumentException`
  (CLAUDE.md). They *throw* on bad input.
- `PageRequest.From` / `SearchTerm.From` returned `Result<T>`, so every endpoint repeated
  `if (x.IsFailure) return x.Error.ToBadRequest();` before it could use the value — a `Result` that was
  never propagated, only unpacked on the spot.

We want one rule at the edge, not two, and the throwing convention is the one already in place for the
dozen value objects.

## Decision drivers

- A factory named `From` should follow the house convention and throw on invalid input.
- A thrown parse failure must still produce a 400 — the client got the input wrong, not the server.
- Don't invent a parallel exception hierarchy when the BCL already has the right one and the value
  objects already throw it.

## Decision

**Edge value factories throw an `Argument` exception on invalid input, and a single global
`IExceptionHandler` maps `ArgumentException` (and its subclasses) to a 400.**

- `PageRequest.From` and `SearchTerm.From` return their type directly and throw
  `ArgumentOutOfRangeException` on a bad limit / over-long term (blank/whitespace still yields the
  no-filter `SearchTerm.None`). They join the value objects, which already throw `ArgumentException`.
- `ArgumentExceptionHandler` (in `web-http`, the HTTP-edge helper, ADR-0046) catches any
  `ArgumentException` and writes the existing `ErrorResponse` body as a 400 with code `bad_request` and
  the exception message. The 400 body *shape* (`{code, message}`) is unchanged, so the OpenAPI contract
  and the generated Angular client are untouched.
- Hosts opt in with `builder.Services.AddRoomyExceptionHandling()` + `app.UseExceptionHandler()`
  (identity, organization, and attendance APIs).

`PageRequest.DecodeCursor` keeps returning `Result` — it is decoded deep inside the page query, not at
the trust boundary, and its failure already surfaces as a 400 through the query result.

> **Trade-off, accepted:** mapping `ArgumentException` broadly means an `ArgumentException` thrown by a
> genuine server-side bug also returns 400, which can mask a 500; and the 400 body carries a single
> `bad_request` code rather than a per-failure code (`pagination.limit_out_of_range`, …). Nothing
> consumes those codes today (no client, no i18n — only the factories' own unit tests did), so the loss
> is theoretical, and we accept the masking risk to keep one validation rule at the edge.

## Consequences

**Positive**
- One rule at the edge: every `From` throws an `Argument` exception; one handler turns it into a 400.
  No endpoint unpacks a `Result` or pre-checks with `TryParse`.
- No bespoke exception type to maintain — the value objects, `PageRequest`, and `SearchTerm` all throw
  the same BCL family.

**Negative / trade-offs**
- The accepted trade-off above (broad `ArgumentException` → 400, single `bad_request` code).
- A new cross-cutting pipeline step (`UseExceptionHandler`) that each API host must wire.

## Related

- ADR-0046 (`web-http` domain-`Error` → HTTP mapping), CLAUDE.md value-object `From`/`TryParse` convention.
- `Ensure` guards (`IsNotNullOrWhiteSpace`, `IsEnum<TEnum>`, …) throw `ArgumentException` too, so the same
  handler renders guard failures at the edge as 400s.
