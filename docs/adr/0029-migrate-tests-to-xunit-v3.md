# 0029. Migrate the .NET test suite from xUnit v2 to xUnit v3

- **Status:** Accepted
- **Date:** 2026-06-08
- **Deciders:** Heiko Weiß

## Context and problem statement

The .NET test projects use xUnit v2 (`xunit` 2.9.3). A dependency audit
(`dotnet list package --deprecated`) flags `xunit` 2.9.3 as **Legacy**, with the alternative
`xunit.v3`. xUnit v3 is the actively developed line; the v2 package is in maintenance only.

xUnit v3 is a re-architecture: each test project compiles to a self-contained executable
test host rather than a class library loaded by a runner. The test-writing API (the `Xunit`
namespace, `[Fact]` / `[Theory]` / `[InlineData]`) is preserved. Our suite uses only those
attributes plus **Shouldly** for assertions — no `Xunit.Abstractions`, `ITestOutputHelper`,
`IAsyncLifetime`, class/collection fixtures, or raw `Assert.*` — so the source surface that
v3 changes is not in use here. The migration window is therefore as small as it will be.

## Decision drivers

- Move off a deprecated package onto the supported line while the suite is small.
- The runner and SDK are already v3-compatible (`xunit.runner.visualstudio` 3.1.5,
  `Microsoft.NET.Test.Sdk` 18.6.0), so only the framework package changes.
- Golden rule 3 (green before done): the existing tests must stay green across the swap.

## Considered options

- **Migrate to `xunit.v3` now** — replace `xunit` with `xunit.v3` in every test project.
- **Stay on xUnit v2** — rejected; it is deprecated/Legacy and diverging from the v3 tooling
  we already pull in.

## Decision

Replace `xunit` `2.9.3` with **`xunit.v3` `3.2.2`** in all five test projects
(`Roomy.SharedKernel.Tests`, `Roomy.ArchitectureTests`, `Roomy.Gateway.Tests`,
`Roomy.Infrastructure.Messaging.Tests`, `Roomy.Infrastructure.Persistence.Tests`).
`xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk` are unchanged (already v3-ready).
The global `Using Include="Xunit"` stays — the namespace is preserved in v3. No test source
changes are expected; the full suite must remain green under `dotnet test` with
warnings-as-errors.

This is a separate change from the dependency bump (ADR-deps PR) precisely because it is a
framework migration, not a version bump (golden rule 5, single-purpose).

## Consequences

**Positive**
- Test stack is on the supported, actively developed xUnit line.
- Self-contained v3 test executables align with the runner/SDK already in place.

**Negative / trade-offs**
- v3 test projects are executables (`OutputType` managed by the `xunit.v3` build props);
  a future test that reaches for v2-only APIs will need the v3 equivalent.

**Follow-ups**
- None beyond keeping the version current. ADR-0022 (testing strategy) is unaffected.
