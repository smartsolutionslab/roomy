# Roomy

Roomy is a B2B office attendance planning platform: teams plan and coordinate who is in
which office, on which day, in which space. The domain is modelled with Domain-Driven
Design and implemented as a set of independently deployable bounded-context services
behind a single gateway.

> **Status: foundational setup.** The repository is in its scaffolding phase. The
> monorepo, quality gates, CI, governance, and architecture decisions are in place; the
> product features (identity, organization, attendance) and the Angular app are **not
> built yet**. The plan lives in [`specs/`](specs/) and [`docs/adr/`](docs/adr/), and the
> setup backlog in [`docs/project-setup-issues.md`](docs/project-setup-issues.md).

## How it works

The domain is split into three bounded contexts, each an independently deployable service
with its own database. They integrate only through asynchronous integration events — never
shared databases or cross-service joins.

| Context | Subdomain | Owns | Feature spec |
|---|---|---|---|
| **Identity & Access** | Generic | Users, roles, authentication | [`specs/001-identity-access`](specs/001-identity-access) |
| **Organization** | Supporting | Companies, offices, rooms, employees | [`specs/002-office-management`](specs/002-office-management) |
| **Attendance** | Core | Attendance days, reservations, occupancy read model | [`specs/003-attendance`](specs/003-attendance), [`specs/004-occupancy`](specs/004-occupancy) |

A YARP gateway / BFF is the only public entry point and composes the context APIs for the
Angular app. See [`CLAUDE.md`](CLAUDE.md) for the full context map and architecture rules,
and [`docs/adr/`](docs/adr/) for the decisions behind them.

## Tech stack

| Concern | Choice |
|---|---|
| Backend | .NET 10 / C#, Clean Architecture + DDD |
| Messaging | Wolverine — integration events + transactional outbox/inbox; RabbitMQ default |
| Gateway / BFF | YARP |
| APIs | REST/JSON with OpenAPI; typed Angular client generated from the spec |
| Orchestration | .NET Aspire |
| Frontend | Angular — standalone, signal-based, zoneless components |
| Persistence | EF Core on PostgreSQL; hand-rolled event store for event-sourced contexts |
| Auth | Keycloak (self-hosted OIDC) with the BFF security pattern |
| Monorepo | Nx, with pnpm as the only supported JS package manager |
| Specs | Spec Kit |

See the stack table in [`CLAUDE.md`](CLAUDE.md) and [`docs/adr/`](docs/adr/) for the
rationale behind each choice.

## Prerequisites

- **.NET 10 SDK** — the pinned version is in [`global.json`](global.json)
- **Node.js** 20 or newer
- **pnpm** 10 or newer — the only supported JS package manager (do **not** use npm or yarn)

## Getting started

Install the JS/TS toolchain (Nx, ESLint, Prettier, commitlint, husky hooks):

```
pnpm install
```

Restore and build the .NET solution:

```
dotnet restore Roomy.slnx
dotnet build Roomy.slnx -warnaserror
```

## Building, testing, and linting

The quality gates below are what CI enforces on every pull request. Run them locally
before opening a PR.

```
dotnet build Roomy.slnx -warnaserror     # nullable on, analyzers on, no warnings
dotnet test Roomy.slnx                    # unit + integration + architecture tests
dotnet format --verify-no-changes         # .NET formatting
pnpm nx run-many -t lint                   # ESLint + Nx module-boundary lint
```

`dotnet test` also enforces the coverage floor on `domain`/`application` projects. See
[`docs/testing-strategy.md`](docs/testing-strategy.md) for the full testing strategy and
[`.github/workflows/ci.yml`](.github/workflows/ci.yml) for the exact CI gate suite.

## Repository layout

```
roomy/
├─ CLAUDE.md            # canonical operating contract for agents working here
├─ CONTRIBUTING.md      # contribution workflow (branching, commits, PRs)
├─ Roomy.slnx           # .NET solution
├─ docs/
│  ├─ adr/              # architecture decision records
│  ├─ coding-standards/ # C# and TypeScript rules
│  └─ project-setup-issues.md
├─ specs/               # Spec Kit specs, plans, tasks per feature
└─ backend/             # ALL .NET projects live here (ADR-0057)
   ├─ apps/             # service hosts and the Aspire app host
   ├─ libs/             # shared-kernel, shared utilities, service defaults, context libs
   └─ tests/
      └─ architecture/  # NetArchTest rules enforcing the dependency rule
```

Only the scaffolding currently exists under `backend/apps/` and `backend/libs/` (the Aspire app host,
service defaults, and shared primitives); context services and the Angular app arrive in
later slices.

## Contributing

Roomy is built spec-first and test-first, one story per short-lived branch, with
Conventional Commits and rebase-and-merge. Before contributing, read:

- [`CONTRIBUTING.md`](CONTRIBUTING.md) — the contribution workflow.
- [`CLAUDE.md`](CLAUDE.md) — the operating contract, golden rules, and work loop.
- [`docs/adr/`](docs/adr/) — the architecture and process decisions.
- [`specs/`](specs/) — the feature specs and plans.
