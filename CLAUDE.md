# Roomy — Agent Context

> This file is the canonical operating contract for any AI agent working in this
> repository. Read it fully before making changes. If anything here conflicts with
> a request, this file wins — unless the request explicitly amends it (in which case,
> update this file in the same change).

## Project

Roomy is a B2B office attendance planning platform: teams plan and coordinate who is
in which office, on which day, in which space. The domain is modelled with DDD and
implemented as a set of bounded contexts behind a single gateway.

---

## Golden rules (non-negotiable)

1. **No code without a spec — and no implementation without a failing test.** Every
   change traces back to a Spec Kit spec with testable acceptance criteria; each
   criterion becomes a failing test *before* you implement it (see *The work loop*).
   If there is no spec, stop and create one.
2. **Stay inside your bounded context.** Never cross an Nx module boundary or take a
   dependency a layer is not allowed to have. The boundary lint and architecture
   tests enforce this — do not work around them.
3. **Green before done.** Run the full verify suite locally before claiming a task is
   complete. **Never** suppress an analyzer, disable a rule, or skip/delete a test to
   make the build pass. If a gate fails, fix the cause.
4. **Architecture decisions are recorded.** Any decision that changes structure,
   dependencies, or a cross-cutting pattern requires an ADR in `docs/adr/` **before**
   the implementing code.
5. **Small, single-purpose changes.** One story per short-lived branch off `main`; every
   commit is a clean, atomic Conventional Commit (no `wip`/`fix lint` noise — we don't
   squash, so they land on `main` as-is); a PR small enough to review in one sitting.
   Merge with rebase-and-merge. See `CONTRIBUTING.md`.
6. **Ambiguity → ask.** If the spec is unclear or under-specified, stop and ask rather
   than guessing the domain.
7. **One story, one fresh context.** Begin every story/spec in a new agent context.
   Do not carry a previous story's conversation forward — rehydrate from `CLAUDE.md`,
   the spec, and the relevant ADRs. A clean context per slice prevents drift, stale
   assumptions, and the quality decay of a bloated context window.

---

## How agents must reason (behavioural guardrails)

These constrain *how* you work on any task, independent of the domain. Adapted from the
widely-adopted Karpathy agent-coding principles; they exist to prevent the typical
agent failure modes, not to teach you the codebase.

- **Think before coding.** State your assumptions explicitly. When a request is
  ambiguous, surface the interpretations and choose deliberately or ask — never pick one
  silently and run. Push back if a simpler path exists. Name what's unclear instead of
  proceeding past it.
- **Simplicity first.** Write the minimum code that satisfies the spec. No speculative
  features, no single-use abstractions, no "while I'm here" generality. The test: would
  a senior reviewer call this overcomplicated?
- **Surgical changes.** Touch only what the task requires. No drive-by refactors, no
  reformatting unrelated code, no edits to files the task didn't name. Every changed
  line must trace to the spec.
- **Goal-driven execution.** Turn the spec's acceptance criteria into verifiable
  success criteria *first* — concretely, failing tests — then loop until they pass.
  "Fix X" means "write a test that reproduces X, then make it green."

---

## Tech stack

| Concern | Choice |
|---|---|
| Backend | .NET 10 / C#, Clean Architecture + DDD |
| Messaging | Wolverine — async integration events + transactional outbox/inbox behind owned abstractions (ADR-0005). Transport-agnostic: **RabbitMQ** default, Azure Service Bus / AWS SQS+SNS selectable by config (ADR-0015). Required from the first cross-service flow (ADR-0014) |
| Gateway / BFF | YARP |
| APIs | REST/JSON documented with OpenAPI; typed Angular client generated from the spec — build-time spec emit + `ng-openapi-gen`, drift-gated in CI (ADR-0036); gRPC reserved for hot internal paths later (ADR-0018) |
| Orchestration (local + composition) | .NET Aspire |
| Frontend | Angular **22** (22.x) — single app + feature libs per context, standalone **signal-based** components, zoneless (ADR-0016); upgraded ahead of Nx support behind a declared peer override (ADR-0027) |
| Localization | Transloco — DE + EN, runtime switching, no hardcoded strings (ADR-0024) |
| Accessibility | WCAG 2.2 AA baseline; behaviours via Angular CDK (ADR-0024) |
| Monorepo | Nx |
| Package manager (JS) | pnpm — **do not** use npm or yarn |
| Persistence | EF Core on **PostgreSQL**; hand-rolled event store on Postgres for event-sourced contexts (ADR-0012) |
| Auth | Keycloak (self-hosted OIDC); BFF security pattern at YARP — no tokens in the SPA (ADR-0013) |
| AI / agents | Product AI deferred for v1, kept AI-ready behind owned abstractions (`Microsoft.Extensions.AI`, Azure OpenAI, EU region); Roomy exposes an MCP server for agent access (ADR-0023) |
| Specs | Spec Kit |

> See `docs/adr/` for the rationale behind each of these.

---

## Repository layout

Nx monorepo. Backend and frontend live together; boundaries are enforced by tags.

```
roomy/
├─ CLAUDE.md                      # this file
├─ docs/
│  ├─ adr/                        # architecture decision records
│  └─ architecture.md             # living high-level overview  (FILL IN)
├─ specs/                         # Spec Kit specs, plans, tasks
├─ apps/                         # .NET hosts only
│  ├─ gateway/                    # YARP gateway / BFF
│  └─ <context>-api/              # one host per bounded context
├─ libs/                         # .NET libraries only
│  ├─ <context>/
│  │  ├─ domain/                  # entities, aggregates, value objects, domain events  (no infra deps)
│  │  ├─ application/             # use cases, handlers, ports
│  │  ├─ infrastructure/          # persistence, messaging, external adapters
│  │  └─ contracts/              # this context's published integration events  (ADR-0031)
│  └─ shared-kernel/              # only truly shared, stable primitives
├─ frontend/                     # ALL Angular/Nx projects live here (ADR-0016/0035)
│  ├─ apps/
│  │  └─ web/                     # Angular app (the single SPA)
│  └─ libs/
│     ├─ <context>/
│     │  ├─ feature/              # routed feature areas / smart components  (type:feature)
│     │  └─ api/                  # typed OpenAPI client + gateway facade    (type:api)
│     └─ shared/
│        ├─ feature/              # cross-cutting smart components (auth guards, theme toggle)  (type:feature)
│        ├─ data-access/          # shared client-side data utils: session, theme, pagination  (type:data-access)
│        ├─ ui/                   # presentational design-system components  (type:ui)
│        └─ util/                 # shared TS utilities (@roomy/util)         (type:util)
└─ tests/
   └─ architecture/              # NetArchTest rules enforcing the dependency rule
```

**Bounded contexts (confirmed).** The model has **three** contexts — three independently
deployable services under ADR-0014. Pick the right home for new code from this list; the
`specs/` folder has four *feature* specs, but a feature is not a context (see the note on
occupancy below).

| Context (tag / folder) | Subdomain | Owns (aggregates / read models) | Feature spec(s) |
|---|---|---|---|
| `identity` — Identity & Access | Generic | `User` (email + password, roles: Employee base + Administrator elevation), seeded `DefaultAdmin` | `001-identity-access` |
| `organization` — Organization | Supporting (master data, admin-managed) | `Company` (seeded root), `Office` (name, location), `Room` (name, capacity), `Employee` (refs `CompanyId`, refs `UserId`) | `002-office-management` |
| `attendance` — Attendance | **Core** | `AttendanceDay` aggregate (identity = `CompanyId` + `Date`; consistency boundary for no-overbooking and one-reservation-per-employee-per-day), `Reservation` entity, **`Occupancy` read model** (per-room + office rollup) | `003-attendance`, `004-occupancy` |

Hosts are `apps/identity-api`, `apps/organization-api`, `apps/attendance-api`; frontend
feature libs live at `frontend/libs/<context>/<type>` and follow `@roomy/<context>-<type>`
(ADR-0016).

- **Occupancy is not a fourth service.** `004-occupancy` is a read model / projection that
  lives inside the **attendance** context. Its office rollup needs `Room`/`Office` capacity
  from **organization**, fed in by integration events (`OfficeOpened`, `RoomAdded`) per
  ADR-0005/0014 — never by a cross-service join.
- **User ↔ Employee is a 1:1 across `identity` and `organization`**, provisioned by a saga
  (eventual consistency, ADR-0014). The orchestration direction and consistency model are
  recorded in **ADR-0025** — read it before touching account/employee creation.
- **The `AttendanceDay` aggregate spans the whole company's day** by design; the trade-off
  and rejected alternatives are recorded in **ADR-0026**.

---

## Architecture rules

- **Dependency rule (Clean Architecture):** `domain` depends on nothing; `application`
  depends only on `domain`; `infrastructure` depends inward; hosts/`apps` wire it all
  together. Enforced by `tests/architecture`.
- **DDD invariants:** behaviour lives in aggregates; value objects over primitives (no
  primitive obsession); aggregates are consistency boundaries; domain events for
  intra-context reactions.
- **Cross-context communication is by ID and integration events only.** Never
  reference another context's aggregate type directly. Cross-context flows go through
  Wolverine integration events with the transactional outbox/inbox. Each context owns the
  events it **publishes** in a `libs/<context>/contracts` library (its *published
  language*); consumers reference the producer's contracts library only. Contracts live
  under the neutral `SmartSolutionsLab.Roomy.Contracts.<OwningContext>` namespace
  (`context:shared`) and carry IDs/primitives, never domain value objects (ADR-0031). The
  wire event is mapped to an internal command at the infrastructure edge, so `application`
  never references another context's contracts.
- **Each context is an independently deployable service with its own database**
  (microservices, ADR-0014). No shared database, no cross-service joins or direct DB
  access; services integrate only through async integration events. No distributed
  transactions — use sagas / eventual consistency.
- **No framework in the core; no MediatR.** `application` owns its dispatch and
  messaging abstractions — command/query handlers and an outbound integration-event
  port it defines itself. Wolverine is an *infrastructure adapter* wired only at the
  composition root and introduced as late as the design allows; `domain` and
  `application` never reference it or any other framework type. See ADR-0005.
- **YARP is the only public entry point.** Context APIs are internal; the gateway/BFF
  composes them for the Angular app.

### Nx tag taxonomy (module boundaries)

Every Nx project carries two tags — one `type:*` (layer) and one `context:*` (bounded
context). `@nx/enforce-module-boundaries` (`eslint.config.mjs`) turns the rules above into
lint failures (ADR-0002/0003). The .NET side mirrors this with architecture tests (#13).

| Tag axis | Values |
|---|---|
| `type:*` | backend: `domain` · `application` · `infrastructure` · `app` · `util`; frontend (ADR-0035): `feature` · `ui` · `api` (per-context OpenAPI client) · `data-access` (shared client-side data utils) |
| `context:*` | `identity` · `organization` · `attendance` · `shared` · `web` (the single SPA, ADR-0035) |

`depConstraints` (encoded in `eslint.config.mjs`):

- **Backend layer (dependency rule):** `domain` → `domain`, `util`; `application` → `application`,
  `domain`, `util`; `infrastructure` → `infrastructure`, `application`, `domain`, `util`;
  `app` (composition root) → any layer; `util` → `util` only.
- **Frontend layer (Angular libs, ADR-0035):** `feature` → `feature`, `ui`, `api`, `data-access`,
  `util`; `ui` → `ui`, `util`; `api` → `api`, `data-access`, `util`; `data-access` → `data-access`,
  `util`. The `app` composition root may also depend on `feature`/`ui`/`api`/`data-access`.
- **Context isolation:** each `context:*` may depend only on its own context and
  `context:shared`; `context:shared` depends only on `context:shared`. Cross-context flow
  is by ID + integration events, never by importing another context's libs. **Exception:** the
  single Angular SPA is tagged `context:web` and may compose any context's frontend libs — it is
  the one frontend composition root (ADR-0016/0030/0035); backend contexts stay isolated per host.

> ESLint here is scoped to the boundary rule only; the full lint/format ruleset is #10.

**Architecture tests (`tests/architecture`).** The .NET counterpart enforces the same
dependency rule (plus "no MediatR" / no-framework-in-core) via NetArchTest. Its
convention-based rules inspect every *loaded* `SmartSolutionsLab.Roomy.*` assembly, and an
assembly is only loaded if `Roomy.ArchitectureTests` references it. **When you create a
context, you MUST add its `domain`/`application`/`infrastructure` projects as
`ProjectReference`s to `Roomy.ArchitectureTests`** — otherwise its layers are never
inspected and the rules pass *vacuously* (green but enforcing nothing). Adding the
reference is part of creating a context. See `tests/architecture/README.md`.

---

## The work loop (spec-driven, test-first)

One pass = one story, started in a **fresh agent context** (golden rule 7).

1. **Specify** — `specs/` entry with acceptance criteria (EARS-style, testable) + target
   bounded context.
2. **Plan** — technical plan; if architectural, produce an ADR first.
3. **Tasks** — decompose into small steps, each tied to specific acceptance criteria.
4. **Red** — translate the acceptance criteria into tests and watch them fail. No
   implementation exists yet; the failing tests are the contract for the slice.
5. **Green** — write the minimum code to make the failing tests pass. Nothing more.
6. **Refactor** — clean up under a green bar: remove duplication, sharpen names, tidy
   structure. Tests stay green throughout.
7. **Verify** — run the full gate suite (below) on affected projects; must be green.
8. **Review & merge** — human reviews intent + domain correctness; gates own the rest.

`main` is always green and releasable. Branch protection requires a PR and passing
checks.

---

## Quality gates (all must pass before a task is "done")

The full testing strategy — pyramid, coverage floor, mutation testing, e2e, contract
tests — is authoritative in `docs/testing-strategy.md`. Run on affected projects:

```
pnpm nx affected -t lint test build      # JS/TS + Angular
dotnet build  -warnaserror               # nullable on, analyzers on, no warnings
dotnet test                              # unit + integration + architecture tests
dotnet format --verify-no-changes        # formatting
pnpm nx affected -t lint                 # includes Nx module-boundary lint
```

> **FILL IN** the exact target names once the Nx targets and solution layout exist.

A gate failure is never resolved by suppression. No new `#pragma warning disable`,
`[SuppressMessage]`, `eslint-disable`, or `[Skip]`/`[Ignore]` without an inline
justification comment **and** sign-off in the PR.

---

## Coding conventions

Full rules are authoritative in `docs/coding-standards/csharp.md` and
`docs/coding-standards/typescript.md`. The load-bearing highlights, repeated here:

- **Names reveal intent; comments only when needed** — a comment explains *why*, never
  *what*; if you need one to explain *what*, rename instead. Default to **no comment**; no
  ceremonial XML/JSDoc that echoes a name. No abbreviations or single-letter names, including
  lambda/LINQ parameters.
- **No primitive obsession** — domain concepts are types, not raw primitives: C# value
  objects (invariants enforced with `Ensure.That(...)`) and TypeScript branded types.
- **Domain modelling** — organize the domain **by aggregate** (a folder + namespace per
  aggregate holds the root, its value objects, **and its repository interface**). Identifiers
  are GUIDv7 branded types named `…Identifier` (never `…Id`) with implicit `Guid` conversions
  for EF Core. Value objects implement `IValueObject`; aggregate roots `IAggregate`, other
  entities `IEntity` (markers in `shared-kernel`).
- **No nullable returns on repositories/services** — a contract never returns `T?` to mean
  "not found": a fetch that may miss returns `Result<T>` (`Error.NotFound`), a presence check
  returns `Task<bool>` (`ExistsBy…`). The caller handles absence explicitly.
- **Guard with `Ensure`, not `ArgumentNullException`** — lean on NRT; keep runtime guards
  only at trust boundaries and write them `Ensure.That(x).IsNotNull()`, never
  `ArgumentNullException.ThrowIfNull(...)` / the `ThrowIf*` helpers.
- **One type per file** (a generic + non-generic overload of one concept may share a file);
  a single-statement guard clause may be one line without braces (`if (x) return;`).
- **API-host endpoint DTOs live in `Endpoints/Request/` and `Endpoints/Response/`** subfolders
  with matching sub-namespaces (gateway BFF: `Bff/Request`, `Bff/Response`); response bodies under
  `Response/`, their keyset `Page` wrappers under `Response/Page/`, request bodies under `Request/`,
  endpoint classes stay in `Endpoints/` (ADR-0049). The DTO type names **drop the folder-redundant
  suffix** — `Response.Employee`, `Response.Page.Employee`, `Request.Reserve`, referenced from endpoint
  code by the qualified short form; the stable OpenAPI schema id (`EmployeeResponse`, `EmployeePage`, …)
  is reconstructed from the namespace tail by `web-http`'s `EndpointSchemaIds`, so the wire contract and
  generated client are unchanged (ADR-0050).
- **Tests assert with Shouldly** (`actual.ShouldBe(expected)`), not raw xUnit `Assert.*`.
- **No framework in `domain`/`application`** — owned abstractions only (ADR-0005).
- **C#:** root namespace `SmartSolutionsLab.Roomy` · file-scoped namespaces · `nullable`
  on · warnings-as-errors · async all the way with `CancellationToken` · constructor
  injection only.
- **Angular:** standalone + signal-based + zoneless · `OnPush` · `inject()` · signal
  `input()`/`output()` · no `NgModule` · per-context feature libs at `frontend/libs/<context>/<type>`
  (`@roomy/<context>-<type>`, types `feature`/`api`; shared libs add `ui`/`data-access`/`util`) mirror the backend contexts
  (ADR-0035). Libs are tested with `vitest-analog` + `@testing-library/angular`; the app uses
  `@angular/build:unit-test` — same test code, different runner.

---

## Definition of Ready / Done

**Ready:** spec exists · acceptance criteria are testable · bounded context identified ·
ADR written if the change is architectural.

**Done:** every acceptance criterion has a test that was written *before* its
implementation and now passes · all gates green · ADRs and this file updated if
conventions changed · no unjustified suppressions or skipped tests.

---

## Where things live

- **Decisions:** `docs/adr/` — read these to understand *why* before changing *what*.
- **Specs:** `specs/`.
- **Architecture overview:** `docs/architecture.md`.

When you change a convention or make an architectural call, update the relevant file in
the same change. Documentation drift is a defect.

<!-- SPECKIT START -->
Active feature plan: `specs/012-employee-search/plan.md` (with `research.md`,
`data-model.md`, `contracts/`, `quickstart.md`). For technologies, project structure,
and other important details for the current slice, read that plan and its design artifacts.
<!-- SPECKIT END -->
