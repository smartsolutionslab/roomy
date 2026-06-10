# 0050. Endpoint DTOs drop the folder-redundant suffix; the OpenAPI schema id is reconstructed

- **Status:** Accepted
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

ADR-0049 moved every API-host endpoint DTO into a `Request/` or `Response/` subfolder with a matching
sub-namespace (`…Endpoints.Request` / `…Endpoints.Response`), and `*Page` keyset wrappers under
`Response/` too. With the folder and namespace now expressing the role, the type-name suffix repeats it:
`Endpoints.Response.EmployeeResponse`, `Endpoints.Response.EmployeePage`, `Endpoints.Request.ReserveRequest`.
The suffix is pure stutter — the namespace already says `Response` / `Request`, and `Page` is the only
distinguishing token the wrapper needs from its row record.

Dropping the suffix (`Response.Employee`, `Response.Page.Employee`, `Request.Reserve`) reads better in the
endpoint code and matches how the rest of the codebase names types inside role-named folders. But the
emitted OpenAPI schema id is, by default, the **simple type name independent of namespace** (ADR-0049
relied on exactly this). Two problems follow:

- **Wire drift.** `EmployeeResponse` would become `Employee`, changing every `#/components/schemas/*` id
  and regenerating the typed Angular client (ADR-0036) — a breaking contract change for a pure rename.
- **Collision.** `Endpoints.Response.Employee` and `Endpoints.Response.Page.Employee` both reduce to the
  simple name `Employee`, so the default short-name id is no longer unique and the document fails to emit.

How do we get the cleaner C# names without changing the wire contract or colliding?

## Decision drivers

- **Contract stability is non-negotiable.** The emitted spec — and therefore the generated client — must be
  byte-identical after the rename; the drift gate (ADR-0036) stays green with no client regeneration.
- **Names reveal intent without stutter.** A type inside a role-named folder should not repeat the role in
  its own name (csharp.md).
- **One owned seam, not per-host bespoke config.** The schema-id rule is one cross-cutting concern; it
  belongs in the shared `web-http` library, not copy-pasted into three `Program.cs` files.
- **Don't surprise the framework default.** Types that are *not* endpoint DTOs (`ProblemDetails`,
  `ErrorResponse`, …) must keep the framework's default short-name id untouched.

## Considered options

- **A — Keep the suffixes.** Zero work, but the names stutter against the folder/namespace introduced by
  ADR-0049, and the convention reads as half-applied.
- **B — Drop the suffix, accept the wire change, regenerate the client.** Cleanest C#, but a breaking
  contract change and client churn for a cosmetic rename — and it still has to solve the `Response` vs
  `Response.Page` collision.
- **C — Drop the suffix, reconstruct the schema id from the namespace tail (chosen).** Rename the types to
  the short form and register a `CreateSchemaReferenceId` that re-derives `<TypeName><Suffix>` from the
  sub-namespace (`.Response.Page` → `Page`, `.Response` → `Response`, `.Request` → `Request`), so the wire
  id is unchanged and the `Response` / `Page` pair never collides. Non-DTO types fall through to the
  framework default.

## Decision

**Option C.**

1. Endpoint DTOs use the short name: response bodies are `…Endpoints.Response.<Name>`, their keyset
   wrappers `…Endpoints.Response.Page.<Name>` (a `Page` sub-namespace/subfolder), request bodies
   `…Endpoints.Request.<Name>`. Endpoint classes reference them by the qualified short form
   (`Response.Reservation`, `Response.Page.Employee`, `Request.Reserve`) rather than importing the
   sub-namespace, so the role stays legible at the use site.
2. `libs/web-http` owns `EndpointSchemaIds.ForEndpointDto`, a `Func<JsonTypeInfo, string?>` that maps a
   DTO's namespace tail back to the historical wire id — `…Response.Page.Employee` → `EmployeePage`,
   `…Response.Employee` → `EmployeeResponse`, `…Request.Reserve` → `ReserveRequest`. Any type whose
   namespace does not end in one of those three segments returns the framework default
   (`OpenApiOptions.CreateDefaultSchemaReferenceId`), so shared types keep their short id.
3. Each host wires it once: `AddOpenApi(options => options.CreateSchemaReferenceId = EndpointSchemaIds.ForEndpointDto)`.
   This required `web-http` to take a `Microsoft.AspNetCore.OpenApi` package reference.

The emitted spec is therefore unchanged: `Account` still serialises as `AccountResponse`, `Employee` as
`EmployeeResponse`, the page wrapper as `EmployeePage`. Verified by the `#/components/schemas/AccountResponse`
assertion in `OpenApiDocumentTests` and by the spec-drift gate, which re-emits all three host specs and
fails on any diff — both green with no change to the committed `Roomy.*.Api.json` or the generated client.

The gateway BFF `CurrentUser` (`…Bff.Response.CurrentUser`) has no redundant suffix to drop and its wire id
was already `CurrentUser`; it is left as-is and the gateway does **not** wire the reconstruction (which would
wrongly append `Response`).

## Consequences

**Positive**
- Endpoint code reads without stutter: `Produces<Response.Page.Employee>()`, `new Request.Reserve(...)`.
- The wire contract and the generated Angular client are untouched — a pure internal rename.
- The schema-id rule lives in one owned seam (`web-http`), tested once, reused by every host.

**Negative / trade-offs**
- The wire schema id no longer equals the C# type name; the mapping lives in `EndpointSchemaIds`. The
  reconstruction comment and this ADR document the indirection, and the drift gate enforces it.
- A new endpoint DTO must sit under one of the three recognised sub-namespaces to get a suffixed id; one in
  a different namespace would silently get a bare short-name id. Acceptable — it is the same folder=namespace
  convention ADR-0049 already requires.

**Follow-ups**
- csharp.md and CLAUDE.md record the short-name rule and the reconstructed-id seam.
