# 0057. Nest all backend (.NET) projects under `backend/`

- **Status:** Accepted
- **Date:** 2026-06-11
- **Deciders:** Heiko Weiß

## Context and problem statement

The repository grew an asymmetric top level. All Angular/Nx projects already live under a
single `frontend/` root (ADR-0016, ADR-0035), but the .NET side stays spread across three
sibling top-level folders:

- `apps/` — the .NET hosts (Aspire app host, YARP gateway, the three context APIs, the
  DB migrator, the dev seeder),
- `libs/` — the backend libraries (domain/application/infrastructure per context, the
  shared kernel, web-http, the infrastructure adapters), and
- `tests/` — every backend test, integration, and test-app-host project.

Reading the repository root no longer makes the front-end/back-end split obvious, and the
three backend roots read as unrelated to a newcomer even though they are one stack. We want
the top level to state the architecture: one `frontend/`, one `backend/`.

## Decision drivers

- **Symmetry with `frontend/`.** A single `backend/` root mirrors the existing front-end
  layout and makes the two halves of the system legible at a glance.
- **No behavioural change.** This is a move, not a redesign — no project is renamed, no
  code logic changes, no dependency is added or removed.
- **Keep the change mechanical and low-risk.** Every inter-project reference must keep
  resolving without per-project edits.

## Decision

Move `apps/`, `libs/`, and `tests/` wholesale under a new `backend/` folder, becoming
`backend/apps`, `backend/libs`, and `backend/tests`. The three move **together**, so every
`<ProjectReference>` between them — all repo-relative (`../../libs/...`,
`../../../libs/...`) — keeps resolving unchanged; no `.csproj` is edited.

What this is safe against, and why it stayed mechanical:

- **Repo-root MSBuild files are untouched.** `Directory.Build.props`,
  `Directory.Packages.props` (Central Package Management, ADR-0043), `coverlet.runsettings`,
  and `global.json` stay at the root and apply recursively / by glob to `backend/**`.
- **The coverage gate moves with its consumers.** `tests/coverage/CoverageGate.props`
  (ADR-0022) and the projects that import it are all under `tests/`, so their relative
  imports survive the move.
- **The Nx graph is unaffected.** Backend projects are not Nx projects (no `project.json`),
  so `nx.json`, `pnpm-workspace.yaml`, and the `@nx/enforce-module-boundaries` constraints
  (ADR-0002/0003) need no change. The .NET dependency rule is still enforced by the
  NetArchTest project, which moves to `backend/tests/architecture`.

The few places that hold an explicit, non-relative path to backend content are updated to
the new prefix: the solution file `Roomy.slnx`; the three `ng-openapi-gen.json` spec inputs
under `frontend/libs/*/api` (ADR-0036); the CI workflow's hard-coded project, spec-drift,
and Wolverine-codegen paths; and `.prettierignore`. Documentation that names the old paths
is updated to `backend/...` across `CLAUDE.md`, `README.md`, the ADRs, and the specs.

## Consequences

**Positive**
- The repository root now reads as `backend/` + `frontend/`, matching the architecture.
- The move preserves git history (`git mv` / rename detection) and required no source or
  project-reference edits, only path strings in a handful of config and doc files.

**Negative / trade-offs**
- One-off churn: the solution file, CI paths, the OpenAPI client spec inputs, and every
  doc/spec path reference are rewritten in a single commit. After this lands, any external
  bookmark or open branch referencing the old `apps/`, `libs/`, `tests/` paths must rebase.
- Historical ADRs and specs are rewritten to the new paths for repo-wide consistency rather
  than left as point-in-time records; this ADR is the anchor that explains the rename.
