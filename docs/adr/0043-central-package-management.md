# 0043. Central Package Management for NuGet versions

- **Status:** Accepted
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

The solution has ~30 .NET projects (each context's `domain`/`application`/`infrastructure`, the API hosts, the
gateway, the migrator, and the test + test-app-host projects). Until now every project declared its own NuGet
versions inline (`<PackageReference Include="X" Version="Y" />`). The same package (e.g. `Microsoft.NET.Test.Sdk`,
`xunit.v3`, the EF Core / Wolverine / Aspire families) is referenced from many projects, so a version lived in
many files. That invites drift — two projects on different versions of the same package — and makes an upgrade a
multi-file edit that is easy to do incompletely.

At the time of this decision an audit found **no** version conflicts (every package was on a single version), so
the move is a clean, mechanical consolidation rather than a reconciliation.

## Decision

Adopt **Central Package Management (CPM)**. A root `Directory.Packages.props` sets
`ManagePackageVersionsCentrally=true` and declares one `<PackageVersion Include="X" Version="Y" />` per package.
Every project file references packages **by name only** — `<PackageReference Include="X" />` with no `Version` —
and the version is resolved centrally. Package-reference metadata that is per-project (`PrivateAssets`,
`IncludeAssets` on the design-time/build-only references) stays on the `<PackageReference>`; only the version
moves out.

The Aspire app-host SDK version stays on the `Sdk="Aspire.AppHost.Sdk/13.4.2"` attribute — that is an MSBuild SDK
reference, not a `PackageReference`, so CPM does not manage it. Transitive pinning
(`CentralPackageTransitivePinningEnabled`) is **not** enabled, to avoid having to enumerate transitive
dependencies; only direct references are centrally versioned.

## Consequences

**Positive**
- One place for every NuGet version; an upgrade is a one-line edit to `Directory.Packages.props`.
- Version drift across projects becomes impossible — a single `<PackageVersion>` governs all references.
- New projects inherit the managed versions automatically (just reference the package by name).
- `dotnet build -warnaserror` over the whole solution passes with CPM enabled (no `NU1008`/version-on-reference
  warnings), so the build gate enforces the convention.

**Negative / trade-offs**
- A `<PackageReference>` that needs a different version must opt out explicitly with `VersionOverride` — a
  deliberate, visible exception rather than silent drift.
- Adding a package now means two edits when the version is new (the `PackageVersion` entry + the reference),
  though the reference alone suffices once the version exists centrally.

## Notes
- Introduced by the CPM conversion change (this ADR + `Directory.Packages.props` + stripping the inline versions
  from every `.csproj`).
- The frontend (pnpm/Angular) has its own version management and is unaffected.
