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
- `infrastructure-messaging` (the Wolverine + RabbitMQ integration-event adapter, #20/ADR-0005/0015)
  is likewise **infrastructure**: the no-MediatR rule applies, and it legitimately depends on
  **Wolverine** — the deferred messaging adapter the core never references. The "no framework in
  the core" rules do not apply to its `.Infrastructure.Messaging` namespace. Crucially, those same
  rules keep `domain`/`application` Wolverine-free: `application-contracts` (which owns the
  `IIntegrationEventPublisher` port) is checked non-vacuously against the framework list — which
  includes `Wolverine` — by `ApplicationContractsPurityTests`. See
  `InfrastructureMessagingConventionTests`.
- No Roomy assembly anywhere references MediatR ("no MediatR", ADR-0005).

**Convention-based** (by namespace, across every discovered assembly):

- `*.Domain` depends on nothing outside itself / shared (no `*.Application`, no
  `*.Infrastructure`, no framework).
- `*.Application` depends only on `*.Domain` and shared (no `*.Infrastructure`, no framework).
- A context never references another context's types (cross-context only by ID + events).

## How assemblies are discovered

`RoomyAssemblies` scans the test output directory for `SmartSolutionsLab.Roomy.*.dll`
and loads every match — so a `<ProjectReference>` from `Roomy.ArchitectureTests.csproj`
(which makes the assembly copy-local) is what guarantees inspection. Two safety nets
keep the suite honest:

- `RoomyAssembliesTests` pins the expected assembly set by name; a silently dropped
  assembly fails that canary loudly.
- A convention rule that inspects **zero** types fails outright — a vacuous pass is
  treated as broken discovery, never as green.

## ⚠️ Adding a new context — required steps

> **When you create a bounded context (or any new Roomy library), add its projects as
> `<ProjectReference>`s in `tests/architecture/Roomy.ArchitectureTests/Roomy.ArchitectureTests.csproj`
> _and_ add their assembly names to the expected set in `RoomyAssembliesTests`.**

The reference makes the assembly discoverable; the canary entry makes a future silent
drop-out (e.g. a removed reference) fail loudly instead of shrinking coverage.

## Running

```
dotnet test Roomy.slnx        # runs these alongside the rest of the suite (also in CI)
```
