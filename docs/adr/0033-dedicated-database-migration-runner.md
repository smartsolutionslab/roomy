# 0033. A dedicated migration runner creates databases and applies EF migrations

- **Status:** Proposed
- **Date:** 2026-06-09
- **Deciders:** Heiko Weiß

## Context and problem statement

Each context service owns its own PostgreSQL database (ADR-0014) and maps it with EF Core
(ADR-0012). The schema has to exist before the service reads it — the `identity` host, for
example, runs the `DefaultAdmin` seeder at startup, which queries the `users` table.

Today the schema is applied **in-process at API startup**: `IdentityDatabaseMigrator`, an
`IHostedService` in `apps/identity-api`, calls `DbContext.Database.MigrateAsync()` before
the seeder runs. Its own comment concedes this is a stopgap — *"single-instance dev/MVP
convention; production schema rollout is a separate ops concern."* As more contexts land
(`organization`, `attendance`) the pattern would be copied into each host, and the
shortcomings compound:

- **Replica races.** Run more than one instance of a service and they race to apply DDL on
  the same database on startup.
- **Excess runtime privilege.** The application's runtime identity needs schema-altering
  (DDL) rights for the life of the process, not just at rollout.
- **Startup coupling.** Schema rollout is welded to application boot; it cannot be run,
  gated, or audited as its own step.

We need one place that creates each database and applies its migrations, decoupled from the
serving processes.

## Decision drivers

- **ADR-0014 (database-per-service).** Each context owns its own database; whatever applies
  the schema must address each database independently.
- **Operational correctness.** Schema rollout should run **once**, before the services that
  read the schema start, and exactly one component should hold DDL rights.
- **Aspire-native orchestration.** The local/composition story is .NET Aspire; it can model
  a run-once resource and gate dependents on its completion (`WaitForCompletion`).
- **Simplicity first (`CLAUDE.md`).** Build the minimum that works for the one context that
  exists today, with a clean seam to add the next two — no dead per-context machinery.
- **Don't widen runtime context coupling.** Contexts integrate only by ID + integration
  events (ADR-0014/0031); nothing introduced here may let one context's *runtime* code call
  another's.

## Considered options

- **A — Single shared migration runner (`apps/db-migrator`).** One console process
  references every context's `infrastructure` (today: `identity`), creates each database and
  applies its EF migrations in one pass, then exits. The context APIs declare
  `WaitForCompletion(db-migrator)` and no longer self-migrate. One resource, one gate.
- **B — Per-service migration runner.** One runner per context (a worker project, or a
  `--migrate` mode of each host). Maximally aligned with independent deployability, but
  multiplies projects/wiring and, for the MVP's single-instance composition, buys isolation
  nothing yet exercises.
- **C — Keep in-host startup migration.** Status quo. Rejected: it is the very anti-pattern
  above (replica races, standing DDL rights, startup coupling).

## Decision

We chose **Option A**: a dedicated, shared migration runner at `apps/db-migrator`.

- It is a **run-once console** (`Host.CreateApplicationBuilder` + `AddServiceDefaults` for
  logging/telemetry, no web surface). It composes each context's persistence through that
  context's own registration extension (today `AddIdentityPersistence`), resolves each
  registered `DbContext`, and calls `MigrateAsync` on it. `MigrateAsync` **creates the
  database if it does not exist** and applies any pending migrations, so the runner both
  *creates databases* and *rolls the schema forward*. It is **idempotent**: a second run
  with no pending migrations is a no-op.
- On any context's migration failure it logs the offending context and **exits non-zero**,
  which fails the orchestration's `WaitForCompletion` gate rather than letting dependents
  start against a half-migrated database.
- The in-host `IdentityDatabaseMigrator` is **removed**. In the Aspire graph,
  `db-migrator` references the same database resource(s) and the context APIs declare
  `WaitForCompletion(db-migrator)`, so a service starts only after its schema is in place.
  Each new context adds its database reference and persistence registration to the runner
  and a `WaitForCompletion` on its API — the same two lines per context.

**On context isolation.** A single process referencing several contexts' `infrastructure`
assemblies is a **deploy-time composition root**, in the same category as the Aspire
`AppHost` (which already references multiple context hosts) — not a runtime cross-context
dependency. It only constructs each context's `DbContext` to run that context's own
migrations; no context's domain/application code calls another's, and no context gains a
reference to another. The Nx/ESLint boundary rules govern the frontend libs, and the .NET
architecture tests govern the `domain`/`application`/`infrastructure` **layer** rule; an
`app` composition root is permitted to reference any layer, so this introduces no boundary
violation. The trade-off (a shared rollout artifact vs. fully independent per-service
rollout) is accepted for the MVP and revisited if/when services are rolled out on genuinely
independent cadences (Option B remains the documented escape hatch).

## Consequences

**Positive**

- One component holds DDL rights and runs once; the serving processes need only DML rights
  and never race to migrate.
- Schema rollout is an explicit, gated, observable step (`WaitForCompletion`), not a
  side effect of app boot.
- Adding a context is two lines (a database reference + persistence registration in the
  runner, a `WaitForCompletion` on its API); no per-host migrator is copied around.

**Negative / trade-offs**

- The runner references multiple contexts' `infrastructure` assemblies, coupling them in a
  single rollout artifact. Mitigated by keeping it a pure composition root (see above) and
  by Option B as the recorded path back to per-service rollout.
- Running a service **outside** Aspire no longer auto-creates its schema; the migrator must
  be run first. This is the intended production posture (rollout is its own step), and the
  integration tests apply migrations through their own fixtures, so it does not affect them.

**Follow-ups**

- `organization` and `attendance` register their persistence in the runner and add a
  `WaitForCompletion` on their APIs as they land.
- Production rollout (running `db-migrator` as a pre-deploy job/init step on the target
  platform, ADR-0017) is wired when deployment is set up; this ADR covers the composition
  and the dev/Aspire gate.
