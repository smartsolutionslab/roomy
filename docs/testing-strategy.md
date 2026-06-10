# Testing Strategy

Authoritative for how Roomy is tested. TDD is the discipline (ADR-0009); this document is
the full pyramid, the tooling, the coverage policy, and the quality signals. Language-
specific conventions (test naming, AAA, builders, runners) live in
`docs/coding-standards/`.

## Principles

- **Test-first.** Tests precede implementation — red, green, refactor (ADR-0009).
- **Pyramid shape.** Most value at the bottom (fast, pure unit tests), very little at the
  top (slow e2e). Push everything provable down the pyramid.
- **Coverage is a diagnostic, not a target.** The real quality signal is the mutation
  score, not a coverage percentage.
- **Agent discipline.** Never write tests that execute code without asserting behaviour to
  hit a number; never skip or suppress to go green; don't accumulate e2e.

## The pyramid

| Layer | What | Tooling | CI |
|---|---|---|---|
| **Unit** | Domain + application logic; pure and fast; the bulk of tests | xUnit (.NET), Vitest (TS) | gated (affected) |
| **Integration** | Per service: real persistence, event store, messaging | xUnit + Testcontainers | gated (affected) |
| **Architecture** | Dependency rule, no framework in the core, cross-context-by-ID, value-object conventions | NetArchTest | gated |
| **Component** | Angular components — behaviour via the DOM, not implementation details | Vitest + Angular Testing Library | gated (affected) |
| **Contract** | BFF/service seams: provider verifies it serves its OpenAPI spec; consumer uses the typed generated client; async event schemas verified between publisher and consumer | OpenAPI verification (+ Pact if consumer-driven contracts are wanted) | gated (fast) |
| **E2e** | A few critical full-stack user journeys | Playwright | smoke per PR, full suite nightly |

Contract tests are what let the e2e layer stay thin: they catch integration breakage
cheaply at the seams, so e2e is reserved for genuine end-to-end journeys.

**Assertions.** .NET tests assert with **Shouldly** (free, MIT) — `actual.ShouldBe(expected)`,
`Should.Throw<T>(...)` — for readable failures, not raw xUnit `Assert.*`. FluentAssertions v8
is a paid commercial license and is **not** used. TS/Angular tests use Vitest's built-in `expect`.

**Test doubles.** .NET **unit** tests stub and verify ports with **NSubstitute** (free, MIT;
ADR-0052) — `Substitute.For<IPort>()`, `port.Method(Arg.Any<…>()).Returns(Result.Success(value))`,
`await port.Received(1).Method(expected, …)` / `DidNotReceive()`, and `Arg.Do<T>(list.Add)` to
capture arguments. Assertions stay in the Assert (Shouldly), never inside a double. Integration/e2e
tests run against the real Aspire-composed stack and do **not** mock.

## E2e (Playwright)

- **Thin and critical-path only** — e.g. log in → book a desk → see attendance. Not
  feature coverage.
- **Against the real stack composed by .NET Aspire** — the app host stands up the
  services, Keycloak, Postgres, and RabbitMQ; Playwright drives the real SPA + BFF, not
  mocks. Same in CI.
- **Auth:** a dedicated Keycloak test realm with seeded users and a programmatic login
  helper through the BFF — don't hand-drive the login UI in every test.
- **Eventual consistency:** this is the only layer that validates cross-service journeys
  over async integration events. Assert with polling/explicit waits, **never** `sleep`,
  and keep these tests few.
- **Cadence:** a small smoke subset per PR; the fuller suite nightly.

## Coverage policy

- A **diagnostic, not a target.** Gate only the layer where it's meaningful.
- **Floor of ~85–90% (line *and* branch) on domain + application**, as a merge gate —
  expected to be exceeded naturally by TDD, not padded toward.
- **Reported but not gated** on infrastructure (integration-test-driven, partly glue) and
  UI (cover logic, not template rendering).
- **Exclude** the OpenAPI-generated client, EF migrations, the composition root / Aspire
  app host, and DTOs/records without behaviour.
- Measured **per service** via `nx affected`. Tools: Coverlet (.NET), Vitest coverage (TS).

### Enforcement (issue #14)

- **.NET** — the floor is a build-failing Coverlet MSBuild threshold (`Threshold=85`,
  `ThresholdType=line,branch`). A domain/application test project opts in by importing
  `tests/coverage/CoverageGate.props` and setting `CoverageInclude` to its
  assembly-under-test; from then on `dotnet test` fails below the floor. CI collects and
  reports coverage for **all** test projects via `coverlet.runsettings` (same policy
  exclusions); only the gated projects block the merge. Shared-kernel primitives,
  infrastructure and UI test projects are reported but do **not** import the gate.
- **TS/Vitest** — deferred until the first JS runtime logic and tests exist (frontend
  app shell, feature libs). The only JS library today (`@roomy/util`) is type-only and
  erased at runtime, so it has no executable lines to cover; a Vitest coverage gate
  lands with the first testable TS, reusing the runner mandated by ADR-0019.

## Mutation testing (the real quality signal)

- **Stryker.NET** (C#) and **StrykerJS** (TS) on **domain + application**.
- Run **nightly**, not per-PR (it's slow).
- The mutation score — do the tests actually fail when the code is broken? — is the true
  measure of test quality and the antidote to coverage-gaming in agent-written tests.

## What CI gates vs. reports

- **Gated per PR (affected):** build (warnings-as-errors), unit + integration +
  architecture + component + contract tests, lint + Nx boundaries, formatting, the
  domain/application coverage floor, and the e2e smoke subset.
- **Nightly:** full e2e suite, mutation testing.
- **Reported, not gated:** infrastructure/UI coverage, the mutation-score trend.

A gate failure is never resolved by suppressing an analyzer or skipping/deleting a test —
fix the cause.
