# Roomy — Project Setup & Init Issues

A ready-to-file backlog for standing up the project. Grouped into milestones; later issues
depend on earlier ones. Labels in parentheses. Each follows the spec-driven, test-first
loop in `CLAUDE.md`.

> Import tip: with the GitHub CLI you can create these quickly, e.g.
> `gh issue create --title "..." --body "..." --label setup`.

---

## M0 — Bootstrap & finalize decisions

### #1 Import the governance baseline (chore, docs)
Push the existing baseline: `CLAUDE.md`, `CONTRIBUTING.md`, `docs/` (ADRs, coding
standards, testing strategy), and the shared-kernel primitives.
- **Done:** files on `main`; branch protection enabled afterwards (see #16).

### #2 Confirm version pins and name the bounded contexts (docs)
Resolve the `FILL IN` markers: Node LTS and Angular major; the real bounded-context names
and where `Company` lives.
- **Done:** `CLAUDE.md` stack table and repo-layout placeholders updated.

### #3 Write the remaining ADR backfills (docs)
ADR-0004 .NET Aspire, 0006 YARP, 0007 Angular signal-based components, 0008 pnpm.
- **Done:** four ADRs added; index updated.

---

## M1 — Workspace & quality gates

### #4 Create the Nx monorepo (chore)
Integrated Nx workspace at the repo root, **pnpm** as the package manager.
- **Done:** `nx.json`, `pnpm-workspace.yaml`, base `tsconfig` in place; `nx graph` runs.

### #5 Add the .NET solution + Aspire (chore, backend)
Solution file, .NET Aspire **app host** and **service defaults** projects.
- **Done:** `dotnet build` succeeds; Aspire dashboard runs locally.

### #6 Wire the shared-kernel and shared/util libraries (chore)
Move `Ensure`, `Result` (+ tests) into a `shared-kernel` .NET library; `branding.ts` into
`@roomy/shared-util`.
- **Done:** both libraries build and their tests pass.

### #7 Nx tag taxonomy + module boundaries (chore, ci)
`context:*` and `type:*` tags; `@nx/enforce-module-boundaries` rules.
- **Done:** a deliberate cross-boundary import fails lint.

### #8 `Directory.Build.props` (chore, backend)
`Nullable` on, warnings-as-errors, latest analyzers, `LangVersion`, and
`RootNamespace = SmartSolutionsLab.Roomy`.
- **Done:** a warning fails the build; root namespace applied.

### #9 `.editorconfig` (chore)
C# + TS formatting and naming rules, including private fields as `camelCase` (no `_`).
- **Done:** `dotnet format --verify-no-changes` and Prettier check pass on the baseline.

### #10 ESLint + Prettier (chore, frontend)
Angular ESLint, the Nx boundary rule, import ordering, no-unused.
- **Done:** lint runs clean on the baseline.

### #11 Conventional Commits enforcement (chore, ci)
commitlint with a `commit-msg` hook (husky or lefthook) **and** a CI check over the PR's
commit range. No squash (ADR-0010).
- **Done:** a non-conventional commit message is rejected locally and in CI.

### #12 CI pipeline (ci)
`nx affected -t lint test build`, `dotnet build -warnaserror`, `dotnet test`,
`dotnet format --verify-no-changes`, commit-range conventional check.
- **Done:** pipeline green on the baseline; required on PRs.

### #13 Architecture tests skeleton (backend, ci)
NetArchTest rules: the dependency rule, no framework types in `domain`/`application`,
cross-context-by-ID, value-object conventions.
- **Done:** rules pass for the baseline and fail on a deliberate violation.

### #14 Coverage gate (ci)
Coverlet (.NET) + Vitest coverage (TS); enforce the ~85–90% line+branch **floor on
domain/application only**; report elsewhere (ADR-0022).
- **Done:** coverage collected per affected project; floor enforced in CI.

### #15 PR/issue templates + README (docs)
- **Done:** templates in `.github/`; README points at `CLAUDE.md` and the docs.

### #16 Branch protection on `main` (chore)
Require a PR and passing checks; **rebase-and-merge**, squash disabled (ADR-0010).
- **Done:** direct pushes to `main` blocked; merges are linear.

---

## M2 — Platform & infrastructure

### #17 Local environment via the Aspire app host (infra)
Compose Postgres, RabbitMQ, and Keycloak containers with service discovery.
- **Done:** `one command` brings the full local stack up.

### #18 Owned application contracts (backend)
Dispatch/handler abstractions and `IIntegrationEventPublisher` — the seam that keeps
Wolverine at the edge (ADR-0005).
- **Done:** abstractions defined in `application`; no framework reference.

### #19 EF Core baseline + hand-rolled event store skeleton (backend)
EF Core setup; append-only events table with a unique `(stream_id, version)` constraint,
`jsonb` payloads, a global sequence, and a transactional outbox table (ADR-0012).
- **Done:** append + load-by-replay works; concurrency conflict is rejected (tested).

### #20 Wolverine + RabbitMQ transport behind the abstractions (backend)
Config-selectable transport (RabbitMQ default; ASB/AWS switchable, ADR-0015); outbox relay
+ inbox dedup.
- **Done:** an integration event round-trips through RabbitMQ locally.

### #21 YARP BFF + Keycloak OIDC (backend)
BFF security pattern (cookie to the browser, token to services); JWT validation on context
APIs; dev realm (ADR-0013).
- **Done:** login flow works end-to-end; SPA holds only a cookie.

### #22 OpenAPI + typed Angular client codegen (ci, frontend)
Generate the client from the services' OpenAPI specs (ADR-0018/0020).
- **Done:** client regenerates in CI and is consumed by the frontend.

### #23 Angular app shell (frontend)
App shell, Transloco (DE + EN), axe a11y wiring, CSS custom-property design tokens
(ADR-0019/0021/0024).
- **Done:** shell renders, language switch works, axe runs in tests.

### #24 Mutation + e2e harness (ci)
Stryker.NET / StrykerJS nightly on domain/application; Playwright against the Aspire stack
with a Keycloak test realm; one smoke test (ADR-0022).
- **Done:** nightly mutation job runs; a Playwright smoke test passes in CI.

---

## M3 — First vertical slice (walking skeleton)

### #25 Walking skeleton: rename a Company, end to end (feat, domain)
One thin slice through every layer to establish the template every later story follows:
`Company` aggregate (rename) → application use case returning `Result` → EF Core
persistence → REST endpoint → BFF → `@roomy/company-feature` (signals) → one e2e.
- **Done:** the slice ships through the full spec-driven loop with all gates green; it
  becomes the reference example for subsequent features.
