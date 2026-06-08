# Architecture tests

NetArchTest rules (`Roomy.ArchitectureTests`) enforcing the Clean Architecture dependency
rule and the related invariants on the .NET side — the counterpart to the Nx
`@nx/enforce-module-boundaries` lint (ADR-0002/0003/0005).

## What is enforced

**Enforced now** (against the assemblies that exist today):

- `shared-kernel` is a pure primitives library — it must not depend on MediatR, Wolverine,
  EF Core, ASP.NET Core, or YARP.
- `application-contracts` is likewise framework-free (the owned dispatch/messaging seam).
- `infrastructure-persistence` (the EF Core baseline + hand-rolled event store, #19/ADR-0012)
  is **infrastructure**: the repo-wide no-MediatR rule applies to it, but the "no framework in
  the core" rules deliberately do **not** — it legitimately depends on EF Core / Npgsql. The
  framework rules are scoped to the `.Domain`/`.Application` namespace segments only, which this
  assembly's `.Infrastructure.Persistence` namespace does not match. See
  `InfrastructurePersistenceConventionTests`.
- No Roomy assembly anywhere references MediatR ("no MediatR", ADR-0005).

**Forward-looking** (convention-based, by namespace — active as soon as a matching assembly
is *loaded*; see the caveat below):

- `*.Domain` depends on nothing outside itself / shared (no `*.Application`, no
  `*.Infrastructure`, no framework).
- `*.Application` depends only on `*.Domain` and shared (no `*.Infrastructure`, no framework).
- A context never references another context's types (cross-context only by ID + events).

## ⚠️ Adding a new context — required step

The convention rules inspect every **loaded** `SmartSolutionsLab.Roomy.*` assembly. An
assembly is loaded only if `Roomy.ArchitectureTests` (transitively) **references** it.
NetArchTest does not scan the build output.

> **When you create a bounded context, add its `domain`, `application`, and
> `infrastructure` projects as `<ProjectReference>`s in
> `tests/architecture/Roomy.ArchitectureTests/Roomy.ArchitectureTests.csproj`.**

If you skip this, the forward-looking rules match zero types for that context and pass
**vacuously** — green, but enforcing nothing. The tests deliberately report when a
convention rule inspected zero types so a dormant rule is visible rather than silent, but
the reference is still your responsibility.

## Running

```
dotnet test Roomy.slnx        # runs these alongside the rest of the suite (also in CI)
```
