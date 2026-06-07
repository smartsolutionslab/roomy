<!--
SYNC IMPACT REPORT
==================
Version change: (template) → 1.0.0
Rationale: Initial ratification. First concrete constitution replacing the bundled
template; codifies governance already established in CLAUDE.md and ADRs 0001–0026.

Principles defined (7):
  I.   Spec-Driven & Test-First (NON-NEGOTIABLE)
  II.  Clean Architecture & DDD Bounded Contexts
  III. Context Isolation — IDs & Integration Events Only
  IV.  No Framework in the Core — Owned Abstractions
  V.   Decisions Are Recorded (ADR-before-code)
  VI.  Green Before Done — No Suppressions
  VII. Small, Single-Purpose Changes — Trunk-Based

Added sections: Technology Constraints; Development Workflow & Quality Gates; Governance.
Removed sections: none (template placeholders fully replaced).

Templates / artifacts reviewed for alignment:
  ✅ .specify/templates/plan-template.md   — Constitution Check gate aligns (no edit needed)
  ✅ .specify/templates/spec-template.md   — tech-agnostic spec requirement aligns
  ✅ .specify/templates/tasks-template.md  — test-first task ordering aligns
  ✅ CLAUDE.md                             — remains the canonical contract; this file references it

Deferred TODOs: none. Ratification date set to first-adoption date (2026-06-07).
-->

# Roomy Constitution

This constitution codifies the non-negotiable principles governing the Roomy codebase.
It does not replace `CLAUDE.md` — that file remains the canonical operating contract and
the authoritative source for detail. Where this constitution and `CLAUDE.md` overlap they
MUST agree; where they conflict, `CLAUDE.md` wins and this file MUST be amended to match.
The ADRs in `docs/adr/` hold the rationale behind every principle below.

## Core Principles

### I. Spec-Driven & Test-First (NON-NEGOTIABLE)

No code is written without a spec, and no implementation is written without a failing
test. Every change MUST trace to a Spec Kit spec in `specs/` with testable acceptance
criteria; each criterion MUST be expressed as a test that fails *before* the implementing
code exists. The cycle is Red → Green → Refactor: watch the test fail, write the minimum
code to make it pass, then clean up under a green bar. "Fix X" means "write a test that
reproduces X, then make it green." If there is no spec, stop and create one; if the spec
is ambiguous, stop and ask rather than guess the domain. (ADR-0009; CLAUDE.md golden
rules 1 & 6.)

### II. Clean Architecture & DDD Bounded Contexts

Each bounded context is structured with Clean Architecture: `domain` depends on nothing;
`application` depends only on `domain`; `infrastructure` depends inward; hosts/`apps` wire
everything at the composition root. The model has three contexts — `identity`,
`organization`, `attendance` — and code MUST live in the context that owns its concept.
Behaviour lives in aggregates, invariants are enforced with value objects over primitives,
and aggregates are the consistency boundary. The dependency rule and DDD invariants are
enforced by architecture tests (NetArchTest) in `tests/architecture` and by the Nx
module-boundary lint — these MUST NOT be worked around. (ADR-0002, ADR-0003; CLAUDE.md
golden rule 2.)

### III. Context Isolation — IDs & Integration Events Only

Each context is an independently deployable service with its own database. There is no
shared database, no cross-service join, and no direct access to another service's data. A
context MUST NOT reference another context's aggregate types; cross-context communication
is by ID and asynchronous integration events only, carried over Wolverine with the
transactional outbox/inbox. Multi-service workflows use sagas and eventual consistency —
never distributed transactions. (ADR-0005, ADR-0012, ADR-0014, ADR-0015, ADR-0025.)

### IV. No Framework in the Core — Owned Abstractions

`domain` and `application` MUST NOT reference Wolverine, MediatR, EF Core, or any other
framework type. `application` owns its dispatch and messaging abstractions — command/query
handlers and an outbound integration-event port it defines itself. MediatR is forbidden.
Framework adapters (Wolverine, EF Core, YARP) are wired only at the composition root and
introduced as late as the design allows. YARP is the only public entry point; context APIs
are internal. (ADR-0005, ADR-0013, ADR-0018; CLAUDE.md architecture rules.)

### V. Decisions Are Recorded (ADR-before-code)

Any decision that changes structure, introduces or removes a dependency, sets a
cross-cutting pattern, or would be expensive to reverse MUST be captured as an ADR in
`docs/adr/` *before* the implementing code is written. ADRs follow MADR format, start as
`Proposed`, and move to `Accepted` on merge; an accepted ADR is immutable and is changed
only by a new ADR that supersedes it. When a convention changes, the relevant docs
(`CLAUDE.md`, this constitution, the ADR index) are updated in the same change —
documentation drift is a defect. (ADR-0001; CLAUDE.md golden rule 4.)

### VI. Green Before Done — No Suppressions

A task is done only when the full verify suite passes on affected projects. A gate failure
is never resolved by suppression: no new `#pragma warning disable`, `[SuppressMessage]`,
`eslint-disable`, or `[Skip]`/`[Ignore]`, and no deleted or weakened test, may be used to
make the build pass. The cause is fixed instead. The only exception is a suppression that
carries an inline justification comment *and* explicit PR sign-off. `main` is always green
and releasable. (ADR-0009, ADR-0022; CLAUDE.md golden rule 3 & quality gates.)

### VII. Small, Single-Purpose Changes — Trunk-Based

Work proceeds one story per short-lived branch off `main`, each begun in a fresh agent
context. Every commit is a clean, atomic Conventional Commit — no `wip` or `fix lint`
noise, because history is not squashed and commits land on `main` as written. A PR is
small enough to review in one sitting and is merged with rebase-and-merge. Changes are
surgical: touch only what the task requires, with no drive-by refactors or unrelated
reformatting; every changed line traces to the spec. (ADR-0010; CLAUDE.md golden rule 5 &
behavioural guardrails.)

## Technology Constraints

The stack is fixed by ADR and MUST be honoured; deviations require a superseding ADR.

- **Backend:** .NET 10 / C#, Clean Architecture + DDD. Root namespace
  `SmartSolutionsLab.Roomy`, file-scoped namespaces, nullable on, warnings-as-errors,
  async-all-the-way with `CancellationToken`, constructor injection only.
- **Messaging:** Wolverine behind owned abstractions; transport-agnostic (RabbitMQ default).
- **Gateway/BFF:** YARP as the only public entry point. **Auth:** Keycloak (OIDC) with the
  BFF security pattern — no tokens in the SPA.
- **APIs:** REST/JSON with OpenAPI; the typed Angular client is generated from the spec.
- **Persistence:** EF Core on PostgreSQL; hand-rolled event store on Postgres for
  event-sourced contexts.
- **Frontend:** Angular — single app + feature libs per context, standalone signal-based
  components, zoneless, `OnPush`, `inject()`, no `NgModule`. Localized with Transloco
  (DE + EN, no hardcoded strings); WCAG 2.2 AA baseline via Angular CDK.
- **Tooling:** Nx monorepo; **pnpm only** (never npm or yarn); .NET Aspire for local
  orchestration and composition.

(ADR-0012, ADR-0013, ADR-0015, ADR-0016, ADR-0018, ADR-0019, ADR-0021, ADR-0024; the
authoritative coding standards live in `docs/coding-standards/`.)

## Development Workflow & Quality Gates

One pass = one story, run through the work loop: **Specify → Plan (ADR first if
architectural) → Tasks → Red → Green → Refactor → Verify → Review & merge.**

Before a task is "done", the full gate suite MUST pass on affected projects:

```
pnpm nx affected -t lint test build      # JS/TS + Angular, incl. module-boundary lint
dotnet build  -warnaserror               # nullable on, analyzers on, no warnings
dotnet test                              # unit + integration + architecture tests
dotnet format --verify-no-changes        # formatting
```

The full testing strategy — pyramid, coverage floor, mutation testing, e2e, contract
tests — is authoritative in `docs/testing-strategy.md`. Branch protection requires a PR
and passing checks; a human reviews intent and domain correctness, the gates own the rest.

## Governance

This constitution and `CLAUDE.md` together govern all work in the repository; both
supersede ad-hoc practice. All PRs and reviews MUST verify compliance with the principles
above, and any added complexity MUST be justified against Principle VII (simplicity and
surgical change).

**Amendment procedure.** Changes to this constitution are made by PR, accompanied by the
ADR(s) that motivate them where the change is architectural, and by matching updates to
`CLAUDE.md` and any affected `.specify/` templates in the same change. The Sync Impact
Report at the top of this file MUST be updated on every amendment.

**Versioning policy (semantic).** MAJOR — backward-incompatible governance or principle
removal/redefinition; MINOR — a new principle or materially expanded guidance; PATCH —
clarifications, wording, and non-semantic refinements.

**Compliance review.** `CLAUDE.md` is the canonical contract and the runtime guidance for
agents and humans; this constitution is the principled summary. When they diverge,
`CLAUDE.md` wins and this file is amended to restore agreement.

**Version**: 1.0.0 | **Ratified**: 2026-06-07 | **Last Amended**: 2026-06-07
