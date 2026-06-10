# Architecture Decision Records

This directory captures the significant architectural decisions for Roomy, in
[MADR](https://adr.github.io/madr/) format.

## Why

Agents (and humans) need the *why* behind the structure, not just the code. An ADR is
the durable record of a decision, its context, and its consequences, so future changes
are made with the original reasoning in view.

## When to write one

Write an ADR for any decision that changes structure, introduces or removes a
dependency, sets a cross-cutting pattern, or would be expensive to reverse. Routine,
local choices do not need one.

ADRs are written **before** the implementing code, not after.

## How

1. Copy `0000-template.md` to `NNNN-short-title.md` (next free number, kebab-case).
2. Fill it in. Keep it concise — a screen, not an essay.
3. Set status to `Proposed`, open the PR, and move to `Accepted` on merge.
4. Decisions are immutable once accepted. To change one, write a new ADR that
   `Supersedes` it and mark the old one `Superseded by NNNN`.

## Index

| ADR | Title | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-nx-monorepo-with-enforced-module-boundaries.md) | Nx monorepo with enforced module boundaries | Accepted |
| [0003](0003-clean-architecture-and-ddd-bounded-contexts.md) | Clean Architecture and DDD bounded contexts | Accepted |
| 0004 | .NET Aspire for orchestration and composition | _pending_ |
| [0005](0005-own-dispatch-and-messaging-abstractions.md) | Own the dispatch and messaging abstractions; defer Wolverine to the edge | Accepted |
| 0006 | YARP as API gateway and BFF | _pending_ |
| 0007 | Angular with signal-based components | _pending_ |
| 0008 | pnpm as the JavaScript package manager | _pending_ |
| [0009](0009-test-driven-development-as-default-discipline.md) | Test-driven development as the default implementation discipline | Accepted |
| [0010](0010-trunk-based-development-with-rebase-and-merge.md) | Trunk-based development with rebase-and-merge and per-commit Conventional Commits | Accepted |
| [0011](0011-single-tenant-first-release-database-per-tenant-target.md) | Single-tenant first release, database-per-tenant as the target | Accepted |
| [0012](0012-ef-core-persistence-hand-rolled-event-store.md) | EF Core for persistence; hand-rolled event store on PostgreSQL for event-sourced contexts | Accepted |
| [0013](0013-keycloak-oidc-bff-security-pattern.md) | Authentication via self-hosted Keycloak (OIDC) with the BFF security pattern | Accepted |
| [0014](0014-microservices-one-service-per-context.md) | Microservices — one service per bounded context | Accepted |
| [0015](0015-rabbitmq-message-broker.md) | Transport-agnostic messaging: RabbitMQ default, Azure Service Bus and AWS selectable | Accepted |
| [0016](0016-single-angular-app-feature-libraries.md) | Single Angular app with feature libraries per context | Accepted |
| [0017](0017-host-on-azure-container-apps.md) | Host on Azure, with Azure Container Apps as the compute target | Accepted |
| [0018](0018-rest-openapi-grpc-later.md) | REST/JSON with OpenAPI now; gRPC reserved for hot internal paths | Accepted |
| [0019](0019-frontend-tooling.md) | Frontend tooling: NgRx SignalStore, vanilla CSS (deferring preprocessors), Vitest | Accepted |
| [0020](0020-frontend-validation-policy.md) | Frontend validation: trust the OpenAPI-generated client, validate only untrusted input | Accepted |
| [0021](0021-angular-cdk-headless-components.md) | UI on Angular CDK + headless components, styled with own CSS | Accepted |
| [0022](0022-testing-strategy.md) | Testing strategy: TDD pyramid, coverage as a diagnostic, mutation testing, Playwright e2e, contract tests | Accepted |
| [0023](0023-ai-posture-and-mcp-server.md) | AI posture: product features deferred, architecture AI-ready, MCP server for agent access | Accepted |
| [0024](0024-frontend-localization-accessibility.md) | Frontend baselines: localization (Transloco, DE + EN) and WCAG 2.2 AA accessibility | Accepted |
| [0025](0025-user-employee-provisioning-saga.md) | User/Employee provisioning across Identity and Organization | Proposed |
| [0026](0026-attendanceday-aggregate-granularity.md) | AttendanceDay aggregate granularity (CompanyId + Date) | Proposed |
| [0031](0031-integration-event-contract-strategy.md) | Integration-event contract strategy: per-context published language | Proposed |
| [0032](0032-domain-events-on-aggregates.md) | Domain events raised by aggregates, collected and dispatch-deferred | Proposed |
| [0036](0036-openapi-client-codegen.md) | OpenAPI client codegen: build-time spec emit, ng-openapi-gen, drift-gated in CI | Proposed |
| [0037](0037-integration-events-via-domain-event-outbox-drain.md) | Integration events published by draining domain events into the outbox at commit | Proposed |
| [0038](0038-occupancy-read-side-inline-projection.md) | Occupancy read side: inline synchronous projection into materialized read models | Proposed |
| [0039](0039-event-sourced-write-model.md) | Event-sourced write model: aggregate base, repository, and optimistic-retry | Proposed |
| [0040](0040-shared-frontend-route-guards.md) | Shared frontend route guards and not-authorized view | Accepted |
| [0041](0041-rabbitmq-conventional-routing-and-no-retry-strategy.md) | Route integration events by convention; no EF retry strategy with the Wolverine outbox | Proposed |
| [0042](0042-scalar-api-docs-keycloak-oauth.md) | Interactive API docs with Scalar and a dev-only Keycloak OAuth try-it-out | Proposed |
| [0043](0043-central-package-management.md) | Central Package Management for NuGet versions | Accepted |
| [0044](0044-cursor-keyset-pagination.md) | Cursor/keyset pagination for endless lists | Accepted |
| [0045](0045-shared-keycloak-auth-and-unit-of-work-contract.md) | Share the Keycloak JWT bearer composition and the IUnitOfWork port | Accepted |
